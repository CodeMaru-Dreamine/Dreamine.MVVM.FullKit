using DreamineWeb.KnowledgeQa.Application;
using DreamineWeb.KnowledgeQa.Domain;
using DreamineWeb.Models;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DreamineWeb.KnowledgeQa.Infrastructure;

/// <summary>Persists structured questions and revisions as an atomic App_Data JSON document.</summary>
public sealed class JsonKnowledgeQuestionRepository : IKnowledgeQuestionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<KnowledgeQuestion>? _cache;

    public JsonKnowledgeQuestionRepository(DreamineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Directory.CreateDirectory(options.ResolvedDataPath);
        _path = Path.Combine(options.ResolvedDataPath, "knowledge-questions.json");
    }

    /// <inheritdoc />
    public async Task<KnowledgeQuestion> CreateAsync(
        KnowledgeQuestion question,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(question);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            List<KnowledgeQuestion> questions = await LoadUnsafeAsync(cancellationToken).ConfigureAwait(false);
            long id = questions.Count == 0 ? 1 : questions.Max(item => item.Id) + 1;
            KnowledgeQuestion created = question with { Id = id };
            questions.Add(created);
            await PersistUnsafeAsync(questions, cancellationToken).ConfigureAwait(false);
            return created;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<KnowledgeQuestion?> GetAsync(long id, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return (await LoadUnsafeAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(item => item.Id == id);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<KnowledgeQuestionPage> SearchAsync(
        string query,
        string category,
        QuestionPublicationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            IEnumerable<KnowledgeQuestion> filtered = await LoadUnsafeAsync(cancellationToken).ConfigureAwait(false);
            if (status.HasValue)
                filtered = filtered.Where(item => item.PublicationStatus == status.Value);
            if (!string.IsNullOrWhiteSpace(category))
                filtered = filtered.Where(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(query))
            {
                string search = query.Trim();
                filtered = filtered.Where(item =>
                    item.OriginalQuestion.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || item.NormalizedQuestion.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || item.Summary.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || item.Tags.Any(tag => tag.Value.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }
            KnowledgeQuestion[] ordered = filtered.OrderByDescending(item => item.CreatedAt).ToArray();
            return new KnowledgeQuestionPage(
                ordered.Skip((page - 1) * pageSize).Take(pageSize).ToArray(),
                page,
                pageSize,
                ordered.Length);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(KnowledgeQuestion question, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(question);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            List<KnowledgeQuestion> questions = await LoadUnsafeAsync(cancellationToken).ConfigureAwait(false);
            int index = questions.FindIndex(item => item.Id == question.Id);
            if (index < 0)
                throw new KeyNotFoundException($"Knowledge question {question.Id} was not found.");
            questions[index] = question;
            await PersistUnsafeAsync(questions, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            List<KnowledgeQuestion> questions = await LoadUnsafeAsync(cancellationToken).ConfigureAwait(false);
            int removed = questions.RemoveAll(item => item.Id == id);
            if (removed == 0)
                return false;
            await PersistUnsafeAsync(questions, cancellationToken).ConfigureAwait(false);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<KnowledgeQuestion>> LoadUnsafeAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
            return _cache;
        if (!File.Exists(_path))
            return _cache = [];
        await using FileStream stream = new(
            _path, FileMode.Open, FileAccess.Read, FileShare.Read,
            32 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        QuestionStoreDocument? document = await JsonSerializer.DeserializeAsync<QuestionStoreDocument>(
            stream, JsonOptions, cancellationToken).ConfigureAwait(false);
        return _cache = document?.Questions ?? [];
    }

    private async Task PersistUnsafeAsync(
        List<KnowledgeQuestion> questions,
        CancellationToken cancellationToken)
    {
        string temporary = _path + ".tmp";
        await using (FileStream stream = new(
            temporary, FileMode.Create, FileAccess.Write, FileShare.None,
            32 * 1024, FileOptions.Asynchronous | FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                new QuestionStoreDocument { Questions = questions },
                JsonOptions,
                cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        File.Move(temporary, _path, true);
        _cache = questions;
    }

    private sealed class QuestionStoreDocument
    {
        public int SchemaVersion { get; set; } = 1;
        public List<KnowledgeQuestion> Questions { get; set; } = [];
    }
}
