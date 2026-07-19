using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length != 5)
{
    Console.Error.WriteLine("Usage: SourceAudit <repository-root> <knowledge-graph.json> <domain-graph.json> <instances.json|-> <output-directory>");
    return 2;
}

var repositoryRoot = Path.GetFullPath(args[0]);
var knowledgePath = Path.GetFullPath(args[1]);
var domainPath = Path.GetFullPath(args[2]);
var instancesPath = args[3] == "-" ? null : Path.GetFullPath(args[3]);
var outputDirectory = Path.GetFullPath(args[4]);
Directory.CreateDirectory(outputDirectory);

var knowledge = ReadGraph(knowledgePath);
var domain = ReadGraph(domainPath);
var allGraphNodes = knowledge.Nodes.Concat(domain.Nodes).ToList();
var allGraphEdges = knowledge.Edges.Concat(domain.Edges).ToList();
var graphCodeFilePaths = knowledge.Nodes
    .Select(node => node.FilePath)
    .Where(file => file is not null && file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
    .Select(file => Normalize(file!))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
    .ToList();
var repositorySourceRoot = Path.Combine(repositoryRoot, "20_SOURCES");
var repositoryCodeFilePaths = Directory.Exists(repositorySourceRoot)
    ? Directory.EnumerateFiles(repositorySourceRoot, "*.cs", SearchOption.AllDirectories)
        .Select(file => Normalize(Path.GetRelativePath(repositoryRoot, file)))
        .Where(file => !HasExcludedSegment(file))
    : [];
var codeFilePaths = graphCodeFilePaths.Concat(repositoryCodeFilePaths)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
    .ToList();

var syntaxTrees = new List<SyntaxTree>();
var excludedGeneratedFiles = new List<string>();
var missingSourceFiles = new List<string>();
foreach (var relativePath in codeFilePaths)
{
    if (HasExcludedSegment(relativePath))
    {
        excludedGeneratedFiles.Add(relativePath);
        continue;
    }

    var absolutePath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    if (!File.Exists(absolutePath))
    {
        missingSourceFiles.Add(relativePath);
        continue;
    }

    var source = await File.ReadAllTextAsync(absolutePath);
    if (IsGeneratedSource(relativePath, source))
    {
        excludedGeneratedFiles.Add(relativePath);
        continue;
    }

    syntaxTrees.Add(CSharpSyntaxTree.ParseText(
        source,
        new CSharpParseOptions(LanguageVersion.Preview, DocumentationMode.Parse),
        relativePath));
}

var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
    .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
    .Select(path => MetadataReference.CreateFromFile(path));
var compilation = CSharpCompilation.Create(
    "DreamineSourceAudit",
    syntaxTrees,
    references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

var declarations = new List<SourceDeclaration>();
var partialMethods = new List<PartialMethodDeclaration>();
var eventBindings = new List<EventBindingDeclaration>();
foreach (var tree in syntaxTrees)
{
    var root = await tree.GetRootAsync();
    var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
    foreach (var declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
    {
        var symbol = model.GetDeclaredSymbol(declaration) as INamedTypeSymbol;
        var isAttribute = declaration is ClassDeclarationSyntax && IsAttribute(symbol, declaration);
        var (category, syntaxKind, ontologyType) = ClassifyDeclaration(declaration, isAttribute);
        var line = declaration.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var qualifiedName = symbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
            ?? BuildQualifiedName(declaration);
        declarations.Add(new SourceDeclaration(
            Normalize(tree.FilePath), declaration.Identifier.ValueText, qualifiedName, category,
            syntaxKind, ontologyType, line, declaration.Modifiers.Any(SyntaxKind.PartialKeyword), isAttribute));
    }

    foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
    {
        if (!method.Modifiers.Any(SyntaxKind.PartialKeyword) || method.Body is not null || method.ExpressionBody is not null)
            continue;
        var methodSymbol = model.GetDeclaredSymbol(method) as IMethodSymbol;
        var containingType = methodSymbol?.ContainingType?.Name
            ?? method.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText;
        var containingDeclaration = method.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
        var containingTypeQualifiedName = methodSymbol?.ContainingType?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
            ?? (containingDeclaration is null ? containingType : BuildQualifiedName(containingDeclaration));
        var line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var commandAttribute = method.AttributeLists.SelectMany(list => list.Attributes)
            .FirstOrDefault(attribute => AttributeMatches(attribute, "DreamineCommand"));
        var generatedRegexAttribute = method.AttributeLists.SelectMany(list => list.Attributes)
            .FirstOrDefault(attribute => AttributeMatches(attribute, "GeneratedRegex"));
        var commandTarget = commandAttribute is null ? null : GetFirstStringArgument(model, commandAttribute);
        var classification = commandTarget?.StartsWith("Event.", StringComparison.Ordinal) == true
            ? "dreamine_event_forwarding"
            : commandAttribute is not null
                ? "dreamine_command_generator_target"
                : generatedRegexAttribute is not null
                    ? "generated_regex_target"
                    : "unclassified";
        var generatorAttribute = commandAttribute is not null ? "DreamineCommand"
            : generatedRegexAttribute is not null ? "GeneratedRegex" : null;
        partialMethods.Add(new PartialMethodDeclaration(
            Normalize(tree.FilePath), containingType, containingTypeQualifiedName, method.Identifier.ValueText, line,
            method.ParameterList.Parameters.Count, classification,
            commandTarget?.StartsWith("Event.", StringComparison.Ordinal) == true ? commandTarget["Event.".Length..] : null,
            commandTarget, generatorAttribute, (commandAttribute ?? generatedRegexAttribute)?.ToFullString().Trim()));
    }

    foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
    {
        var eventAttribute = field.AttributeLists.SelectMany(list => list.Attributes)
            .FirstOrDefault(attribute => AttributeMatches(attribute, "DreamineEvent"));
        if (eventAttribute is null) continue;

        var componentType = model.GetTypeInfo(field.Declaration.Type).Type as INamedTypeSymbol;
        var componentDeclarationReference = componentType?.DeclaringSyntaxReferences.FirstOrDefault();
        var componentPath = componentDeclarationReference?.SyntaxTree.FilePath is { } declaredPath
            ? Normalize(declaredPath) : null;
        var componentLine = componentDeclarationReference?.GetSyntax().GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var containingDeclaration = field.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
        var containingSymbol = containingDeclaration is null ? null : model.GetDeclaredSymbol(containingDeclaration) as INamedTypeSymbol;
        foreach (var variable in field.Declaration.Variables)
        {
            eventBindings.Add(new EventBindingDeclaration(
                Normalize(tree.FilePath),
                containingSymbol?.Name ?? containingDeclaration?.Identifier.ValueText,
                containingSymbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                    ?? (containingDeclaration is null ? null : BuildQualifiedName(containingDeclaration)),
                variable.Identifier.ValueText,
                componentType?.Name ?? field.Declaration.Type.ToString().Split('.').Last(),
                componentType?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                    ?? field.Declaration.Type.ToString(),
                field.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                eventAttribute.ToFullString().Trim(), componentPath, componentLine));
        }
    }
}

var declarationsByPathAndName = declarations
    .GroupBy(declaration => DeclarationKey(declaration.Path, declaration.Name), StringComparer.OrdinalIgnoreCase)
    .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
var nodeTypeOverrides = new Dictionary<string, string>(StringComparer.Ordinal);
var nodeMappings = new List<NodeDeclarationMapping>();
var unmappedTypeNodes = new List<NodeMappingIssue>();
var ambiguousTypeNodes = new List<NodeMappingIssue>();
var sourcePathOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var excludedGeneratedPathSet = excludedGeneratedFiles.Select(Normalize).ToHashSet(StringComparer.OrdinalIgnoreCase);
var excludedGraphTypeNodes = new List<NodeMappingIssue>();
// Preserve raw graph paths as provenance, but publish a deterministic path
// override whenever the old test-tree artifact has an exact physical match in
// 200. Tests. This includes project/configuration nodes as well as C# nodes.
foreach (var rawPath in knowledge.Nodes.Select(node => node.FilePath)
             .Where(path => !string.IsNullOrWhiteSpace(path))
             .Select(path => Normalize(path!))
             .Distinct(StringComparer.OrdinalIgnoreCase))
{
    if (TryMapStaleTestPath(rawPath) is not { } currentPath) continue;
    var rawAbsolutePath = Path.Combine(repositoryRoot, rawPath.Replace('/', Path.DirectorySeparatorChar));
    var currentAbsolutePath = Path.Combine(repositoryRoot, currentPath.Replace('/', Path.DirectorySeparatorChar));
    if (!File.Exists(rawAbsolutePath) && File.Exists(currentAbsolutePath))
        sourcePathOverrides[rawPath] = currentPath;
}
foreach (var node in knowledge.Nodes.Where(node => node.Type == "class"))
{
    var rawPath = node.FilePath is null ? null : Normalize(node.FilePath);
    var candidates = rawPath is null
        ? []
        : declarationsByPathAndName.GetValueOrDefault(DeclarationKey(rawPath, node.Name), []);
    var remapped = false;
    if (candidates.Count == 0 && rawPath is not null && TryMapStaleTestPath(rawPath) is { } currentPath)
    {
        var remappedCandidates = declarationsByPathAndName.GetValueOrDefault(DeclarationKey(currentPath, node.Name), []);
        if (remappedCandidates.Count > 0)
        {
            candidates = remappedCandidates;
            sourcePathOverrides[rawPath] = currentPath;
            remapped = true;
        }
    }
    if (candidates.Count > 1 && node.LineStart is not null && node.LineEnd is not null)
        candidates = candidates.Where(candidate => candidate.Line >= node.LineStart && candidate.Line <= node.LineEnd).ToList();
    if (candidates.Count > 1 && node.ApiNamespace is not null)
        candidates = candidates.Where(candidate => candidate.QualifiedName.StartsWith(node.ApiNamespace + ".", StringComparison.Ordinal)).ToList();

    if (candidates.Count == 1)
    {
        var declaration = candidates[0];
        nodeTypeOverrides[node.Id] = declaration.OntologyType;
        nodeMappings.Add(new NodeDeclarationMapping(node.Id, declaration.Path, declaration.Name, declaration.QualifiedName,
            declaration.Line, declaration.OntologyType, rawPath, remapped));
    }
    else
    {
        var issue = new NodeMappingIssue(node.Id, node.Name, node.FilePath, candidates.Count,
            candidates.Count == 0 ? "source declaration not found" : "multiple source declarations matched");
        if (rawPath is not null && excludedGeneratedPathSet.Contains(rawPath)) excludedGraphTypeNodes.Add(issue with
        {
            Reason = "generated source excluded by policy"
        });
        else (candidates.Count == 0 ? unmappedTypeNodes : ambiguousTypeNodes).Add(issue);
    }
}

var boundEventComponentDeclarations = eventBindings
    .Select(binding => ResolveEventComponentDeclaration(binding, declarations))
    .Where(declaration => declaration is not null)
    .Cast<SourceDeclaration>()
    .ToList();
var conventionEventComponentFilePaths = syntaxTrees.Select(tree => Normalize(tree.FilePath))
    .Where(sourcePath => sourcePath.EndsWith(".xaml.Event.cs", StringComparison.OrdinalIgnoreCase))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
var boundEventComponentFilePaths = boundEventComponentDeclarations.Select(item => item.Path);
var eventComponentFilePaths = conventionEventComponentFilePaths.Concat(boundEventComponentFilePaths)
    .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
var eventComponentFilePathSet = eventComponentFilePaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
var eventComponentDeclarations = declarations
    .Where(declaration => eventComponentFilePathSet.Contains(declaration.Path)
        || boundEventComponentDeclarations.Any(bound =>
            bound.Path.Equals(declaration.Path, StringComparison.OrdinalIgnoreCase)
            && bound.QualifiedName == declaration.QualifiedName
            && bound.Line == declaration.Line))
    .ToList();
var eventComponentDeclarationKeys = eventComponentDeclarations
    .Select(declaration => $"{Normalize(declaration.Path)}|{declaration.QualifiedName}|{declaration.Line}")
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
eventBindings = eventBindings.Select(binding =>
{
    var declaration = ResolveEventComponentDeclaration(binding, declarations);
    return declaration is null ? binding : binding with { ComponentPath = declaration.Path, ComponentLine = declaration.Line };
}).ToList();
var eventComponentNodeIds = nodeMappings.Where(mapping =>
        eventComponentDeclarationKeys.Contains($"{Normalize(mapping.Path)}|{mapping.QualifiedName}|{mapping.Line}"))
    .Select(mapping => mapping.NodeId).Distinct(StringComparer.Ordinal).ToArray();

var graphFunctionsByPathAndName = knowledge.Nodes
    .Where(node => node.Type == "function" && node.FilePath is not null)
    .GroupBy(node => DeclarationKey(node.FilePath!, node.Name), StringComparer.OrdinalIgnoreCase)
    .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
var missingPartialMethods = partialMethods.Where(method =>
{
    var candidates = graphFunctionsByPathAndName.GetValueOrDefault(DeclarationKey(method.Path, method.Name), []);
    return !candidates.Any(candidate => candidate.LineStart is null || candidate.LineEnd is null ||
        (method.Line >= candidate.LineStart && method.Line <= candidate.LineEnd));
}).ToList();

var instance = instancesPath is not null && File.Exists(instancesPath)
    ? ReadInstances(instancesPath)
    : new InstanceGraph([], []);
var elementByGraphId = instance.Elements
    .Where(element => element.SourceGraphId is not null)
    .GroupBy(element => element.SourceGraphId!, StringComparer.Ordinal)
    .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
var sourceVsOverlayMismatches = new List<TypeMismatch>();
var sourceVsOriginalMismatches = new List<TypeMismatch>();
var automaticallyCorrectedNodes = new List<TypeMismatch>();
foreach (var mapping in nodeMappings)
{
    var node = knowledge.NodesById[mapping.NodeId];
    var originalType = OriginalGraphType(node);
    var overlay = elementByGraphId.GetValueOrDefault(mapping.NodeId);
    var overlayMatches = overlay is not null && overlay.Types.Contains(mapping.OntologyType, StringComparer.Ordinal);
    if (!string.Equals(originalType, mapping.OntologyType, StringComparison.Ordinal))
    {
        var mismatch = new TypeMismatch(mapping.NodeId, node.Name, node.FilePath, mapping.OntologyType,
            originalType, overlay?.ElementType, "source-vs-original");
        sourceVsOriginalMismatches.Add(mismatch);
        if (overlayMatches) automaticallyCorrectedNodes.Add(mismatch with { Scope = "auto-corrected-overlay" });
    }
    if (overlay is not null && !overlayMatches)
        sourceVsOverlayMismatches.Add(new TypeMismatch(mapping.NodeId, node.Name, node.FilePath,
            mapping.OntologyType, originalType, overlay.ElementType, "source-vs-overlay"));
}

var allNodeIds = allGraphNodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
var rawDanglingRelations = allGraphEdges
    .Where(edge => !allNodeIds.Contains(edge.Source) || !allNodeIds.Contains(edge.Target))
    .Select(edge => new RelationIssue(edge.Id, edge.Type, edge.Source, edge.Target, null, null, "raw graph endpoint is missing"))
    .ToList();
var elementIds = instance.Elements.Select(element => element.StableId).ToHashSet(StringComparer.Ordinal);
var overlayDanglingRelations = instance.Relations
    .Where(relation => !elementIds.Contains(relation.Source) || !elementIds.Contains(relation.Target))
    .Select(relation => new RelationIssue(relation.StableId, relation.RelationType, relation.Source, relation.Target,
        null, null, "overlay endpoint is missing"))
    .ToList();

var codeTypes = new HashSet<string>(["CodeClass", "CodeInterface", "CodeRecord", "CodeStruct", "CodeEnum", "CodeAttribute"], StringComparer.Ordinal);
var relationTypeViolations = new List<RelationIssue>();
var unverifiableTypedRelations = new List<RelationIssue>();
foreach (var edge in knowledge.Edges.Where(edge => edge.Type is "implements" or "inherits"))
{
    var sourceType = nodeTypeOverrides.GetValueOrDefault(edge.Source);
    var targetType = nodeTypeOverrides.GetValueOrDefault(edge.Target);
    if (sourceType is null || targetType is null)
    {
        unverifiableTypedRelations.Add(new RelationIssue(edge.Id, edge.Type, edge.Source, edge.Target,
            sourceType, targetType, "source declaration type could not be verified"));
        continue;
    }

    string? reason = null;
    if (edge.Type == "implements" && (!codeTypes.Contains(sourceType) || targetType != "CodeInterface"))
        reason = "implements requires a source code type and a CodeInterface target";
    if (edge.Type == "inherits")
    {
        if (!codeTypes.Contains(sourceType) || !codeTypes.Contains(targetType))
            reason = "inherits requires source and target code types";
        else if (sourceType == "CodeInterface" && targetType != "CodeInterface")
            reason = "an interface may inherit only another interface";
        else if (sourceType is "CodeStruct" or "CodeEnum")
            reason = "struct and enum declarations cannot use class inheritance";
        else if (sourceType != "CodeInterface" && targetType == "CodeInterface")
            reason = "class/record to interface must be represented as implements";
    }
    if (reason is not null)
        relationTypeViolations.Add(new RelationIssue(edge.Id, edge.Type, edge.Source, edge.Target, sourceType, targetType, reason));
}

var stableGroups = instance.Elements.GroupBy(element => element.StableId, StringComparer.Ordinal).ToList();
var duplicateStableUris = stableGroups.Where(group => group.Count() > 1)
    .Select(group => new StableUriIssue(group.Key, group.Count(), group.Select(item => item.ElementType).Distinct().Order().ToArray()))
    .ToList();
var stableUriTypeConflicts = duplicateStableUris.Where(issue => issue.Types.Length > 1).ToList();
var duplicateDeclarationNodes = nodeMappings
    .GroupBy(mapping => $"{Normalize(mapping.Path)}|{mapping.Name}|{mapping.Line}", StringComparer.OrdinalIgnoreCase)
    .Where(group => group.Count() > 1)
    .Select(group => new DuplicateDeclarationNode(group.Key, group.Select(item => item.NodeId).ToArray()))
    .ToList();
var mappedDeclarationKeys = nodeMappings
    .Select(mapping => $"{Normalize(mapping.Path)}|{mapping.Name}|{mapping.Line}")
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
var declarationsMissingGraphNodes = declarations
    .Where(declaration => !mappedDeclarationKeys.Contains($"{Normalize(declaration.Path)}|{declaration.Name}|{declaration.Line}"))
    .ToList();
var overlayDeclarationKeys = instance.Elements
    .Where(element => element.SourcePath is not null && element.LineStart is not null && element.Types.Any(codeTypes.Contains))
    .Select(element => $"{Normalize(element.SourcePath!)}|{element.CanonicalName}|{element.LineStart}")
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
var declarationsMissingOverlayNodes = declarationsMissingGraphNodes
    .Where(declaration => !overlayDeclarationKeys.Contains($"{Normalize(declaration.Path)}|{declaration.Name}|{declaration.Line}"))
    .ToList();
var overlayPartialKeys = instance.Elements
    .Where(element => element.SourcePath is not null && element.LineStart is not null && element.Types.Contains("Method", StringComparer.Ordinal))
    .Select(element => $"{Normalize(element.SourcePath!)}|{element.CanonicalName}|{element.LineStart}")
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
var partialMethodsMissingOverlayNodes = partialMethods
    .Where(method => !overlayPartialKeys.Contains($"{Normalize(method.Path)}|{method.Name}|{method.Line}"))
    .ToList();
var partialClassificationCounts = partialMethods.GroupBy(item => item.Classification, StringComparer.Ordinal)
    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
var stalePathRemaps = nodeMappings.Where(mapping => mapping.IsRemapped)
    .Select(mapping => new
    {
        mapping.NodeId,
        rawPath = mapping.RawPath,
        currentPath = mapping.Path,
        mapping.Name,
        mapping.QualifiedName,
        mapping.Line
    }).ToList();
var excludedGraphNodeIds = knowledge.Nodes
    .Where(node => node.FilePath is not null && excludedGeneratedPathSet.Contains(Normalize(node.FilePath)))
    .Select(node => node.Id).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
var unresolvedEventBindings = eventBindings.Where(binding => binding.ComponentPath is null).ToList();

var declarationCounts = declarations.GroupBy(item => item.Category, StringComparer.Ordinal)
    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
var mismatchByType = sourceVsOriginalMismatches
    .GroupBy(item => $"{item.ActualOriginalType ?? "unknown"} -> {item.ExpectedSourceType}", StringComparer.Ordinal)
    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
var report = new
{
    generatedAt = DateTimeOffset.UtcNow,
    scope = new
    {
        totalExistingNodes = allGraphNodes.Count,
        knowledgeGraphNodes = knowledge.Nodes.Count,
        domainGraphNodes = domain.Nodes.Count,
        totalExistingRelations = allGraphEdges.Count,
        knowledgeGraphRelations = knowledge.Edges.Count,
        domainGraphRelations = domain.Edges.Count,
        csharpFilesInGraph = graphCodeFilePaths.Count,
        csharpFilesInRepositoryScope = codeFilePaths.Count,
        parsedCsharpFiles = syntaxTrees.Count,
        excludedGeneratedFileCount = excludedGeneratedFiles.Count,
        missingSourceFileCount = missingSourceFiles.Count
    },
    declarations = new
    {
        counts = declarationCounts,
        total = declarations.Count,
        uniqueLogicalTypes = declarations.Select(item => item.QualifiedName).Distinct(StringComparer.Ordinal).Count(),
        partialTypeDeclarationCount = declarations.Count(item => item.IsPartial),
        excludedGeneratedFiles,
        missingSourceFiles
    },
    typeAudit = new
    {
        graphTypeNodeCount = knowledge.Nodes.Count(node => node.Type == "class"),
        mappedTypeNodeCount = nodeMappings.Count,
        uniqueMisclassifiedNodeCount = sourceVsOriginalMismatches.Select(item => item.NodeId).Distinct().Count(),
        autoCorrectedNodeCount = automaticallyCorrectedNodes.Select(item => item.NodeId).Distinct().Count(),
        uncorrectableNodeCount = unmappedTypeNodes.Count + ambiguousTypeNodes.Count,
        excludedGeneratedTypeNodeCount = excludedGraphTypeNodes.Count,
        sourceVsOverlayMismatchCount = sourceVsOverlayMismatches.Count,
        mismatchByType,
        sourceVsOriginalMismatches,
        sourceVsOverlayMismatches,
        automaticallyCorrectedNodes,
        unmappedTypeNodes,
        ambiguousTypeNodes,
        excludedGraphTypeNodes,
        stalePathRemapCount = stalePathRemaps.Count,
        stalePathRemaps,
        sourceDeclarationsMissingGraphNodeCount = declarationsMissingGraphNodes.Count,
        declarationsMissingGraphNodes,
        sourceDeclarationsMissingOverlayNodeCount = declarationsMissingOverlayNodes.Count,
        declarationsMissingOverlayNodes
    },
    relations = new
    {
        violationRelationCount = relationTypeViolations.Count + rawDanglingRelations.Count + overlayDanglingRelations.Count,
        implementsInheritsTypeViolationCount = relationTypeViolations.Count,
        unverifiableImplementsInheritsCount = unverifiableTypedRelations.Count,
        rawDanglingRelationCount = rawDanglingRelations.Count,
        overlayDanglingRelationCount = overlayDanglingRelations.Count,
        relationTypeViolations,
        unverifiableTypedRelations,
        rawDanglingRelations,
        overlayDanglingRelations
    },
    partialMethods = new
    {
        declarationOnlyPartialMethodCount = partialMethods.Count,
        missingFromOriginalGraphCount = missingPartialMethods.Count,
        missingFromOverlayCount = partialMethodsMissingOverlayNodes.Count,
        dreamineEventForwardingMissingCount = missingPartialMethods.Count(item => item.Classification == "dreamine_event_forwarding"),
        unclassifiedCount = partialMethods.Count(item => item.Classification == "unclassified"),
        classificationCounts = partialClassificationCounts,
        missingPartialMethods,
        partialMethodsMissingOverlayNodes
    },
    dreamineEvent = new
    {
        componentDeclarationCount = eventComponentDeclarations.Count,
        componentFileCount = eventComponentFilePaths.Length,
        bindingCount = eventBindings.Count,
        unresolvedBindingCount = unresolvedEventBindings.Count,
        forwardingDeclarationCount = partialMethods.Count(item => item.Classification == "dreamine_event_forwarding"),
        eventComponentNodeIds,
        eventComponentFilePaths,
        eventBindings,
        unresolvedEventBindings
    },
    identity = new
    {
        duplicateStableUriGroupCount = duplicateStableUris.Count,
        duplicateStableUriNodeCount = duplicateStableUris.Sum(item => item.Count - 1),
        stableUriTypeConflictCount = stableUriTypeConflicts.Count,
        duplicateSourceDeclarationNodeGroupCount = duplicateDeclarationNodes.Count,
        duplicateStableUris,
        stableUriTypeConflicts,
        duplicateDeclarationNodes
    },
    canonicalTypePolicy = new
    {
        typeAuthority = "Roslyn source declaration -> source-verified ontology overlay -> raw graph metadata",
        ui = "Preserve raw graph topology and IDs; use overlay rdf:type for labels, filters, and project views.",
        chatbot = "Resolve type questions from source-verified overlay; expose raw graph type only as provenance/diagnostic metadata.",
        unresolved = "Do not guess. Mark the type unverified and retain the raw graph value with lower authority."
    },
    generatedSourcePolicy = new
    {
        policy = "Exclude compiler/designer generated sources from source declaration completeness, architecture rules, UI default search, and chatbot retrieval; retain raw nodes as diagnostic provenance.",
        excludedGeneratedFileCount = excludedGeneratedFiles.Count,
        excludedGraphNodeCount = excludedGraphNodeIds.Length,
        excludedGeneratedFiles,
        excludedGraphNodeIds
    }
};

var sourceDeclarationOutput = new
{
    generatedAt = DateTimeOffset.UtcNow,
    source = "Roslyn CSharpSyntaxTree and semantic Attribute base inspection",
    declarationCounts,
    declarations,
    nodeTypeOverrides,
    nodeMappings,
    declarationsMissingGraphNodes,
    partialMethods,
    eventBindings,
    eventComponentDeclarations,
    eventComponentNodeIds,
    eventComponentFilePaths,
    sourcePathOverrides,
    excludedGeneratedFiles,
    excludedGraphNodeIds,
    unmappedTypeNodes,
    ambiguousTypeNodes,
    excludedGraphTypeNodes,
    policies = new
    {
        generatedSource = "Retain raw diagnostics, exclude from source completeness and default UI/chatbot retrieval.",
        stalePath = "Remap only the deterministic 100. Library -> 200. Tests move when the same relative path and declaration match; otherwise quarantine."
    }
};
var serializerOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
await File.WriteAllTextAsync(Path.Combine(outputDirectory, "source-declarations.json"),
    JsonSerializer.Serialize(sourceDeclarationOutput, serializerOptions));
await File.WriteAllTextAsync(Path.Combine(outputDirectory, "source-audit.json"),
    JsonSerializer.Serialize(report, serializerOptions));
Console.WriteLine(JsonSerializer.Serialize(new
{
    report.scope,
    report.declarations.counts,
    report.typeAudit.uniqueMisclassifiedNodeCount,
    report.typeAudit.autoCorrectedNodeCount,
    report.typeAudit.uncorrectableNodeCount,
    report.typeAudit.sourceVsOverlayMismatchCount,
    report.relations.violationRelationCount,
    report.partialMethods.declarationOnlyPartialMethodCount,
    report.partialMethods.missingFromOriginalGraphCount,
    report.partialMethods.missingFromOverlayCount,
    report.partialMethods.unclassifiedCount,
    report.typeAudit.sourceDeclarationsMissingOverlayNodeCount,
    report.typeAudit.stalePathRemapCount,
    report.generatedSourcePolicy.excludedGraphNodeCount,
    report.identity.duplicateStableUriGroupCount,
    report.identity.stableUriTypeConflictCount
}, serializerOptions));
return 0;

static (string Category, string SyntaxKind, string OntologyType) ClassifyDeclaration(BaseTypeDeclarationSyntax declaration, bool isAttribute) =>
    declaration switch
    {
        ClassDeclarationSyntax when isAttribute => ("attribute", "class", "CodeAttribute"),
        ClassDeclarationSyntax => ("class", "class", "CodeClass"),
        InterfaceDeclarationSyntax => ("interface", "interface", "CodeInterface"),
        RecordDeclarationSyntax record when record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => ("record", "record struct", "CodeRecord"),
        RecordDeclarationSyntax => ("record", "record class", "CodeRecord"),
        StructDeclarationSyntax => ("struct", "struct", "CodeStruct"),
        EnumDeclarationSyntax => ("enum", "enum", "CodeEnum"),
        _ => ("unknown", declaration.Kind().ToString(), "CodeType")
    };

static bool IsAttribute(INamedTypeSymbol? symbol, BaseTypeDeclarationSyntax declaration)
{
    for (var current = symbol?.BaseType; current is not null; current = current.BaseType)
    {
        if (current.Name == nameof(Attribute) && current.ContainingNamespace?.ToDisplayString() == "System")
            return true;
    }
    return declaration.BaseList?.Types.Any(type =>
        type.Type.ToString() is "Attribute" or "System.Attribute" or "global::System.Attribute") == true;
}

static string BuildQualifiedName(BaseTypeDeclarationSyntax declaration)
{
    var names = declaration.Ancestors().OfType<BaseTypeDeclarationSyntax>()
        .Select(type => type.Identifier.ValueText).Reverse().ToList();
    names.Add(declaration.Identifier.ValueText);
    var namespaceName = declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
    return string.IsNullOrWhiteSpace(namespaceName) ? string.Join(".", names) : $"{namespaceName}.{string.Join(".", names)}";
}

static bool AttributeMatches(AttributeSyntax attribute, string expectedName)
{
    var name = attribute.Name.ToString().Split('.').Last();
    return name.Equals(expectedName, StringComparison.Ordinal)
        || name.Equals(expectedName + "Attribute", StringComparison.Ordinal);
}

static string? GetFirstStringArgument(SemanticModel model, AttributeSyntax attribute)
{
    var expression = attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression;
    if (expression is null) return null;
    var constant = model.GetConstantValue(expression);
    return constant.HasValue ? constant.Value as string
        : expression is LiteralExpressionSyntax literal ? literal.Token.ValueText : null;
}

static string? TryMapStaleTestPath(string path)
{
    const string stalePrefix = "20_SOURCES/100. Library/";
    const string currentPrefix = "20_SOURCES/200. Tests/";
    var normalized = Normalize(path);
    return normalized.StartsWith(stalePrefix, StringComparison.OrdinalIgnoreCase)
        ? currentPrefix + normalized[stalePrefix.Length..]
        : null;
}

static SourceDeclaration? ResolveEventComponentDeclaration(
    EventBindingDeclaration binding,
    IReadOnlyList<SourceDeclaration> declarations)
{
    if (binding.ComponentPath is not null)
    {
        var sourceMatch = declarations.FirstOrDefault(declaration =>
            declaration.Path.Equals(binding.ComponentPath, StringComparison.OrdinalIgnoreCase)
            && declaration.Name == binding.ComponentTypeName
            && (binding.ComponentLine is null || declaration.Line == binding.ComponentLine));
        if (sourceMatch is not null) return sourceMatch;
    }

    var exactQualified = declarations.Where(declaration =>
        declaration.QualifiedName == binding.ComponentTypeQualifiedName).ToList();
    if (exactQualified.Count == 1) return exactQualified[0];

    var bindingDirectory = Path.GetDirectoryName(binding.Path.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty;
    return declarations.Where(declaration => declaration.Name == binding.ComponentTypeName)
        .OrderByDescending(declaration => CommonPrefixLength(
            bindingDirectory,
            Path.GetDirectoryName(declaration.Path.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty))
        .FirstOrDefault();
}

static int CommonPrefixLength(string left, string right)
{
    var count = 0;
    var length = Math.Min(left.Length, right.Length);
    while (count < length && char.ToUpperInvariant(left[count]) == char.ToUpperInvariant(right[count])) count++;
    return count;
}

static string OriginalGraphType(GraphNode node) => node.ApiKind?.ToLowerInvariant() switch
{
    "interface" => "CodeInterface",
    "record" or "recordclass" or "recordstruct" => "CodeRecord",
    "struct" => "CodeStruct",
    "enum" => "CodeEnum",
    _ => "CodeClass"
};

static bool HasExcludedSegment(string path)
{
    var segments = Normalize(path).Split('/', StringSplitOptions.RemoveEmptyEntries);
    return segments.Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
        || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)
        || segment.Equals(".git", StringComparison.OrdinalIgnoreCase)
        || segment.Equals(".vs", StringComparison.OrdinalIgnoreCase)
        || segment.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
        || segment.Equals("TestResults", StringComparison.OrdinalIgnoreCase));
}

static bool IsGeneratedSource(string path, string source)
{
    var fileName = Path.GetFileName(path);
    if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase))
        return true;
    var prefix = source.Length > 1024 ? source[..1024] : source;
    return prefix.Contains("<auto-generated", StringComparison.OrdinalIgnoreCase);
}

static string DeclarationKey(string path, string name) => $"{Normalize(path)}\u001f{name}";
static string Normalize(string path) => path.Replace('\\', '/');

static GraphData ReadGraph(string path)
{
    using var document = JsonDocument.Parse(File.ReadAllText(path));
    var nodes = new List<GraphNode>();
    foreach (var item in document.RootElement.GetProperty("nodes").EnumerateArray())
    {
        int? lineStart = null;
        int? lineEnd = null;
        if (item.TryGetProperty("lineRange", out var lineRange) && lineRange.ValueKind == JsonValueKind.Array)
        {
            var values = lineRange.EnumerateArray().Select(value => value.GetInt32()).ToArray();
            if (values.Length > 0) lineStart = values[0];
            if (values.Length > 1) lineEnd = values[1];
        }
        string? apiKind = null;
        string? apiNamespace = null;
        string? containingType = null;
        if (item.TryGetProperty("apiMeta", out var apiMeta) && apiMeta.ValueKind == JsonValueKind.Object)
        {
            apiKind = GetString(apiMeta, "kind");
            apiNamespace = GetString(apiMeta, "namespace");
            containingType = GetString(apiMeta, "containingType");
        }
        nodes.Add(new GraphNode(
            item.GetProperty("id").GetString()!, GetString(item, "type") ?? string.Empty,
            GetString(item, "name") ?? string.Empty, GetString(item, "filePath"), apiKind,
            apiNamespace, containingType, lineStart, lineEnd));
    }
    var edges = new List<GraphEdge>();
    var index = 0;
    foreach (var item in document.RootElement.GetProperty("edges").EnumerateArray())
    {
        edges.Add(new GraphEdge(GetString(item, "id") ?? $"edge:{index++}",
            GetString(item, "type") ?? string.Empty, item.GetProperty("source").GetString()!, item.GetProperty("target").GetString()!));
    }
    return new GraphData(nodes, edges);
}

static InstanceGraph ReadInstances(string path)
{
    using var document = JsonDocument.Parse(File.ReadAllText(path));
    var elements = document.RootElement.GetProperty("elements").EnumerateArray().Select(item =>
        new InstanceElement(item.GetProperty("stable_id").GetString()!, GetString(item, "source_graph_id"),
            GetString(item, "element_type") ?? string.Empty,
            item.GetProperty("@type").EnumerateArray().Select(type => type.GetString()!).ToArray(),
            GetString(item, "canonical_name") ?? string.Empty,
            GetString(item, "source_path"),
            GetInt32(item, "line_start"),
            GetString(item, "forwardTargetName"))).ToList();
    var relations = document.RootElement.GetProperty("relations").EnumerateArray().Select(item =>
        new InstanceRelation(item.GetProperty("stable_id").GetString()!, item.GetProperty("relation_type").GetString()!,
            item.GetProperty("source").GetString()!, item.GetProperty("target").GetString()!)).ToList();
    return new InstanceGraph(elements, relations);
}

static string? GetString(JsonElement element, string propertyName) =>
    element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
static int? GetInt32(JsonElement element, string propertyName) =>
    element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number ? property.GetInt32() : null;

sealed record GraphNode(string Id, string Type, string Name, string? FilePath, string? ApiKind,
    string? ApiNamespace, string? ContainingType, int? LineStart, int? LineEnd);
sealed record GraphEdge(string Id, string Type, string Source, string Target);
sealed record GraphData(IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphEdge> Edges)
{
    public IReadOnlyDictionary<string, GraphNode> NodesById { get; } = Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
}
sealed record SourceDeclaration(string Path, string Name, string QualifiedName, string Category,
    string SyntaxKind, string OntologyType, int Line, bool IsPartial, bool IsAttribute);
sealed record PartialMethodDeclaration(string Path, string? ContainingType, string? ContainingTypeQualifiedName,
    string Name, int Line, int ParameterCount, string Classification, string? ForwardTargetName,
    string? CommandTarget, string? GeneratorAttribute, string? AttributeText);
sealed record EventBindingDeclaration(string Path, string? ContainingType, string? ContainingTypeQualifiedName,
    string FieldName, string ComponentTypeName, string ComponentTypeQualifiedName, int Line,
    string AttributeText, string? ComponentPath, int? ComponentLine);
sealed record NodeDeclarationMapping(string NodeId, string Path, string Name, string QualifiedName,
    int Line, string OntologyType, string? RawPath, bool IsRemapped);
sealed record NodeMappingIssue(string NodeId, string Name, string? Path, int CandidateCount, string Reason);
sealed record TypeMismatch(string NodeId, string Name, string? Path, string ExpectedSourceType,
    string? ActualOriginalType, string? ActualOverlayType, string Scope);
sealed record RelationIssue(string Id, string RelationType, string Source, string Target,
    string? SourceType, string? TargetType, string Reason);
sealed record StableUriIssue(string StableUri, int Count, string[] Types);
sealed record DuplicateDeclarationNode(string DeclarationKey, string[] NodeIds);
sealed record InstanceElement(string StableId, string? SourceGraphId, string ElementType, string[] Types,
    string CanonicalName, string? SourcePath, int? LineStart, string? ForwardTargetName);
sealed record InstanceRelation(string StableId, string RelationType, string Source, string Target);
sealed record InstanceGraph(IReadOnlyList<InstanceElement> Elements, IReadOnlyList<InstanceRelation> Relations);
