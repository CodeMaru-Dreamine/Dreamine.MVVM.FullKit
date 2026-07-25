using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DreamineWeb.KnowledgeQa.Application;

/// <summary>
/// Runs repository questions outside the lifetime of the page that submitted them and exposes compact polling state.
/// </summary>
public sealed class KnowledgeQuestionJobService : BackgroundService, IKnowledgeQuestionJobService
{
    private static readonly TimeSpan CompletedJobRetention = TimeSpan.FromHours(24);
    private const int MaximumTrackedJobs = 512;
    private readonly Channel<KnowledgeQuestionRequest> _queue = Channel.CreateBounded<KnowledgeQuestionRequest>(
        new BoundedChannelOptions(128)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = false
        });
    private readonly ConcurrentDictionary<Guid, KnowledgeQuestionJobViewModel> _jobs = [];
    private readonly IKnowledgeQaService _knowledgeQa;
    private readonly KnowledgeQaOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<KnowledgeQuestionJobService> _logger;

    public KnowledgeQuestionJobService(
        IKnowledgeQaService knowledgeQa,
        KnowledgeQaOptions options,
        TimeProvider? timeProvider = null,
        ILogger<KnowledgeQuestionJobService>? logger = null)
    {
        _knowledgeQa = knowledgeQa ?? throw new ArgumentNullException(nameof(knowledgeQa));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<KnowledgeQuestionJobService>.Instance;
    }

    public KnowledgeQuestionJobViewModel Enqueue(KnowledgeQuestionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        string question = request.Question.Trim();
        if (question.Length is < 5 or > 1000)
            throw new ArgumentException("Question must contain between 5 and 1,000 characters.", nameof(request));

        PruneJobs();
        Guid submissionId = request.SubmissionId is { } requestedId && requestedId != Guid.Empty
            ? requestedId
            : Guid.NewGuid();
        string language = request.Language.Equals("en", StringComparison.OrdinalIgnoreCase) ? "en" : "ko";
        DateTimeOffset now = _timeProvider.GetUtcNow();
        KnowledgeQuestionJobViewModel candidate = new(
            submissionId, question, language, KnowledgeQuestionJobStatus.Queued, now, now);
        KnowledgeQuestionJobViewModel job = _jobs.GetOrAdd(submissionId, candidate);
        if (!ReferenceEquals(job, candidate))
            return job;

        if (!_queue.Writer.TryWrite(new KnowledgeQuestionRequest(question, language, submissionId)))
        {
            _jobs.TryRemove(submissionId, out _);
            throw new InvalidOperationException(language == "ko"
                ? "현재 답변 대기열이 가득 찼습니다. 잠시 후 다시 시도해 주세요."
                : "The answer queue is full. Please try again shortly.");
        }
        return candidate;
    }

    public KnowledgeQuestionJobViewModel? Get(Guid submissionId) =>
        _jobs.GetValueOrDefault(submissionId);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int workers = Math.Clamp(_options.CodexMaxConcurrency, 1, 4);
        return Task.WhenAll(Enumerable.Range(0, workers).Select(_ => ProcessQueueAsync(stoppingToken)));
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        await foreach (KnowledgeQuestionRequest request in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            Guid submissionId = request.SubmissionId!.Value;
            if (!_jobs.TryGetValue(submissionId, out KnowledgeQuestionJobViewModel? queued))
                continue;

            DateTimeOffset startedAt = _timeProvider.GetUtcNow();
            Set(queued with { Status = KnowledgeQuestionJobStatus.Running, UpdatedAt = startedAt });
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.RequestTimeoutSeconds, 15, 600)));
            try
            {
                KnowledgeQuestionCreatedViewModel result = await _knowledgeQa.AskAsync(request, timeout.Token)
                    .ConfigureAwait(false);
                Set(queued with
                {
                    Status = KnowledgeQuestionJobStatus.Completed,
                    UpdatedAt = _timeProvider.GetUtcNow(),
                    Result = result
                });
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                Set(queued with
                {
                    Status = KnowledgeQuestionJobStatus.Failed,
                    UpdatedAt = _timeProvider.GetUtcNow(),
                    ErrorMessage = request.Language == "ko"
                        ? "저장소 분석이 10분 안에 끝나지 않아 중단되었습니다."
                        : "Repository analysis did not finish within 10 minutes."
                });
            }
            catch (KnowledgeUnverifiedAnswerException exception)
            {
                _logger.LogWarning(
                    "Knowledge Q&A background job {SubmissionId} retained an unverified preview.",
                    submissionId);
                Set(queued with
                {
                    Status = KnowledgeQuestionJobStatus.Failed,
                    UpdatedAt = _timeProvider.GetUtcNow(),
                    ErrorMessage = exception.Message,
                    UnverifiedDraft = exception.Draft
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Knowledge Q&A background job {SubmissionId} failed.", submissionId);
                Set(queued with
                {
                    Status = KnowledgeQuestionJobStatus.Failed,
                    UpdatedAt = _timeProvider.GetUtcNow(),
                    ErrorMessage = exception is InvalidOperationException
                        ? exception.Message
                        : request.Language == "ko"
                            ? "답변 생성 중 오류가 발생했습니다. 잠시 후 다시 시도해 주세요."
                            : "An error occurred while generating the answer. Please try again shortly."
                });
            }
        }
    }

    private void Set(KnowledgeQuestionJobViewModel value) => _jobs[value.SubmissionId] = value;

    private void PruneJobs()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        foreach ((Guid id, KnowledgeQuestionJobViewModel job) in _jobs)
        {
            bool terminal = job.Status is KnowledgeQuestionJobStatus.Completed or KnowledgeQuestionJobStatus.Failed;
            if (terminal && (now - job.UpdatedAt >= CompletedJobRetention || _jobs.Count > MaximumTrackedJobs))
                _jobs.TryRemove(id, out _);
        }
    }
}
