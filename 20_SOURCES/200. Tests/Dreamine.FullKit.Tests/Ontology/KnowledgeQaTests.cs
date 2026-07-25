using DreamineWeb.KnowledgeQa.Application;
using DreamineWeb.KnowledgeQa.Domain;
using DreamineWeb.KnowledgeQa.Infrastructure;
using DreamineWeb.Models;
using DreamineWeb.Ontology.Application;
using DreamineWeb.Ontology.Domain;
using DreamineWeb.Ontology.Infrastructure;
using DreamineWeb.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Dreamine.FullKit.Tests.Ontology;

/// <summary>Verifies the evidence-first Q&amp;A vertical slice and its delivery-independent contracts.</summary>
public sealed class KnowledgeQaTests
{
    private readonly ITestOutputHelper _output;

    public KnowledgeQaTests(ITestOutputHelper output) => _output = output;

    [Fact]
    [Trait("Category", "GeneratedOntology")]
    public async Task EvidenceQuery_FindsSourceVerifiedDreamineEventForwardingAndDoxygen()
    {
        string root = FindRepositoryRoot();
        JsonOntologyRepository repository = new(
            new FixedOntologyDataPathResolver(Path.Combine(root, ".ua", "ontology")));
        OntologySourceService source = new(
            repository,
            new FixedOntologySourcePathResolver(Path.Combine(
                root,
                "20_SOURCES", "000. Project", "010. App", "Dreamine.Web",
                "wwwroot", "understand", "source")));
        string webRoot = Path.Combine(root, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot");
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Documentation:DoxygenRoot"] = Path.Combine(root, "10_DOCUMENTS", "Doxygen")
        }).Build();
        DocumentationCatalogService catalog = new(new TestWebHostEnvironment(webRoot));
        KnowledgeEvidenceBundleBuilder query = new(
            repository,
            new OntologyRelationResolver(),
            source,
            new DoxygenXmlEvidenceProvider(catalog, configuration),
            new KnowledgeQaOptions());

        EvidenceBundle bundle = await query.QueryAsync(
            new KnowledgeEvidenceQuery("SampleSmart에서 MainWindowViewModel.Ok는 어디로 전달되나요?", "ko"),
            CancellationToken.None);

        EvidenceReference[] forwardings = bundle.Evidence.Where(item =>
            item.Kind == EvidenceKind.OntologyRelation && item.RelationType == "forwardsTo").ToArray();
        Assert.Contains(forwardings, item =>
            item.Title.Contains("MainWindowViewModel.Ok", StringComparison.Ordinal)
            && item.Title.Contains("MainWindowEvent.Ok", StringComparison.Ordinal));
        EvidenceReference forwarding = forwardings.First(item =>
            item.Title.Contains("MainWindowViewModel.Ok", StringComparison.Ordinal)
            && item.Title.Contains("MainWindowEvent.Ok", StringComparison.Ordinal));
        Assert.Equal("calls", forwarding.ProjectionType);
        Assert.Equal(EvidenceOrigin.Direct, forwarding.Origin);
        Assert.Contains(bundle.Evidence, item =>
            item.Kind == EvidenceKind.Doxygen
            && item.Title.Contains("MainWindowViewModel.Ok", StringComparison.Ordinal)
            && item.DoxygenUrl?.Contains("/998.%20DEMO/SampleSmart/KR/html/", StringComparison.Ordinal) == true);
        Assert.Contains(bundle.Evidence, item =>
            item.Kind == EvidenceKind.Source
            && item.Title.Contains("Ok source declaration", StringComparison.Ordinal)
            && item.CodeExcerpt?.Contains("partial void Ok", StringComparison.Ordinal) == true);
        EvidenceReference okSource = Assert.Single(bundle.Evidence, item =>
            item.Kind == EvidenceKind.Source
            && item.Title.Contains("Ok source declaration", StringComparison.Ordinal)
            && item.CodeExcerpt?.Contains("DreamineCommand(\"Event.Ok\")", StringComparison.Ordinal) == true);
        Assert.Contains("DreamineCommand(\"Event.Ok\")", okSource.CodeExcerpt, StringComparison.Ordinal);
        Assert.DoesNotContain("Cancel", okSource.CodeExcerpt, StringComparison.Ordinal);
        Assert.Equal(66, okSource.LineStart);
        Assert.Equal(67, okSource.LineEnd);
        EvidenceReference okDoxygen = Assert.Single(bundle.Evidence, item =>
            item.Kind == EvidenceKind.Doxygen
            && item.Title.Equals("SampleSmart.Pages.MainWindowViewModel.Ok", StringComparison.Ordinal));
        Assert.True(okDoxygen.DoxygenUrlValidated);
        string doxygenRelativePath = Uri.UnescapeDataString(
            okDoxygen.DoxygenUrl!["/docs/doxygen/".Length..].Split('#')[0]);
        Assert.True(File.Exists(Path.Combine(root, "10_DOCUMENTS", "Doxygen", doxygenRelativePath)));
        Assert.Contains("SampleSmart", bundle.Version.ProjectVersions.Single(item => item.Contains("SampleSmart", StringComparison.Ordinal)));
    }

    [Fact]
    [Trait("Category", "GeneratedOntology")]
    public async Task RepositoryButtonQuestion_SuppliesCoherentXamlAndViewModelExamples()
    {
        string root = FindRepositoryRoot();
        CapturingRepositoryCodexRunner runner = new(root, """
        {
          "summary": "실제 실행 코드는 Event 클래스에 작성하고 ViewModel command와 XAML binding으로 연결합니다.",
          "sections": [
            { "heading": "실행 흐름", "body": "버튼에서 생성 command를 거쳐 Event 메서드를 호출합니다.", "sourceIndexes": [1, 2, 3] }
          ],
          "sources": [
            {
              "title": "CounterEvent.Increment",
              "summary": "실제 증가 동작입니다.",
              "sourcePath": "20_SOURCES/998. DEMO/000. Sample/050. CrossUi/SampleCrossUi.Shared/ViewModels/CounterEvent.cs",
              "lineStart": 96,
              "lineEnd": 101,
              "declaration": "public void Increment()"
            },
            {
              "title": "CounterViewModel.Increment",
              "summary": "Event 메서드로 전달되는 command 선언입니다.",
              "sourcePath": "20_SOURCES/998. DEMO/000. Sample/050. CrossUi/SampleCrossUi.Shared/ViewModels/CounterViewModel.cs",
              "lineStart": 57,
              "lineEnd": 58,
              "declaration": "[DreamineCommand(\"Event.Increment\")]"
            },
            {
              "title": "CounterView Increment button",
              "summary": "생성된 IncrementCommand를 바인딩합니다.",
              "sourcePath": "20_SOURCES/998. DEMO/000. Sample/050. CrossUi/SampleCrossUi.Wpf/Views/CounterView.xaml",
              "lineStart": 30,
              "lineEnd": 36,
              "declaration": "Command=\"{Binding IncrementCommand}\""
            }
          ],
          "relatedComponents": ["CounterEvent", "CounterViewModel", "CounterView"],
          "unverifiedStatements": []
        }
        """);
        KnowledgeQaOptions options = new() { RepositoryRoot = root };
        CodexRepositoryKnowledgeAnswerGenerator generator = new(runner, options);

        RepositoryKnowledgeAnswerResult result = await generator.GenerateAsync(
            "Dreamine으로 만든 화면에서 버튼을 눌렀을 때 실행할 코드는 어디에 작성해야 하나요?",
            "ko",
            CancellationToken.None);

        Assert.Contains("CounterView.xaml", runner.InputJson, StringComparison.Ordinal);
        Assert.Contains("IncrementCommand", runner.InputJson, StringComparison.Ordinal);
        Assert.Contains("CounterViewModel.cs", runner.InputJson, StringComparison.Ordinal);
        Assert.Contains("DreamineCommand", runner.InputJson, StringComparison.Ordinal);
        Assert.True(result.EvidenceBundle.Coverage.Required);
        Assert.True(
            result.EvidenceBundle.Coverage.IsComplete,
            $"Missing: {string.Join(", ", result.EvidenceBundle.Coverage.MissingSteps)}; "
            + $"Chain: {result.EvidenceBundle.Coverage.Chain}");
        Assert.All(result.EvidenceBundle.Coverage.Steps, step =>
        {
            Assert.True(step.Covered, step.Label);
            Assert.NotEmpty(step.EvidenceIds);
        });
        string[] relationTypes = result.EvidenceBundle.Evidence
            .Select(item => item.RelationType ?? string.Empty)
            .ToArray();
        Assert.Contains("bindsCommand", relationTypes);
        Assert.Contains("generatesCommand", relationTypes);
        Assert.Contains("declaresCommandMethod", relationTypes);
        Assert.Contains("forwardsTo", relationTypes);
        Assert.Contains("targetMethod", relationTypes);
        Assert.All(result.EvidenceBundle.Evidence
            .Where(item => item.Provenance.Equals("repository-command-trace", StringComparison.Ordinal)), item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.SourcePath));
            Assert.True(item.LineStart > 0);
            Assert.True(item.LineEnd >= item.LineStart);
        });
    }

    [Fact]
    [Trait("Category", "GeneratedOntology")]
    public async Task RepositoryAnswer_InvalidSourceRetainsUnverifiedDraftWithoutEvidence()
    {
        string root = FindRepositoryRoot();
        CapturingRepositoryCodexRunner runner = new(root, """
        {
          "summary": "검증되지는 않았지만 사용자가 요청하면 볼 수 있는 초안입니다.",
          "sections": [
            { "heading": "추정 흐름", "body": "이 내용은 소스 근거가 확인되지 않았습니다.", "sourceIndexes": [1] }
          ],
          "sources": [
            {
              "title": "Missing.Handler",
              "summary": "존재하지 않는 파일입니다.",
              "sourcePath": "20_SOURCES/Missing/DoesNotExist.cs",
              "lineStart": 10,
              "lineEnd": 20,
              "declaration": "void Handler()"
            }
          ],
          "relatedComponents": [],
          "unverifiedStatements": []
        }
        """);
        CodexRepositoryKnowledgeAnswerGenerator generator = new(
            runner,
            new KnowledgeQaOptions { RepositoryRoot = root });

        RepositoryKnowledgeAnswerResult result = await generator.GenerateAsync(
            "Dreamine 서버의 Missing.Handler 구현은 어디에 있나요?",
            "ko",
            CancellationToken.None);

        Assert.Equal("repository-search-gate", result.Answer.ModelId);
        Assert.Equal("no-valid-sources", result.Answer.Diagnostics.FallbackReason);
        Assert.Empty(result.EvidenceBundle.Evidence);
        KnowledgeAnswerContent draft = Assert.IsType<KnowledgeAnswerContent>(result.UnverifiedDraft);
        Assert.Contains("사용자가 요청하면 볼 수 있는 초안", draft.Summary, StringComparison.Ordinal);
        Assert.Single(draft.Sections);
        Assert.Empty(draft.EvidenceIds);
    }

    [Fact]
    [Trait("Category", "GeneratedOntology")]
    public async Task EvidenceQuery_ExactViewModelBaseImplementsUsesOnlyOutgoingImplements()
    {
        const string question = "Dreamine.MVVM.ViewModels.ViewModelBase가 직접 구현하는 INotifyPropertyChanged와 INotifyPropertyChanging을 찾아주세요.";
        EvidenceBundle bundle = await CreateRealEvidenceQuery().QueryAsync(
            new KnowledgeEvidenceQuery(question, "ko"), CancellationToken.None);

        KnowledgeRelationConstraint constraint = Assert.Single(bundle.RetrievalDiagnostics.RelationConstraints);
        Assert.Equal("implements", constraint.RelationType, ignoreCase: true);
        Assert.Equal(KnowledgeRelationDirection.Outgoing, constraint.Direction);
        Assert.Equal("Dreamine.MVVM.ViewModels.ViewModelBase", constraint.AnchorSymbol);

        EvidenceReference[] relations = bundle.Evidence
            .Where(item => item.Kind == EvidenceKind.OntologyRelation)
            .ToArray();
        Assert.Equal(2, relations.Length);
        Assert.All(relations, item => Assert.Equal("implements", item.RelationType, ignoreCase: true));
        Assert.All(relations, item => Assert.Contains(
            "Dreamine.MVVM.ViewModels.ViewModelBase → implements →",
            item.Title,
            StringComparison.Ordinal));
        Assert.Contains(relations, item => item.Title.Contains("INotifyPropertyChanged", StringComparison.Ordinal));
        Assert.Contains(relations, item => item.Title.Contains("INotifyPropertyChanging", StringComparison.Ordinal));
        Assert.DoesNotContain(relations, item => item.RelationType?.Equals("inherits", StringComparison.OrdinalIgnoreCase) == true);

        KnowledgeQuestion storedQuestion = CreateQuestion(bundle) with
        {
            OriginalQuestion = question,
            NormalizedQuestion = question
        };
        KnowledgeAnswerViewModel answer = new KnowledgeAnswerProjectionService().Project(
            storedQuestion, CreateRevision(bundle));
        Assert.DoesNotContain(answer.CoreEvidence, item =>
            item.RelationType?.Equals("inherits", StringComparison.OrdinalIgnoreCase) == true);
        KnowledgeEvidenceCardViewModel[] relationCards = answer.CoreEvidence
            .Where(item => item.RelationType is not null)
            .ToArray();
        Assert.Equal(2, relationCards.Length);
        Assert.All(relationCards, item =>
            Assert.Equal("implements", item.RelationType, ignoreCase: true));
        Assert.Contains("INotifyPropertyChanged", answer.DirectAnswer, StringComparison.Ordinal);
        Assert.Contains("INotifyPropertyChanging", answer.DirectAnswer, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "GeneratedOntology")]
    public async Task EvidenceQuery_TypesInheritingViewModelBaseUsesIncomingInherits()
    {
        const string question = "Dreamine.MVVM.ViewModels.ViewModelBase를 상속하는 타입을 찾아주세요.";
        EvidenceBundle bundle = await CreateRealEvidenceQuery().QueryAsync(
            new KnowledgeEvidenceQuery(question, "ko"), CancellationToken.None);

        KnowledgeRelationConstraint constraint = Assert.Single(bundle.RetrievalDiagnostics.RelationConstraints);
        Assert.Equal("inherits", constraint.RelationType, ignoreCase: true);
        Assert.Equal(KnowledgeRelationDirection.Incoming, constraint.Direction);
        Assert.Equal("Dreamine.MVVM.ViewModels.ViewModelBase", constraint.AnchorSymbol);
        EvidenceReference[] relations = bundle.Evidence
            .Where(item => item.Kind == EvidenceKind.OntologyRelation)
            .ToArray();
        Assert.NotEmpty(relations);
        Assert.All(relations, item => Assert.Equal("inherits", item.RelationType, ignoreCase: true));
        Assert.All(relations, item => Assert.EndsWith(
            "→ inherits → Dreamine.MVVM.ViewModels.ViewModelBase",
            item.Title,
            StringComparison.Ordinal));
        Assert.Contains(relations, item => item.Title.Contains("MainWindowViewModel", StringComparison.Ordinal));
        Assert.DoesNotContain(relations, item => item.RelationType?.Equals("implements", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    [Trait("Category", "GeneratedOntology")]
    public async Task RuntimeRepository_ViewModelBaseReturnsTwoOutgoingImplementsFromGeneratedArtifacts()
    {
        string root = FindRepositoryRoot();
        string[] artifactDirectories =
        [
            Path.Combine(root, ".ua", "ontology"),
            Path.Combine(root, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web",
                "wwwroot", "understand", "ontology")
        ];
        foreach (string artifactDirectory in artifactDirectories)
        {
            JsonOntologyRepository repository = new(
                new FixedOntologyDataPathResolver(artifactDirectory));
            OntologyPage<OntologyNode> page = await repository.SearchNodesAsync(
                new OntologyQuery("Dreamine.MVVM.ViewModels.ViewModelBase"),
                1,
                100,
                CancellationToken.None);
            OntologyNode viewModelBase = Assert.Single(page.Items, node =>
                node.QualifiedName.Equals(
                    "Dreamine.MVVM.ViewModels.ViewModelBase",
                    StringComparison.Ordinal));

            IReadOnlyList<OntologyRelation> allRelations = await repository.GetRelationsAsync(
                viewModelBase.StableUri,
                CancellationToken.None);
            OntologyRelation[] outgoingImplements = allRelations.Where(relation =>
                relation.SourceUri == viewModelBase.StableUri
                && relation.OriginalType.Equals("implements", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.Equal(2, outgoingImplements.Length);

            IReadOnlyDictionary<string, OntologyNode> targets = await repository.GetNodesAsync(
                outgoingImplements.Select(relation => relation.TargetUri),
                CancellationToken.None);
            Assert.Equal(
                [
                    "System.ComponentModel.INotifyPropertyChanged",
                    "System.ComponentModel.INotifyPropertyChanging"
                ],
                targets.Values.Select(node => node.QualifiedName).Order(StringComparer.Ordinal).ToArray());
        }
    }

    [Fact]
    [Trait("Category", "GeneratedOntology")]
    public async Task EvidenceQuery_NotifyPropertyChangedUsageReturnsAllIncomingImplements()
    {
        const string question = "INotifyPropertyChanged는 어떤 클래스에서 사용해요?";
        EvidenceBundle bundle = await CreateRealEvidenceQuery().QueryAsync(
            new KnowledgeEvidenceQuery(question, "ko"), CancellationToken.None);

        KnowledgeRelationConstraint constraint = Assert.Single(bundle.RetrievalDiagnostics.RelationConstraints);
        Assert.Equal("implements", constraint.RelationType, ignoreCase: true);
        Assert.Equal(KnowledgeRelationDirection.Incoming, constraint.Direction);
        Assert.Equal("INotifyPropertyChanged", constraint.AnchorSymbol);
        EvidenceReference[] relations = bundle.Evidence
            .Where(item => item.Kind == EvidenceKind.OntologyRelation)
            .ToArray();
        Assert.NotEmpty(relations);
        Assert.All(relations, item => Assert.Equal("implements", item.RelationType, ignoreCase: true));
        Assert.Contains(relations, item =>
            item.Title.Contains("Dreamine.MVVM.ViewModels.ViewModelBase", StringComparison.Ordinal)
            && item.Title.Contains("System.ComponentModel.INotifyPropertyChanged", StringComparison.Ordinal));

        KnowledgeQuestion storedQuestion = CreateQuestion(bundle) with
        {
            OriginalQuestion = question,
            NormalizedQuestion = question
        };
        KnowledgeAnswerViewModel answer = new KnowledgeAnswerProjectionService().Project(
            storedQuestion, CreateRevision(bundle));
        Assert.Equal(relations.Length, answer.CoreEvidence.Count(item =>
            item.RelationType?.Equals("implements", StringComparison.OrdinalIgnoreCase) == true));
        Assert.Contains("INotifyPropertyChanged", answer.DirectAnswer, StringComparison.Ordinal);
        Assert.Contains("ViewModelBase", answer.DirectAnswer, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "GeneratedOntology")]
    public async Task EvidenceQuery_WpfButtonQuestionExcludesUnrelatedActionSymbolsAndFlows()
    {
        string root = FindRepositoryRoot();
        JsonOntologyRepository repository = new(
            new FixedOntologyDataPathResolver(Path.Combine(root, ".ua", "ontology")));
        OntologySourceService source = new(
            repository,
            new FixedOntologySourcePathResolver(Path.Combine(
                root, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot", "understand", "source")));
        string webRoot = Path.Combine(root, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot");
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Documentation:DoxygenRoot"] = Path.Combine(root, "10_DOCUMENTS", "Doxygen")
        }).Build();
        KnowledgeQaOptions options = new();
        KnowledgeEvidenceBundleBuilder query = new(
            repository,
            new OntologyRelationResolver(),
            source,
            new DoxygenXmlEvidenceProvider(new DocumentationCatalogService(new TestWebHostEnvironment(webRoot)), configuration),
            options);

        EvidenceBundle bundle = await query.QueryAsync(
            new KnowledgeEvidenceQuery("WPF에 버튼은 어떻게 추가하나요?", "ko"), CancellationToken.None);
        KnowledgeScopeDecision decision = new KnowledgeRequestScopePolicy(options).EvaluateEvidence(bundle, "ko");

        string evidenceText = string.Join('\n', bundle.Evidence.Select(item =>
            $"{item.Title} {item.Summary} {item.SourcePath} {item.Declaration} {item.CodeExcerpt}"));
        Assert.DoesNotContain("AddChannel", evidenceText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AddItem", evidenceText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(bundle.Evidence, item =>
            item.Kind == EvidenceKind.Source
            && item.SourcePath?.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) == true
            && item.CodeExcerpt?.Contains("<Button", StringComparison.Ordinal) == true
            && item.CodeExcerpt?.Contains("Command=\"{Binding", StringComparison.Ordinal) == true);
        Assert.Contains(bundle.Evidence, item =>
            item.Kind == EvidenceKind.Source
            && item.SourcePath?.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) == true
            && item.CodeExcerpt?.Contains("Command", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(bundle.Evidence, item => item.Kind == EvidenceKind.OntologyRelation);
        Assert.DoesNotContain(bundle.Evidence, item =>
            item.Title.Contains("DreamineButton", StringComparison.OrdinalIgnoreCase)
            || item.Title.Contains("GetCommand", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(KnowledgeRequestDisposition.Supported, decision.Disposition);

        KnowledgeQuestion question = CreateQuestion(bundle) with
        {
            OriginalQuestion = bundle.Question,
            NormalizedQuestion = bundle.NormalizedQuestion
        };
        KnowledgeAnswerViewModel projection = new KnowledgeAnswerProjectionService().Project(
            question, CreateRevision(bundle));
        Assert.Empty(projection.Flow);
        Assert.DoesNotContain(projection.CoreEvidence, item =>
            item.Title.Contains("AddChannel", StringComparison.OrdinalIgnoreCase));
        if (bundle.Evidence.Count == 0)
        {
            Assert.Empty(projection.CoreEvidence);
            Assert.Contains("직접 관련된 저장소 근거를 찾지 못했습니다", projection.DirectAnswer, StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait("Category", "GeneratedOntology")]
    public async Task EvidenceQuery_GeneralListSelectionUsesCoherentXamlAndViewModelEvidence()
    {
        KnowledgeEvidenceBundleBuilder query = CreateRealEvidenceQuery();

        EvidenceBundle bundle = await query.QueryAsync(
            new KnowledgeEvidenceQuery(
                "Dreamine WPF에서 ComboBox의 ItemsSource와 SelectedItem을 ViewModel에 바인딩하는 실제 예제를 알려주세요.",
                "ko"),
            CancellationToken.None);

        string evidenceText = string.Join('\n', bundle.Evidence.Select(item =>
            $"{item.Title} {item.SourcePath} {item.CodeExcerpt}"));
        EvidenceReference xaml = Assert.Single(bundle.Evidence, item =>
            item.Kind == EvidenceKind.Source
            && item.SourcePath?.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) == true
            && item.CodeExcerpt?.Contains("ComboBox", StringComparison.OrdinalIgnoreCase) == true
            && item.CodeExcerpt?.Contains("ItemsSource=\"{Binding", StringComparison.Ordinal) == true
            && item.CodeExcerpt?.Contains("SelectedItem=\"{Binding", StringComparison.Ordinal) == true);
        EvidenceReference[] csharp = bundle.Evidence.Where(item =>
            item.Kind == EvidenceKind.Source
            && item.SourcePath?.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) == true).ToArray();
        Assert.NotEmpty(csharp);
        Assert.All(csharp, item => Assert.Equal(
            ProjectBoundary(xaml.SourcePath!),
            ProjectBoundary(item.SourcePath!)));
        Assert.Contains(csharp, item =>
            item.CodeExcerpt?.Contains("Selected", StringComparison.OrdinalIgnoreCase) == true
            || item.CodeExcerpt?.Contains("Items", StringComparison.OrdinalIgnoreCase) == true
            || item.CodeExcerpt?.Contains("Slugs", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain("AddChannel", evidenceText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AdminViewModel", evidenceText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(bundle.Evidence, item => item.Kind == EvidenceKind.OntologyRelation);
    }

    [Fact]
    [Trait("Category", "GeneratedOntology")]
    public async Task EvidenceQuery_GeneralTextInputProvesPolicyIsNotOverfitToListSelection()
    {
        KnowledgeEvidenceBundleBuilder query = CreateRealEvidenceQuery();

        EvidenceBundle bundle = await query.QueryAsync(
            new KnowledgeEvidenceQuery(
                "Dreamine WPF에서 TextBox 입력을 Text 바인딩으로 ViewModel에 연결한 저장소 예제를 찾아주세요.",
                "ko"),
            CancellationToken.None);

        string evidenceText = string.Join('\n', bundle.Evidence.Select(item =>
            $"{item.Title} {item.SourcePath} {item.CodeExcerpt}"));
        Assert.Contains(bundle.Evidence, item =>
            item.Kind == EvidenceKind.Source
            && item.SourcePath?.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) == true
            && item.CodeExcerpt?.Contains("Text=\"{Binding", StringComparison.Ordinal) == true);
        Assert.Contains(bundle.Evidence, item =>
            item.Kind == EvidenceKind.Source
            && item.SourcePath?.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain("AddChannel", evidenceText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AdminViewModel", evidenceText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "GeneratedOntology")]
    public async Task NaturalUsageQuestions_ProduceStructuredPlansAndAvoidGenericButtonTypes()
    {
        (string Question, string Intent, string Concept, string? Project, string? Symbol)[] cases =
        [
            ("Dreamine에서 버튼을 누르면 원하는 코드를 실행하게 하려면?", "button-command", "DreamineCommand", null, null),
            ("입력한 글자가 바뀌면 화면에도 바로 반영되게 하려면?", "input-binding", "INotifyPropertyChanged", null, null),
            ("콤보박스에 목록을 넣고 선택한 값을 가져오려면?", "items-selection", "ItemsSource", null, null),
            ("새 창이나 팝업을 띄우려면?", "window-popup", "Popup", null, null),
            ("SampleSmart에서 채널 추가 버튼은 어디에서 처리?", "channel-action", "AddChannel", "SampleSmart", "AddChannel"),
            ("버튼 클릭으로 ViewModel 명령을 실행하려면?", "button-command", "Button.Command", null, null),
            ("텍스트 입력값을 ViewModel과 양방향으로 연결하려면?", "input-binding", "Binding", null, null),
            ("목록에서 선택한 항목을 ViewModel에서 받으려면?", "items-selection", "SelectedItem", null, null)
        ];
        KnowledgeQaOptions options = new();
        KnowledgeRequestScopePolicy scope = new(options);
        KnowledgeEvidenceBundleBuilder query = CreateRealEvidenceQuery();

        foreach ((string question, string expectedIntent, string expectedConcept, string? expectedProject, string? expectedSymbol) in cases)
        {
            Assert.Equal(KnowledgeRequestDisposition.Supported,
                scope.EvaluateQuestion(question, "ko").Disposition);

            EvidenceBundle bundle = await query.QueryAsync(
                new KnowledgeEvidenceQuery(question, "ko"), CancellationToken.None);

            Assert.Equal(expectedIntent, bundle.RetrievalDiagnostics.Intent);
            Assert.Contains(expectedConcept, bundle.RetrievalDiagnostics.Concepts, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(expectedProject ?? string.Empty, bundle.RetrievalDiagnostics.Project);
            if (expectedSymbol is not null)
                Assert.Contains(expectedSymbol, bundle.RetrievalDiagnostics.ExactSymbols, StringComparer.OrdinalIgnoreCase);
            Assert.NotEmpty(bundle.RetrievalDiagnostics.SourceKinds);
            Assert.NotEmpty(bundle.RetrievalDiagnostics.ServerRequests);
            Assert.NotEmpty(bundle.Evidence);
            Assert.DoesNotContain(bundle.RetrievalDiagnostics.ServerRequests,
                request => request.Purpose.Equals("raw-fallback", StringComparison.OrdinalIgnoreCase));
            Assert.All(bundle.RetrievalDiagnostics.ServerRequests, request =>
                Assert.True(
                    bundle.RetrievalDiagnostics.Concepts.Contains(request.Query, StringComparer.OrdinalIgnoreCase)
                    || bundle.RetrievalDiagnostics.ExactSymbols.Contains(request.Query, StringComparer.OrdinalIgnoreCase)
                    || bundle.RetrievalDiagnostics.SearchTerms.Contains(request.Query, StringComparer.OrdinalIgnoreCase)));

            string coreText = string.Join('\n', bundle.Evidence
                .Where(item => item.Kind is EvidenceKind.OntologyNode or EvidenceKind.OntologyRelation)
                .Select(item => $"{item.Title} {item.Summary} {item.Declaration}"));
            Assert.DoesNotContain("RadioButton", coreText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("KeyButton", coreText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotMatch(@"(?:^|\s)Button(?:\s|$)", coreText);
            if (expectedSymbol is not null)
            {
                Assert.Contains(bundle.Evidence, item =>
                    item.Kind == EvidenceKind.OntologyRelation
                    && item.RelationType?.Equals("forwardsTo", StringComparison.OrdinalIgnoreCase) == true
                    && item.Title.Contains(expectedSymbol, StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    [Trait("Category", "GeneratedOntology")]
    public async Task EvidenceQuery_AddChannelSpecificQuestionKeepsItsForwarding()
    {
        string root = FindRepositoryRoot();
        JsonOntologyRepository repository = new(
            new FixedOntologyDataPathResolver(Path.Combine(root, ".ua", "ontology")));
        OntologySourceService source = new(
            repository,
            new FixedOntologySourcePathResolver(Path.Combine(
                root, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot", "understand", "source")));
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Documentation:DoxygenRoot"] = Path.Combine(root, "10_DOCUMENTS", "Doxygen")
        }).Build();
        KnowledgeEvidenceBundleBuilder query = new(
            repository,
            new OntologyRelationResolver(),
            source,
            new DoxygenXmlEvidenceProvider(
                new DocumentationCatalogService(new TestWebHostEnvironment(Path.Combine(
                    root, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot"))),
                configuration),
            new KnowledgeQaOptions());

        OntologySymbolScopeResolver scopeResolver = new(repository);
        KnowledgeSymbolScopeResolution resolution = await scopeResolver.ResolveAsync(
            "AddChannel은 어디로 전달되나요?", CancellationToken.None);
        Assert.Equal(KnowledgeSymbolScopeResolutionKind.Exact, resolution.Kind);
        Assert.Equal("AddChannel", resolution.Symbol);
        Assert.Equal(1, resolution.ForwardingCandidateCount);
        Assert.Equal(KnowledgeSymbolScopeResolutionKind.None, (await scopeResolver.ResolveAsync(
            "DefinitelyMissingPascalCase는 어디로 전달되나요?", CancellationToken.None)).Kind);
        Assert.Equal(KnowledgeSymbolScopeResolutionKind.None, (await scopeResolver.ResolveAsync(
            "오늘 날씨는 어때요?", CancellationToken.None)).Kind);

        EvidenceBundle bundle = await query.QueryAsync(
            new KnowledgeEvidenceQuery("AddChannel은 어디로 전달되나요?", "ko"), CancellationToken.None);

        Assert.Contains(bundle.Evidence, item =>
            item.Kind == EvidenceKind.OntologyRelation
            && item.RelationType?.Equals("forwardsTo", StringComparison.OrdinalIgnoreCase) == true
            && item.Title.Contains("AddChannel", StringComparison.Ordinal));
        Assert.Equal(KnowledgeRequestDisposition.Supported,
            new KnowledgeRequestScopePolicy(new KnowledgeQaOptions()).EvaluateEvidence(bundle, "ko").Disposition);

        string directory = Path.Combine(Path.GetTempPath(), $"dreamine-add-channel-scope-{Guid.NewGuid():N}");
        try
        {
            KnowledgeQaService service = new(
                new JsonKnowledgeQuestionRepository(new DreamineOptions { DataPath = directory }),
                new FixedEvidenceQuery(bundle),
                new FixedAnswerGenerator(),
                new KnowledgeRequestScopePolicy(new KnowledgeQaOptions()),
                new KnowledgeAnswerProjectionService(),
                new KnowledgePrivacyScanner(),
                new FixedValidationService(bundle.Version),
                scopeResolver);
            KnowledgeQuestionCreatedViewModel created = await service.AskAsync(
                new KnowledgeQuestionRequest("AddChannel은 어디로 전달되나요?", "ko"), CancellationToken.None);
            Assert.Equal(KnowledgeRequestDisposition.Supported, created.RequestDisposition);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task ExactSymbolScope_MultipleProjectsWithoutUniqueForwardingNeedsClarification()
    {
        OntologyNode first = CreateSymbolNode("urn:test:first", "AddChannel", "Project.One");
        OntologyNode second = CreateSymbolNode("urn:test:second", "AddChannel", "Project.Two");
        OntologySymbolScopeResolver resolver = new(new SymbolTestOntologyRepository([first, second], []));

        KnowledgeSymbolScopeResolution resolution = await resolver.ResolveAsync(
            "AddChannel은 어디로 전달되나요?", CancellationToken.None);

        Assert.Equal(KnowledgeSymbolScopeResolutionKind.Ambiguous, resolution.Kind);
        Assert.Equal(2, resolution.ExactNodeCount);
        Assert.Equal(0, resolution.ForwardingCandidateCount);

        string directory = Path.Combine(Path.GetTempPath(), $"dreamine-ambiguous-symbol-{Guid.NewGuid():N}");
        try
        {
            CountingEvidenceQuery evidence = new(CreateBundle());
            CountingAnswerGenerator generator = new();
            KnowledgeQaService service = new(
                new JsonKnowledgeQuestionRepository(new DreamineOptions { DataPath = directory }),
                evidence,
                generator,
                new KnowledgeRequestScopePolicy(new KnowledgeQaOptions()),
                new KnowledgeAnswerProjectionService(),
                new KnowledgePrivacyScanner(),
                new FixedValidationService(CreateBundle().Version),
                new FixedSymbolScopeResolver(resolution));

            KnowledgeQuestionCreatedViewModel created = await service.AskAsync(
                new KnowledgeQuestionRequest("AddChannel은 어디로 전달되나요?", "ko"), CancellationToken.None);

            Assert.Equal(KnowledgeRequestDisposition.NeedsClarification, created.RequestDisposition);
            Assert.Equal(0, evidence.CallCount);
            Assert.Equal(0, generator.CallCount);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task LanguageModel_UsesConfiguredGemmaModelAndRejectsUnknownCitations()
    {
        CapturingHandler handler = new("""
            {
              "choices": [{
                "message": {
                  "content": "{\"summary\":\"Ok는 Event Component로 전달됩니다.\",\"sections\":[{\"heading\":\"전달 흐름\",\"body\":\"검증된 forwardsTo 관계입니다.\",\"evidenceIds\":[\"relation-1\",\"invented\"]}],\"relatedComponents\":[\"MainWindowViewModel.Ok\",\"MainWindowEvent.Ok\"],\"unverifiedStatements\":[],\"evidenceIds\":[\"relation-1\",\"invented\"]}"
                }
              }]
            }
            """);
        OpenAiCompatibleKnowledgeAnswerGenerator generator = new(
            new HttpClient(handler),
            new KnowledgeQaOptions
            {
                Endpoint = "http://lm-studio.test/v1/",
                Model = "gemma-3-4b-it"
            });

        KnowledgeAnswerGenerationResult result = await generator.GenerateAsync(
            CreateBundle(), CancellationToken.None);

        Assert.Equal("gemma-3-4b-it", result.ModelId);
        Assert.Equal(["relation-1"], result.Content.EvidenceIds);
        Assert.DoesNotContain("invented", result.Content.Sections.SelectMany(section => section.EvidenceIds));
        Assert.Contains("\"model\":\"gemma-3-4b-it\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("EvidenceBundle", handler.RequestBody, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("{\"choices\":[")]
    public async Task LanguageModel_EmptyOrMalformedEnvelopeFallsBackWithoutExposingInternalError(string response)
    {
        OpenAiCompatibleKnowledgeAnswerGenerator generator = CreateLanguageModelGenerator(
            new CapturingHandler(response));

        KnowledgeAnswerGenerationResult result = await generator.GenerateAsync(CreateBundle(), CancellationToken.None);

        Assert.Equal("deterministic-evidence", result.ModelId);
        Assert.Contains("전달됩니다", result.Content.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("JSON", result.Content.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Path:", result.Content.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LanguageModel_TruncatedAnswerJsonFallsBackToDeterministicAnswer()
    {
        string truncated = "{\"summary\":\"WPF 버튼\",\"sections\":[{\"heading\":\"추가\",\"body\":\"잘린 응답";
        OpenAiCompatibleKnowledgeAnswerGenerator generator = CreateLanguageModelGenerator(
            new CapturingHandler(CompletionPayload(truncated)));

        KnowledgeAnswerGenerationResult result = await generator.GenerateAsync(CreateBundle(), CancellationToken.None);

        Assert.Equal("deterministic-evidence", result.ModelId);
        Assert.DoesNotContain("Expected end", result.Content.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CodexCliProcessRunner_UsesBomlessUtf8AndPreservesKoreanPrompt()
    {
        KnowledgeQaOptions options = new() { CodexExecutable = "codex.exe" };
        using CodexCliProcessRunner runner = new(options);
        MethodInfo createStartInfo = Assert.IsAssignableFrom<MethodInfo>(typeof(CodexCliProcessRunner).GetMethod(
            "CreateStartInfo",
            BindingFlags.Instance | BindingFlags.NonPublic));
        ProcessStartInfo startInfo = Assert.IsType<ProcessStartInfo>(createStartInfo.Invoke(
            runner,
            [
                Path.GetTempPath(),
                Path.Combine(Path.GetTempPath(), "schema.json"),
                Path.Combine(Path.GetTempPath(), "answer.json"),
                "한국어 질문을 분석하세요"
            ]));

        Encoding[] encodings =
        [
            Assert.IsAssignableFrom<Encoding>(startInfo.StandardInputEncoding),
            Assert.IsAssignableFrom<Encoding>(startInfo.StandardOutputEncoding),
            Assert.IsAssignableFrom<Encoding>(startInfo.StandardErrorEncoding)
        ];
        foreach (Encoding encoding in encodings)
        {
            Assert.Equal(Encoding.UTF8.CodePage, encoding.CodePage);
            Assert.Empty(encoding.GetPreamble());
        }

        const string prompt = "Dreamine에서 메인 화면 명령 전달 흐름을 설명해 주세요";
        byte[] bytes = startInfo.StandardInputEncoding!.GetBytes(prompt);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        Assert.Equal(prompt, startInfo.StandardInputEncoding.GetString(bytes));
        Assert.DoesNotContain((byte)'?', bytes);
        Assert.EndsWith("codex.exe", startInfo.FileName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CodexPlanner_ReturnsIntentConceptsProjectRelationsDirectionsAndSourceKinds()
    {
        CapturingCodexRunner runner = new("""
            {
              "queryKind":"mixed",
              "intent":"channel-action",
              "concepts":["AddChannel","Command"],
              "searchTerms":["AddChannel"],
              "exactSymbols":["AddChannel"],
              "relationTypes":["forwardsTo","hasEventComponent"],
              "relationConstraints":[
                {"relationType":"forwardsTo","direction":"outgoing","anchorSymbol":"AddChannel"},
                {"relationType":"hasEventComponent","direction":"outgoing","anchorSymbol":"AddChannel"}
              ],
              "project":"SampleSmart",
              "sourceKinds":["Xaml","ViewModel","Event"],
              "suppressUnverifiedFlows":false
            }
            """);
        CodexCliKnowledgeQuestionPlanner planner = new(
            runner,
            new KnowledgeQaOptions { Enabled = true, IncludeDevelopmentDiagnostics = true });

        KnowledgeSearchPlan plan = await planner.PlanAsync(
            "SampleSmart에서 채널 추가 버튼은 어디에서 처리?", "ko", CancellationToken.None);

        Assert.Equal("channel-action", plan.Intent);
        Assert.Equal("SampleSmart", plan.Project);
        Assert.Contains("AddChannel", plan.ExactSymbols, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Command", plan.Concepts, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(["Xaml", "ViewModel", "Event"], plan.SourceKinds);
        Assert.Equal(2, plan.RelationConstraints.Count);
        Assert.All(plan.RelationConstraints, constraint =>
        {
            Assert.Equal(KnowledgeRelationDirection.Outgoing, constraint.Direction);
            Assert.Equal("AddChannel", constraint.AnchorSymbol);
        });
        Assert.Contains("sourceKinds", runner.OutputSchema, StringComparison.Ordinal);
        Assert.Contains("Never copy the", runner.Instruction, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LanguageModel_TimeoutOrConnectionFailureFallsBackToDeterministicAnswer(bool timeout)
    {
        Exception failure = timeout
            ? new TaskCanceledException("simulated timeout")
            : new HttpRequestException("simulated connection failure");
        OpenAiCompatibleKnowledgeAnswerGenerator generator = CreateLanguageModelGenerator(
            new FailureHandler(failure));

        KnowledgeAnswerGenerationResult result = await generator.GenerateAsync(CreateBundle(), CancellationToken.None);

        Assert.Equal("deterministic-evidence", result.ModelId);
        Assert.Contains("전달됩니다", result.Content.Summary, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"summary\":\"요약만 있음\"}")]
    public async Task LanguageModel_MissingRequiredFieldsFallsBackToDeterministicAnswer(string answer)
    {
        OpenAiCompatibleKnowledgeAnswerGenerator generator = CreateLanguageModelGenerator(
            new CapturingHandler(CompletionPayload(answer)));

        KnowledgeAnswerGenerationResult result = await generator.GenerateAsync(CreateBundle(), CancellationToken.None);

        Assert.Equal("deterministic-evidence", result.ModelId);
    }

    [Fact]
    public async Task LanguageModel_DoesNotCallEndpointWhenNoEvidenceExists()
    {
        CapturingHandler handler = new("{}");
        OpenAiCompatibleKnowledgeAnswerGenerator generator = new(
            new HttpClient(handler),
            new KnowledgeQaOptions { Endpoint = "http://lm-studio.test/v1/", Model = "gemma-3-4b-it" });
        EvidenceBundle empty = CreateBundle() with { Evidence = [] };

        KnowledgeAnswerGenerationResult result = await generator.GenerateAsync(empty, CancellationToken.None);

        Assert.Equal("evidence-gate", result.ModelId);
        Assert.Equal(0, handler.CallCount);
        Assert.NotEmpty(result.Content.UnverifiedStatements);
    }

    [Fact]
    public async Task VerticalSlice_PersistsPrivateUrlThenPublishesAndSearchesWithoutRegeneration()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dreamine-knowledge-qa-{Guid.NewGuid():N}");
        try
        {
            JsonKnowledgeQuestionRepository repository = new(new DreamineOptions { DataPath = directory });
            KnowledgeQaService service = new(
                repository,
                new FixedEvidenceQuery(CreateBundle()),
                new FixedAnswerGenerator(),
                new KnowledgeRequestScopePolicy(new KnowledgeQaOptions()),
                new KnowledgeAnswerProjectionService(),
                new KnowledgePrivacyScanner(),
                new FixedValidationService(CreateBundle().Version));

            KnowledgeQuestionCreatedViewModel created = await service.AskAsync(
                new KnowledgeQuestionRequest("MainWindowViewModel.Ok는 어디로 전달되나요?", "ko"),
                CancellationToken.None);

            Assert.Equal(QuestionPublicationStatus.PendingReview, created.PublicationStatus);
            Assert.StartsWith($"/questions/{created.Id}/", created.Url, StringComparison.Ordinal);
            Assert.Contains("?access=", created.Url, StringComparison.Ordinal);
            Assert.Null(await service.GetAsync(created.Id, null, false, CancellationToken.None));
            KnowledgeQuestionDetailsViewModel stored = Assert.IsType<KnowledgeQuestionDetailsViewModel>(
                await service.GetAsync(created.Id, created.AccessKey, false, CancellationToken.None));
            Assert.Equal("근거 기반 영구 답변", stored.Revision.Content.Summary);
            Assert.Equal("gemma-3-4b-it", stored.Revision.ModelId);
            Assert.Empty((await service.SearchPublishedAsync(
                "MainWindowViewModel", string.Empty, 1, 10, CancellationToken.None)).Items);
            Assert.Single(await service.GetAccessibleAsync(
                [new KnowledgeQuestionAccessReference(created.Id, created.Slug, created.AccessKey)],
                CancellationToken.None));

            Assert.True(await service.SetPublicationStatusAsync(
                created.Id, QuestionPublicationStatus.Published, CancellationToken.None));
            KnowledgeQuestionSearchViewModel published = await service.SearchPublishedAsync(
                "MainWindowViewModel", string.Empty, 1, 10, CancellationToken.None);
            Assert.Single(published.Items);
            Assert.NotNull(await service.GetAsync(created.Id, null, false, CancellationToken.None));

            JsonKnowledgeQuestionRepository reloadedRepository = new(new DreamineOptions { DataPath = directory });
            KnowledgeQuestion reloaded = Assert.IsType<KnowledgeQuestion>(
                await reloadedRepository.GetAsync(created.Id, CancellationToken.None));
            Assert.Single(reloaded.Answer.Revisions);
            Assert.Equal(stored.Revision.Content.Summary, reloaded.Answer.Revisions[0].Content.Summary);
            Assert.Equal(stored.Revision.Content.EvidenceIds, reloaded.Answer.Revisions[0].Content.EvidenceIds);
            Assert.Equal(stored.Revision.Content.Sections[0].Body, reloaded.Answer.Revisions[0].Content.Sections[0].Body);
            Assert.Single((await service.SearchForAdministrationAsync(
                1, 50, CancellationToken.None)).Items);
            Assert.True(await service.DeleteAsync(created.Id, CancellationToken.None));
            Assert.Null(await service.GetAsync(created.Id, created.AccessKey, true, CancellationToken.None));
            Assert.Empty((await service.SearchPublishedAsync(
                string.Empty, string.Empty, 1, 10, CancellationToken.None)).Items);
            JsonKnowledgeQuestionRepository afterDelete = new(new DreamineOptions { DataPath = directory });
            Assert.Null(await afterDelete.GetAsync(created.Id, CancellationToken.None));
            Assert.False(await service.DeleteAsync(created.Id, CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task RepositoryDirectAnswer_BypassesOntologyEvidenceAndPersistsVerifiedSource()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dreamine-repository-answer-{Guid.NewGuid():N}");
        try
        {
            CountingEvidenceQuery ontologyEvidence = new(CreateBundle());
            CountingAnswerGenerator evidenceOnlyAnswer = new();
            KnowledgeQaService service = new(
                new JsonKnowledgeQuestionRepository(new DreamineOptions { DataPath = directory }),
                ontologyEvidence,
                evidenceOnlyAnswer,
                new KnowledgeRequestScopePolicy(new KnowledgeQaOptions()),
                new KnowledgeAnswerProjectionService(),
                new KnowledgePrivacyScanner(),
                new FixedValidationService(CreateBundle().Version),
                repositoryAnswerGenerator: new FixedRepositoryAnswerGenerator());

            KnowledgeQuestionCreatedViewModel created = await service.AskAsync(
                new KnowledgeQuestionRequest("AddChannel은 실제로 어떤 코드를 실행하나요?", "ko"),
                CancellationToken.None);
            KnowledgeQuestionDetailsViewModel details = Assert.IsType<KnowledgeQuestionDetailsViewModel>(
                await service.GetAsync(created.Id, created.AccessKey, false, CancellationToken.None));

            Assert.Equal(0, ontologyEvidence.CallCount);
            Assert.Equal(0, evidenceOnlyAnswer.CallCount);
            Assert.Equal(QuestionPublicationStatus.PendingReview, created.PublicationStatus);
            EvidenceReference source = Assert.Single(details.Revision.EvidenceBundle.Evidence);
            Assert.Equal(EvidenceKind.Source, source.Kind);
            Assert.Equal("20_SOURCES/Sample/AddChannel.cs", source.SourcePath);
            Assert.Equal("Codex", details.Revision.ExecutionDiagnostics.StoredDirectAnswerProducer);
            Assert.Equal("저장소에서 AddChannel 구현을 찾아 실제 호출 흐름을 확인했습니다.", details.Answer.DirectAnswer);
            Assert.False(details.NeedsRevalidation);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task RepositoryDirectAnswer_IncompleteRequiredChainIsPartiallySupported()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dreamine-partial-chain-{Guid.NewGuid():N}");
        KnowledgeEvidenceCoverage coverage = new()
        {
            Required = true,
            Chain = "LaunchCommand → LaunchViewModel.Launch → ? forwardsTo → ? Event target method",
            Steps =
            [
                new("xaml-command-binding", "XAML Command Binding", true, ["source-1"]),
                new("generated-command", "generated Command", true, ["source-1"]),
                new("viewmodel-dreamine-command", "ViewModel DreamineCommand method", true, ["source-1"]),
                new("forwards-to", "forwardsTo", false, [], "No verified target declaration."),
                new("event-target-method", "Event target method", false, [], "No verified target method.")
            ]
        };
        try
        {
            KnowledgeQaService service = new(
                new JsonKnowledgeQuestionRepository(new DreamineOptions { DataPath = directory }),
                new CountingEvidenceQuery(CreateBundle()),
                new CountingAnswerGenerator(),
                new KnowledgeRequestScopePolicy(new KnowledgeQaOptions()),
                new KnowledgeAnswerProjectionService(),
                new KnowledgePrivacyScanner(),
                new FixedValidationService(CreateBundle().Version),
                repositoryAnswerGenerator: new FixedRepositoryAnswerGenerator(coverage));

            KnowledgeQuestionCreatedViewModel created = await service.AskAsync(
                new KnowledgeQuestionRequest("Dreamine 화면의 버튼 실행 코드는 어디에 작성하나요?", "ko"),
                CancellationToken.None);
            KnowledgeQuestionDetailsViewModel details = Assert.IsType<KnowledgeQuestionDetailsViewModel>(
                await service.GetAsync(created.Id, created.AccessKey, false, CancellationToken.None));

            Assert.Equal(KnowledgeRequestDisposition.PartiallySupported, created.RequestDisposition);
            Assert.Equal(QuestionPublicationStatus.Private, created.PublicationStatus);
            Assert.Contains("forwardsTo", details.ScopeReason, StringComparison.Ordinal);
            Assert.Contains("Event target method", details.ScopeReason, StringComparison.Ordinal);
            Assert.Equal(details.ScopeReason, details.Answer.DirectAnswer);
            Assert.Equal(["forwardsTo", "Event target method"],
                details.Revision.EvidenceBundle.Coverage.MissingSteps);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task RuleFallbackAnswer_IsPrivateAndCannotBePublished()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dreamine-knowledge-fallback-{Guid.NewGuid():N}");
        try
        {
            JsonKnowledgeQuestionRepository repository = new(new DreamineOptions { DataPath = directory });
            KnowledgeQaService service = new(
                repository,
                new FixedEvidenceQuery(CreateBundle()),
                new FallbackAnswerGenerator("start-failed"),
                new KnowledgeRequestScopePolicy(new KnowledgeQaOptions()),
                new KnowledgeAnswerProjectionService(),
                new KnowledgePrivacyScanner(),
                new FixedValidationService(CreateBundle().Version));

            KnowledgeQuestionCreatedViewModel created = await service.AskAsync(
                new KnowledgeQuestionRequest("MainWindowViewModel.Ok는 어디로 전달되나요?", "ko"),
                CancellationToken.None);

            Assert.Equal(KnowledgeRequestDisposition.Supported, created.RequestDisposition);
            Assert.Equal(QuestionPublicationStatus.Private, created.PublicationStatus);
            Assert.False(await service.SetPublicationStatusAsync(
                created.Id, QuestionPublicationStatus.Published, CancellationToken.None));
            Assert.Empty((await service.SearchForReviewAsync(1, 10, CancellationToken.None)).Items);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task ConcurrentSameSubmissionCreatesOneQuestionAndCallsGeneratorOnce()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dreamine-knowledge-dedup-{Guid.NewGuid():N}");
        try
        {
            DelayedAnswerGenerator generator = new();
            JsonKnowledgeQuestionRepository repository = new(new DreamineOptions { DataPath = directory });
            KnowledgeQaService service = CreateService(repository, generator);
            KnowledgeQuestionRequest request = new(
                "MainWindowViewModel.Ok는 어디로 전달되나요?", "ko", Guid.NewGuid());

            Task<KnowledgeQuestionCreatedViewModel> first = service.AskAsync(request, CancellationToken.None);
            await generator.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Task<KnowledgeQuestionCreatedViewModel> second = service.AskAsync(request, CancellationToken.None);
            generator.Release.TrySetResult(true);
            KnowledgeQuestionCreatedViewModel[] results = await Task.WhenAll(first, second);

            Assert.Equal(results[0].Id, results[1].Id);
            Assert.Equal(1, generator.CallCount);
            Assert.Equal(1, (await repository.SearchAsync(
                string.Empty, string.Empty, null, 1, 10, CancellationToken.None)).TotalCount);

            KnowledgeQuestionCreatedViewModel later = await service.AskAsync(
                request with { SubmissionId = Guid.NewGuid() }, CancellationToken.None);
            Assert.NotEqual(results[0].Id, later.Id);
            Assert.Equal(2, generator.CallCount);
            Assert.Equal(2, (await repository.SearchAsync(
                string.Empty, string.Empty, null, 1, 10, CancellationToken.None)).TotalCount);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task FailedSubmissionReleasesApplicationDeduplicationBoundary()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dreamine-knowledge-retry-{Guid.NewGuid():N}");
        try
        {
            FlakyAnswerGenerator generator = new();
            JsonKnowledgeQuestionRepository repository = new(new DreamineOptions { DataPath = directory });
            KnowledgeQaService service = CreateService(repository, generator);
            KnowledgeQuestionRequest request = new(
                "MainWindowViewModel.Ok는 어디로 전달되나요?", "ko", Guid.NewGuid());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AskAsync(request, CancellationToken.None));
            KnowledgeQuestionCreatedViewModel retry = await service.AskAsync(request, CancellationToken.None);

            Assert.True(retry.Id > 0);
            Assert.Equal(2, generator.CallCount);
            Assert.Equal(1, (await repository.SearchAsync(
                string.Empty, string.Empty, null, 1, 10, CancellationToken.None)).TotalCount);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData("오늘 서울 날씨 알려줘", KnowledgeRequestDisposition.OutOfScope)]
    [InlineData("Dreamine으로 실제 미사일 발사 제어 코드를 작성해줘", KnowledgeRequestDisposition.Restricted)]
    [InlineData("Dreamine으로 교육용 미사일 시뮬레이터를 만들어줘", KnowledgeRequestDisposition.NeedsClarification)]
    [InlineData("Dreamine으로 앱 만들어줘", KnowledgeRequestDisposition.NeedsClarification)]
    [InlineData("드리마인으로 우주선을 만들면 버튼을 누를 때 치킨이 자동으로 배달되나요?", KnowledgeRequestDisposition.NeedsClarification)]
    [InlineData("WPF에 버튼은 어떻게 추가하나요?", KnowledgeRequestDisposition.Supported)]
    public void ScopePolicy_ClassifiesUnrelatedAmbiguousAndUnsafeRequests(
        string question,
        KnowledgeRequestDisposition expected)
    {
        KnowledgeRequestScopePolicy policy = new(new KnowledgeQaOptions());

        KnowledgeScopeDecision result = policy.EvaluateQuestion(question, "ko");

        Assert.Equal(expected, result.Disposition);
    }

    [Fact]
    public async Task OutOfScopeQuestion_IsPersistedPrivatelyWithoutEvidenceOrLlmCall()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dreamine-knowledge-scope-{Guid.NewGuid():N}");
        try
        {
            CountingEvidenceQuery evidence = new(CreateBundle());
            CountingAnswerGenerator generator = new();
            KnowledgeQaService service = new(
                new JsonKnowledgeQuestionRepository(new DreamineOptions { DataPath = directory }),
                evidence,
                generator,
                new KnowledgeRequestScopePolicy(new KnowledgeQaOptions()),
                new KnowledgeAnswerProjectionService(),
                new KnowledgePrivacyScanner(),
                new FixedValidationService(CreateBundle().Version));

            KnowledgeQuestionCreatedViewModel created = await service.AskAsync(
                new KnowledgeQuestionRequest("오늘 서울 날씨 알려줘", "ko"), CancellationToken.None);

            Assert.Equal(KnowledgeRequestDisposition.OutOfScope, created.RequestDisposition);
            Assert.Equal(QuestionPublicationStatus.Private, created.PublicationStatus);
            Assert.Equal(0, evidence.CallCount);
            Assert.Equal(0, generator.CallCount);
            Assert.False(await service.SetPublicationStatusAsync(
                created.Id, QuestionPublicationStatus.Published, CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task SpeculativeDreamineOutcome_IsClarificationWithoutCodeEvidenceOrCodexCall()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dreamine-knowledge-speculative-{Guid.NewGuid():N}");
        try
        {
            CountingEvidenceQuery evidence = new(CreateBundle());
            CountingAnswerGenerator generator = new();
            KnowledgeQaService service = new(
                new JsonKnowledgeQuestionRepository(new DreamineOptions { DataPath = directory }),
                evidence,
                generator,
                new KnowledgeRequestScopePolicy(new KnowledgeQaOptions()),
                new KnowledgeAnswerProjectionService(),
                new KnowledgePrivacyScanner(),
                new FixedValidationService(CreateBundle().Version));

            KnowledgeQuestionCreatedViewModel created = await service.AskAsync(
                new KnowledgeQuestionRequest(
                    "드리마인으로 우주선을 만들면 버튼을 누를 때 치킨이 자동으로 배달되나요?",
                    "ko"),
                CancellationToken.None);

            Assert.Equal(KnowledgeRequestDisposition.NeedsClarification, created.RequestDisposition);
            Assert.Equal(QuestionPublicationStatus.Private, created.PublicationStatus);
            Assert.Equal(0, evidence.CallCount);
            Assert.Equal(0, generator.CallCount);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task EvidenceQuery_HonorsCancellationBeforeRetrieval()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        FixedEvidenceQuery query = new(CreateBundle());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            query.QueryAsync(new KnowledgeEvidenceQuery("cancel"), cancellation.Token));
    }

    [Fact]
    public void AnswerProjection_PrioritizesSampleSmartForwardingAndPreservesBundle()
    {
        EvidenceBundle bundle = CreateBundle() with
        {
            Evidence =
            [
                .. CreateBundle().Evidence,
                new EvidenceReference
                {
                    Id = "other-project-contains",
                    Kind = EvidenceKind.OntologyRelation,
                    Origin = EvidenceOrigin.Direct,
                    Title = "WeddingPlatform.MainWindowViewModel → contains → WeddingPlatform.MainWindowViewModel.Ok",
                    StableUri = "urn:wedding:viewmodel",
                    RelatedStableUri = "urn:wedding:ok",
                    RelationType = "contains",
                    Provenance = "ontology",
                    Confidence = 1d
                },
                new EvidenceReference
                {
                    Id = "source-1",
                    Kind = EvidenceKind.Source,
                    Origin = EvidenceOrigin.Direct,
                    Title = "Ok source declaration",
                    StableUri = "urn:test:viewmodel-ok",
                    SourcePath = "20_SOURCES/998. DEMO/000. Sample/010. Wpfs/SampleSmart/MainWindow.xaml.ViewModel.cs",
                    LineStart = 42,
                    LineEnd = 44,
                    Declaration = "private partial void Ok();",
                    CodeExcerpt = "[DreamineCommand(\"Event.Ok\")]\nprivate partial void Ok();",
                    Provenance = "source-mirror"
                }
            ]
        };
        KnowledgeQuestion question = CreateQuestion(bundle);
        AnswerRevision revision = CreateRevision(bundle);
        int before = bundle.Evidence.Count;

        KnowledgeAnswerViewModel result = new KnowledgeAnswerProjectionService().Project(question, revision);

        Assert.StartsWith("MainWindowViewModel.Ok", result.DirectAnswer, StringComparison.Ordinal);
        Assert.Contains("MainWindowEvent.Ok", result.DirectAnswer, StringComparison.Ordinal);
        Assert.Equal(["MainWindowViewModel.Ok", "MainWindowEvent.Ok"], result.Flow);
        Assert.DoesNotContain(result.CoreEvidence, item => item.Title.Contains("WeddingPlatform", StringComparison.Ordinal));
        Assert.Equal(before, bundle.Evidence.Count);
        Assert.Single(result.CoreEvidence, item => item.StableUri == "urn:test:viewmodel-ok" && item.RelationType == "forwardsTo");
    }

    [Fact]
    public void AnswerProjection_OutOfScopeHasNoEmptyEvidenceSections()
    {
        EvidenceBundle empty = CreateBundle() with { Evidence = [] };
        KnowledgeQuestion question = CreateQuestion(empty) with
        {
            OriginalQuestion = "오늘 날씨는 어때요?",
            RequestDisposition = KnowledgeRequestDisposition.OutOfScope,
            ScopeReason = "Dreamine과 관련 없는 질문입니다."
        };

        KnowledgeAnswerViewModel result = new KnowledgeAnswerProjectionService().Project(question, CreateRevision(empty));

        Assert.False(result.ShowEvidenceSections);
        Assert.Empty(result.CoreEvidence);
        Assert.Empty(result.AdditionalSections);
        Assert.Empty(result.UnverifiedStatements);
        Assert.Contains("Dreamine", result.ScopeGuidance, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisabledLanguageModel_ReturnsDeterministicKoreanAnswerWithoutHttpCall()
    {
        CapturingHandler handler = new("{}");
        OpenAiCompatibleKnowledgeAnswerGenerator generator = new(
            new HttpClient(handler),
            new KnowledgeQaOptions { Enabled = false, Endpoint = "http://lm-studio.test/v1/", Model = "gemma-3-4b-it" });

        KnowledgeAnswerGenerationResult result = await generator.GenerateAsync(CreateBundle(), CancellationToken.None);

        Assert.Equal(0, handler.CallCount);
        Assert.Equal("deterministic-evidence", result.ModelId);
        Assert.Contains("전달됩니다", result.Content.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void AnswerProjection_DoesNotExposeUnvalidatedDoxygenAnchor()
    {
        EvidenceBundle bundle = CreateBundle();
        KnowledgeAnswerViewModel result = new KnowledgeAnswerProjectionService().Project(
            CreateQuestion(bundle), CreateRevision(bundle));

        Assert.DoesNotContain(result.CoreEvidence, item => item.DoxygenUrl is not null);
    }

    [Fact]
    public void AnswerProjection_WpfButtonQuestionDistinguishesPlatformAndDreamineGuidanceInKorean()
    {
        EvidenceBundle bundle = CreateBundle() with
        {
            Question = "WPF에 버튼은 어떻게 추가하나요?",
            NormalizedQuestion = "WPF에 버튼은 어떻게 추가하나요?"
        };
        KnowledgeQuestion question = CreateQuestion(bundle) with
        {
            OriginalQuestion = bundle.Question,
            NormalizedQuestion = bundle.NormalizedQuestion
        };

        KnowledgeAnswerViewModel result = new KnowledgeAnswerProjectionService().Project(question, CreateRevision(bundle));

        Assert.Contains("XAML", result.DirectAnswer, StringComparison.Ordinal);
        Assert.Contains("Dreamine", result.DirectAnswer, StringComparison.Ordinal);
        Assert.Contains("Command", result.DirectAnswer, StringComparison.Ordinal);
        Assert.Contains("직접 관련된 저장소 근거를 찾지 못했습니다", result.DirectAnswer, StringComparison.Ordinal);
        Assert.Empty(result.Flow);
        Assert.Empty(result.CoreEvidence);
        Assert.False(result.ShowEvidenceSections);
        Assert.DoesNotMatch("Detailed analysis|English content", result.DirectAnswer);
    }

    [Fact]
    public void AnswerProjection_WpfButtonQuestionShowsOnlyActuallyRelatedDocumentation()
    {
        EvidenceBundle bundle = CreateBundle() with
        {
            Question = "WPF에 버튼은 어떻게 추가하나요?",
            NormalizedQuestion = "WPF에 버튼은 어떻게 추가하나요?",
            Evidence =
            [
                .. CreateBundle().Evidence,
                new EvidenceReference
                {
                    Id = "wpf-button-doc",
                    Kind = EvidenceKind.Doxygen,
                    Origin = EvidenceOrigin.Direct,
                    Title = "WPF XAML Button.Command Binding usage",
                    Summary = "A WPF Button binds its Command to an ICommand.",
                    DoxygenUrl = "/docs/doxygen/wpf-button.html",
                    DoxygenUrlValidated = true,
                    Provenance = "Doxygen XML",
                    Confidence = 1d
                }
            ]
        };
        KnowledgeQuestion question = CreateQuestion(bundle) with
        {
            OriginalQuestion = bundle.Question,
            NormalizedQuestion = bundle.NormalizedQuestion
        };

        KnowledgeAnswerViewModel result = new KnowledgeAnswerProjectionService().Project(
            question, CreateRevision(bundle));

        Assert.Empty(result.Flow);
        KnowledgeEvidenceCardViewModel card = Assert.Single(result.CoreEvidence);
        Assert.Contains("Button", card.Title, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(result.CoreEvidence, item =>
            item.Title.Contains("MainWindowViewModel.Ok", StringComparison.Ordinal));
    }

    [Fact]
    public void AnswerProjection_UsesPersistedCodexSummaryInsteadOfRuleTemplate()
    {
        EvidenceBundle bundle = CreateBundle();
        AnswerRevision revision = CreateRevision(bundle) with
        {
            Content = new KnowledgeAnswerContent(
                "Codex가 검증된 여러 근거를 종합한 최종 설명입니다.",
                [new KnowledgeAnswerSection("전체 흐름", "검증된 전달 흐름입니다.", ["relation-1"])],
                [],
                [],
                ["relation-1"]),
            ExecutionDiagnostics = new KnowledgeExecutionDiagnostics
            {
                ScopeDisposition = KnowledgeRequestDisposition.Supported,
                StoredDirectAnswerProducer = "Codex",
                AnswerGenerator = new KnowledgeAnswerGeneratorDiagnostics { Provider = "Codex" }
            }
        };

        KnowledgeAnswerViewModel result = new KnowledgeAnswerProjectionService().Project(
            CreateQuestion(bundle),
            revision);

        Assert.Equal("Codex가 검증된 여러 근거를 종합한 최종 설명입니다.", result.DirectAnswer);
        Assert.Contains(result.AdditionalSections, section => section.Heading == "전체 흐름");
    }

    [Fact]
    [Trait("Category", "GeneratedOntology")]
    public async Task CodexIntegration_ThreeRepresentativeQuestionsPersistDiagnosticsAndFinalAnswer()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("DREAMINE_RUN_CODEX_INTEGRATION"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        string root = FindRepositoryRoot();
        string directory = Path.Combine(Path.GetTempPath(), $"dreamine-codex-integration-{Guid.NewGuid():N}");
        try
        {
            JsonOntologyRepository ontology = new(
                new FixedOntologyDataPathResolver(Path.Combine(root, ".ua", "ontology")));
            OntologySourceService source = new(
                ontology,
                new FixedOntologySourcePathResolver(Path.Combine(
                    root, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web",
                    "wwwroot", "understand", "source")));
            string webRoot = Path.Combine(
                root, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot");
            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Documentation:DoxygenRoot"] = Path.Combine(root, "10_DOCUMENTS", "Doxygen")
                }).Build();
            KnowledgeQaOptions options = new()
            {
                Enabled = true,
                CodexExecutable = "codex.exe",
                RequestTimeoutSeconds = 240,
                IncludeDevelopmentDiagnostics = true,
                MaximumOntologyNodes = 16,
                MaximumRelations = 48,
                MaximumDoxygenReferences = 16,
                MaximumSourceReferences = 16
            };
            using CodexCliProcessRunner runner = new(options);
            CodexCliKnowledgeQuestionPlanner planner = new(runner, options);
            KnowledgeEvidenceBundleBuilder evidence = new(
                ontology,
                new OntologyRelationResolver(),
                source,
                new DoxygenXmlEvidenceProvider(
                    new DocumentationCatalogService(new TestWebHostEnvironment(webRoot)),
                    configuration),
                options,
                planner);
            JsonKnowledgeQuestionRepository questions = new(new DreamineOptions { DataPath = directory });
            KnowledgeQaService service = new(
                questions,
                evidence,
                new CodexCliKnowledgeAnswerGenerator(runner, options),
                new KnowledgeRequestScopePolicy(options),
                new KnowledgeAnswerProjectionService(),
                new KnowledgePrivacyScanner(),
                new OntologyValidationService(ontology),
                new OntologySymbolScopeResolver(ontology));
            string[] prompts =
            [
                "드리마인으로 우주선을 만들면 버튼을 누를 때 치킨이 자동으로 배달되나요?",
                "Dreamine WPF에서 XAML 버튼을 ViewModel의 명령에 연결하려면 어떻게 해야 하나요? 초보자도 따라 할 수 있게 간단한 예제와 함께 설명해 주세요.",
                "Dreamine WPF에서 XAML 버튼을 클릭했을 때 MainWindowViewModel의 Ok 명령이 MainWindowEvent.Ok까지 전달되는 전체 흐름을 설명해 주세요. DreamineCommand 특성, 생성 코드, hasEventComponent와 forwardsTo 관계가 각각 어떤 역할을 하는지 구분하고, 실제 선언 파일과 stable URI 근거를 제시해 주세요. 일반적인 ICommand 방식과 Dreamine 방식의 차이도 초보자가 이해할 수 있게 설명해 주세요."
            ];
            for (int promptIndex = 0; promptIndex < prompts.Length; promptIndex += 1)
            {
                string prompt = prompts[promptIndex];
                KnowledgeQuestionCreatedViewModel created = await service.AskAsync(
                    new KnowledgeQuestionRequest(prompt, "ko", Guid.NewGuid()),
                    CancellationToken.None);
                KnowledgeQuestionDetailsViewModel details = Assert.IsType<KnowledgeQuestionDetailsViewModel>(
                    await service.GetAsync(created.Id, created.AccessKey, false, CancellationToken.None));
                EvidenceReference[] actualEvidence = details.Revision.EvidenceBundle.Evidence.ToArray();
                if (promptIndex == 0)
                {
                    Assert.NotEqual(KnowledgeRequestDisposition.Supported, details.RequestDisposition);
                    Assert.Empty(actualEvidence);
                    Assert.Equal("RulePolicy", details.Revision.ExecutionDiagnostics.StoredDirectAnswerProducer);
                }
                else
                {
                    Assert.Equal("Codex", details.Revision.ExecutionDiagnostics.Retrieval.Planner.Provider);
                    Assert.True(details.Revision.ExecutionDiagnostics.Retrieval.Planner.Codex.JsonParseSucceeded);
                    Assert.Equal("Codex", details.Revision.ExecutionDiagnostics.AnswerGenerator.Provider);
                    Assert.True(details.Revision.ExecutionDiagnostics.AnswerGenerator.Codex.JsonParseSucceeded);
                    Assert.Equal("Codex", details.Revision.ExecutionDiagnostics.StoredDirectAnswerProducer);
                    Assert.False(details.Revision.ExecutionDiagnostics.FallbackUsed);
                }

                if (promptIndex == 1)
                {
                    EvidenceReference xaml = Assert.Single(actualEvidence, item =>
                        item.Kind == EvidenceKind.Source
                        && (item.SourcePath ?? string.Empty).EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
                        && (item.CodeExcerpt ?? string.Empty).Contains("<Button", StringComparison.OrdinalIgnoreCase)
                        && (item.CodeExcerpt ?? string.Empty).Contains("Command=\"{Binding", StringComparison.Ordinal));
                    EvidenceReference[] code = actualEvidence.Where(item =>
                        item.Kind == EvidenceKind.Source
                        && (item.SourcePath ?? string.Empty).EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToArray();
                    Assert.NotEmpty(code);
                    Assert.All(code, item => Assert.Equal(
                        ProjectBoundary(xaml.SourcePath!),
                        ProjectBoundary(item.SourcePath!)));
                    Assert.Contains(actualEvidence, item =>
                        item.Kind == EvidenceKind.Doxygen
                        && item.DoxygenUrlValidated);
                    Assert.DoesNotContain(actualEvidence, item =>
                        item.Title.Contains("AddChannel", StringComparison.OrdinalIgnoreCase)
                        || item.Title.Contains("AdminViewModel", StringComparison.OrdinalIgnoreCase));
                }

                if (promptIndex == 2)
                {
                    Assert.Contains(actualEvidence, item =>
                        item.Kind == EvidenceKind.OntologyRelation
                        && item.RelationType == "forwardsTo"
                        && item.Title.Contains("MainWindowViewModel.Ok", StringComparison.Ordinal)
                        && item.Title.Contains("MainWindowEvent.Ok", StringComparison.Ordinal));
                    Assert.Contains(actualEvidence, item =>
                        item.Kind == EvidenceKind.OntologyRelation
                        && item.RelationType == "hasEventComponent"
                        && item.Title.Contains("MainWindowViewModel", StringComparison.Ordinal)
                        && item.Title.Contains("MainWindowEvent", StringComparison.Ordinal));
                    Assert.DoesNotContain(actualEvidence, item =>
                        (item.SourcePath ?? string.Empty).Contains("SampleCore", StringComparison.OrdinalIgnoreCase)
                        || (item.SourcePath ?? string.Empty).Contains("SampleEnterprise", StringComparison.OrdinalIgnoreCase));
                    Assert.Contains("MainWindowViewModel", details.Revision.Content.Summary, StringComparison.Ordinal);
                }
                _output.WriteLine(JsonSerializer.Serialize(new
                {
                    prompt,
                    details.RequestDisposition,
                    details.ScopeReason,
                    details.Revision.ExecutionDiagnostics,
                    Evidence = actualEvidence.Select(item => new
                    {
                        item.Id,
                        item.Kind,
                        item.Title,
                        item.RelationType,
                        item.SourcePath,
                        item.StableUri,
                        item.RelatedStableUri
                    }),
                    StoredContent = details.Revision.Content,
                    ProjectedAnswer = details.Answer
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static OpenAiCompatibleKnowledgeAnswerGenerator CreateLanguageModelGenerator(
        HttpMessageHandler handler) => new(
            new HttpClient(handler),
            new KnowledgeQaOptions
            {
                Endpoint = "http://lm-studio.test/v1/",
                Model = "gemma-3-4b-it"
            });

    private static string CompletionPayload(string answer) => JsonSerializer.Serialize(new
    {
        choices = new[] { new { message = new { content = answer } } }
    });

    private static KnowledgeQaService CreateService(
        IKnowledgeQuestionRepository repository,
        IKnowledgeAnswerGenerator generator) => new(
            repository,
            new FixedEvidenceQuery(CreateBundle()),
            generator,
            new KnowledgeRequestScopePolicy(new KnowledgeQaOptions()),
            new KnowledgeAnswerProjectionService(),
            new KnowledgePrivacyScanner(),
            new FixedValidationService(CreateBundle().Version));

    private static EvidenceBundle CreateBundle()
    {
        KnowledgeVersionSnapshot version = new(
            ["SampleSmart v1.0.0.0"], "graph-v1", "ontology-v1", "hash-v1", DateTimeOffset.UtcNow);
        return new EvidenceBundle(
            "MainWindowViewModel.Ok는 어디로 전달되나요?",
            "MainWindowViewModel.Ok는 어디로 전달되나요?",
            [
                new EvidenceReference
                {
                    Id = "relation-1",
                    Kind = EvidenceKind.OntologyRelation,
                    Origin = EvidenceOrigin.Direct,
                    Title = "MainWindowViewModel.Ok → forwardsTo → MainWindowEvent.Ok",
                    StableUri = "urn:test:viewmodel-ok",
                    RelatedStableUri = "urn:test:event-ok",
                    RelationType = "forwardsTo",
                    ProjectionType = "calls",
                    Provenance = "source_attribute",
                    Confidence = 1d
                },
                new EvidenceReference
                {
                    Id = "doxygen-1",
                    Kind = EvidenceKind.Doxygen,
                    Origin = EvidenceOrigin.Direct,
                    Title = "SampleSmart.Pages.MainWindowViewModel.Ok",
                    StableUri = "urn:test:viewmodel-ok",
                    DoxygenUrl = "/docs/doxygen/SampleSmart/MainWindowViewModel.html#ok",
                    Provenance = "Doxygen XML",
                    Confidence = 1d
                }
            ],
            version,
            DateTimeOffset.UtcNow);
    }

    private static KnowledgeEvidenceBundleBuilder CreateRealEvidenceQuery()
    {
        string root = FindRepositoryRoot();
        JsonOntologyRepository repository = new(
            new FixedOntologyDataPathResolver(Path.Combine(root, ".ua", "ontology")));
        OntologySourceService source = new(
            repository,
            new FixedOntologySourcePathResolver(Path.Combine(
                root, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot", "understand", "source")));
        string webRoot = Path.Combine(root, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot");
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Documentation:DoxygenRoot"] = Path.Combine(root, "10_DOCUMENTS", "Doxygen")
        }).Build();
        return new KnowledgeEvidenceBundleBuilder(
            repository,
            new OntologyRelationResolver(),
            source,
            new DoxygenXmlEvidenceProvider(
                new DocumentationCatalogService(new TestWebHostEnvironment(webRoot)), configuration),
            new KnowledgeQaOptions());
    }

    private static string ProjectBoundary(string sourcePath)
    {
        string normalized = sourcePath.Replace('\\', '/');
        string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        int sources = Array.FindIndex(segments, segment => segment.Equals("20_SOURCES", StringComparison.OrdinalIgnoreCase));
        if (sources >= 0)
        {
            for (int index = sources + 1; index < segments.Length; index += 1)
            {
                if (!char.IsDigit(segments[index][0]))
                {
                    string project = segments[index];
                    string[] projectParts = project.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    string[] platformSuffixes = ["Wpf", "WinForms", "Blazor", "Maui", "Shared", "Web"];
                    if (projectParts.Length > 1
                        && platformSuffixes.Contains(projectParts[^1], StringComparer.OrdinalIgnoreCase))
                    {
                        project = string.Join('.', projectParts[..^1]);
                    }
                    return string.Join('/', segments.Take(index).Append(project));
                }
            }
        }
        return Path.GetDirectoryName(normalized) ?? normalized;
    }

    private static KnowledgeQuestion CreateQuestion(EvidenceBundle bundle) => new()
    {
        Id = 1,
        Slug = "samplesmart-forwarding",
        OriginalQuestion = "SampleSmart에서 MainWindowViewModel.Ok는 어디로 전달되나요?",
        NormalizedQuestion = "SampleSmart에서 MainWindowViewModel.Ok는 어디로 전달되나요?",
        Summary = "근거 기반 영구 답변",
        Category = "호출 및 전달 흐름",
        Language = "ko",
        RequestDisposition = KnowledgeRequestDisposition.Supported,
        PublicationStatus = QuestionPublicationStatus.PendingReview,
        AccessKeyHash = new string('A', 64),
        Answer = new KnowledgeAnswer { CurrentRevision = 1, Revisions = [CreateRevision(bundle)] },
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static OntologyNode CreateSymbolNode(string uri, string name, string project) => new()
    {
        StableUri = uri,
        CanonicalName = name,
        QualifiedName = $"{project}.{name}",
        EffectiveType = "Method",
        ProjectName = project,
        SourcePath = $"20_SOURCES/{project}/{name}.cs",
        DefaultSearchVisible = true
    };

    private static AnswerRevision CreateRevision(EvidenceBundle bundle) => new()
    {
        Revision = 1,
        Content = new KnowledgeAnswerContent(
            "영문이 섞인 model answer",
            [new KnowledgeAnswerSection("Detailed analysis", "English content", ["relation-1"])],
            [], [], ["relation-1"]),
        EvidenceBundle = bundle,
        Version = bundle.Version,
        PromptPolicyVersion = "test-policy",
        ModelId = "test-model",
        CreatedAt = DateTimeOffset.UtcNow,
        LastValidatedAt = DateTimeOffset.UtcNow
    };

    private static string FindRepositoryRoot()
    {
        foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            DirectoryInfo? current = new(start);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, ".ua", "ontology", "instances.json")))
                    return current.FullName;
                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not find the Dreamine ontology root.");
    }

    private sealed class CapturingHandler(string responseBody) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount += 1;
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class FailureHandler(Exception failure) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromException<HttpResponseMessage>(failure);
    }

    private sealed class CapturingCodexRunner(string output) : ICodexCliProcessRunner
    {
        public string Instruction { get; private set; } = string.Empty;
        public string OutputSchema { get; private set; } = string.Empty;

        public Task<CodexCliProcessResult> RunAsync(
            string instruction,
            string inputJson,
            string outputSchema,
            CancellationToken cancellationToken)
        {
            Instruction = instruction;
            OutputSchema = outputSchema;
            return Task.FromResult(new CodexCliProcessResult(
                true, output, string.Empty, 0, false, output, string.Empty, 1));
        }

        public Task<CodexCliProcessResult> RunInRepositoryAsync(
            string instruction,
            string inputJson,
            string outputSchema,
            CancellationToken cancellationToken) =>
            RunAsync(instruction, inputJson, outputSchema, cancellationToken);

        public string ResolveRepositoryRoot() => Directory.GetCurrentDirectory();
    }

    private sealed class CapturingRepositoryCodexRunner(string repositoryRoot, string output)
        : ICodexCliProcessRunner
    {
        public string InputJson { get; private set; } = string.Empty;

        public Task<CodexCliProcessResult> RunAsync(
            string instruction,
            string inputJson,
            string outputSchema,
            CancellationToken cancellationToken) =>
            RunInRepositoryAsync(instruction, inputJson, outputSchema, cancellationToken);

        public Task<CodexCliProcessResult> RunInRepositoryAsync(
            string instruction,
            string inputJson,
            string outputSchema,
            CancellationToken cancellationToken)
        {
            InputJson = inputJson;
            return Task.FromResult(new CodexCliProcessResult(
                true, output, string.Empty, 0, false, output, string.Empty, 1));
        }

        public string ResolveRepositoryRoot() => repositoryRoot;
    }

    private sealed class FixedEvidenceQuery(EvidenceBundle bundle) : IKnowledgeEvidenceQueryService
    {
        public Task<EvidenceBundle> QueryAsync(KnowledgeEvidenceQuery query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(bundle with { Question = query.Query, NormalizedQuestion = query.Query.Trim() });
        }
    }

    private sealed class CountingEvidenceQuery(EvidenceBundle bundle) : IKnowledgeEvidenceQueryService
    {
        public int CallCount { get; private set; }
        public Task<EvidenceBundle> QueryAsync(KnowledgeEvidenceQuery query, CancellationToken cancellationToken)
        {
            CallCount += 1;
            return Task.FromResult(bundle);
        }
    }

    private sealed class FixedSymbolScopeResolver(KnowledgeSymbolScopeResolution resolution)
        : IKnowledgeSymbolScopeResolver
    {
        public Task<KnowledgeSymbolScopeResolution> ResolveAsync(
            string question,
            CancellationToken cancellationToken) => Task.FromResult(resolution);
    }

    private sealed class SymbolTestOntologyRepository(
        IReadOnlyList<OntologyNode> nodes,
        IReadOnlyList<OntologyRelation> relations) : IOntologyRepository
    {
        public Task<OntologyPage<OntologyNode>> SearchNodesAsync(
            OntologyQuery query,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            OntologyNode[] matches = nodes.Where(node =>
                node.CanonicalName.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase)
                || node.QualifiedName.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase)).ToArray();
            return Task.FromResult(new OntologyPage<OntologyNode>(matches, 1, pageSize, matches.Length));
        }

        public Task<IReadOnlyList<OntologyRelation>> GetRelationsAsync(
            string stableUri,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OntologyRelation>>(
                relations.Where(item => item.SourceUri == stableUri || item.TargetUri == stableUri).ToArray());

        public Task<OntologyNode?> GetNodeAsync(string stableUri, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, OntologyNode>> GetNodesAsync(
            IEnumerable<string> stableUris, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<OntologyTBoxClass>> GetTBoxClassesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<OntologyFacets> GetFacetsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<OntologyValidationData> GetValidationDataAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<OntologyLoadMetrics> GetLoadMetricsAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FixedAnswerGenerator : IKnowledgeAnswerGenerator
    {
        public Task<KnowledgeAnswerGenerationResult> GenerateAsync(
            EvidenceBundle bundle,
            CancellationToken cancellationToken) => Task.FromResult(new KnowledgeAnswerGenerationResult(
                new KnowledgeAnswerContent(
                    "근거 기반 영구 답변",
                    [new KnowledgeAnswerSection("전달 흐름", "MainWindowEvent.Ok로 전달됩니다.", ["relation-1"])],
                    ["MainWindowViewModel.Ok", "MainWindowEvent.Ok"],
                    [],
                    ["relation-1"]),
                "gemma-3-4b-it",
                OpenAiCompatibleKnowledgeAnswerGenerator.PromptPolicyVersion)
            {
                Diagnostics = new KnowledgeAnswerGeneratorDiagnostics { Provider = "Codex" }
            });
    }

    private sealed class FallbackAnswerGenerator(string reason) : IKnowledgeAnswerGenerator
    {
        public Task<KnowledgeAnswerGenerationResult> GenerateAsync(
            EvidenceBundle bundle,
            CancellationToken cancellationToken) => Task.FromResult(new KnowledgeAnswerGenerationResult(
                new KnowledgeAnswerContent(
                    "요청한 관계를 검증된 근거에서 찾지 못했습니다.", [], [], [], []),
                "deterministic-evidence",
                OpenAiCompatibleKnowledgeAnswerGenerator.PromptPolicyVersion)
            {
                Diagnostics = new KnowledgeAnswerGeneratorDiagnostics
                {
                    Provider = "RuleFallback",
                    FallbackReason = reason
                }
            });
    }

    private sealed class FixedRepositoryAnswerGenerator(KnowledgeEvidenceCoverage? coverage = null)
        : IKnowledgeRepositoryAnswerGenerator
    {
        public Task<RepositoryKnowledgeAnswerResult> GenerateAsync(
            string question,
            string language,
            CancellationToken cancellationToken)
        {
            EvidenceReference source = new()
            {
                Id = "source-1",
                Kind = EvidenceKind.Source,
                Origin = EvidenceOrigin.Direct,
                Title = "AddChannel",
                Summary = "실제 채널 추가 구현입니다.",
                SourcePath = "20_SOURCES/Sample/AddChannel.cs",
                LineStart = 10,
                LineEnd = 14,
                Declaration = "private void AddChannel()",
                CodeExcerpt = "Monitor.AddChannel();",
                Provenance = "Codex read-only repository search"
            };
            CodexInvocationDiagnostics codex = new()
            {
                Attempted = true,
                Succeeded = true,
                ExitCode = 0,
                JsonParseSucceeded = true
            };
            EvidenceBundle bundle = new(
                question,
                question,
                [source],
                new KnowledgeVersionSnapshot(["server-repository"], string.Empty, string.Empty, string.Empty, null),
                DateTimeOffset.UtcNow)
            {
                Coverage = coverage ?? new KnowledgeEvidenceCoverage(),
                RetrievalDiagnostics = new KnowledgeRetrievalDiagnostics
                {
                    Intent = "repository-search",
                    SourceKinds = ["Code"],
                    Planner = new KnowledgePlannerDiagnostics
                    {
                        Provider = "CodexRepositorySearch",
                        Codex = codex
                    }
                }
            };
            KnowledgeAnswerGenerationResult answer = new(
                new KnowledgeAnswerContent(
                    "저장소에서 AddChannel 구현을 찾아 실제 호출 흐름을 확인했습니다.",
                    [new KnowledgeAnswerSection("실행 흐름", "AddChannel이 Monitor.AddChannel을 호출합니다.", ["source-1"])],
                    ["AddChannel"], [], ["source-1"]),
                "codex-cli:test",
                CodexRepositoryKnowledgeAnswerGenerator.PromptPolicyVersion)
            {
                Diagnostics = new KnowledgeAnswerGeneratorDiagnostics
                {
                    Provider = "Codex",
                    Codex = codex
                }
            };
            return Task.FromResult(new RepositoryKnowledgeAnswerResult(bundle, answer));
        }
    }

    private sealed class DelayedAnswerGenerator : IKnowledgeAnswerGenerator
    {
        private int _callCount;
        public int CallCount => Volatile.Read(ref _callCount);
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<KnowledgeAnswerGenerationResult> GenerateAsync(
            EvidenceBundle bundle,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            Started.TrySetResult(true);
            await Release.Task.WaitAsync(cancellationToken);
            return await new FixedAnswerGenerator().GenerateAsync(bundle, cancellationToken);
        }
    }

    private sealed class FlakyAnswerGenerator : IKnowledgeAnswerGenerator
    {
        private int _callCount;
        public int CallCount => Volatile.Read(ref _callCount);

        public Task<KnowledgeAnswerGenerationResult> GenerateAsync(
            EvidenceBundle bundle,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _callCount) == 1)
                throw new InvalidOperationException("simulated generator failure");
            return new FixedAnswerGenerator().GenerateAsync(bundle, cancellationToken);
        }
    }

    private sealed class CountingAnswerGenerator : IKnowledgeAnswerGenerator
    {
        public int CallCount { get; private set; }
        public Task<KnowledgeAnswerGenerationResult> GenerateAsync(
            EvidenceBundle bundle,
            CancellationToken cancellationToken)
        {
            CallCount += 1;
            throw new InvalidOperationException("The LLM must not be called for a gated request.");
        }
    }

    private sealed class FixedValidationService(KnowledgeVersionSnapshot version) : IOntologyValidationService
    {
        public Task<OntologyValidationSummaryViewModel> GetSummaryAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new OntologyValidationSummaryViewModel
            {
                IsHealthy = true,
                OntologyVersion = version.OntologyVersion,
                GraphVersion = version.GraphVersion,
                ContentHash = version.OntologyHash,
                GeneratedAt = version.OntologyGeneratedAt
            });
    }

    private sealed class TestWebHostEnvironment(string webRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Dreamine.Web.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(webRootPath);
        public string WebRootPath { get; set; } = webRootPath;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = Path.GetDirectoryName(webRootPath)!;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
