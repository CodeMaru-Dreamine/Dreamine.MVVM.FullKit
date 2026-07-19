import { createHash } from 'node:crypto';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(process.argv[2] || path.join(scriptDirectory, '..', '..'));
const outputDirectory = path.resolve(process.argv[3] || path.join(repositoryRoot, '.ua', 'ontology'));
const uaDirectory = path.join(repositoryRoot, '.ua');

const readJson = async filePath => JSON.parse(await readFile(filePath, 'utf8'));
const koGraph = await readJson(path.join(uaDirectory, 'knowledge-graph.ko.json'));
const enGraph = await readJson(path.join(uaDirectory, 'knowledge-graph.en.json'));
const domainGraph = await readJson(path.join(uaDirectory, 'domain-graph.json'));
let sourceDeclarationAudit = { nodeTypeOverrides: {} };
try {
  sourceDeclarationAudit = await readJson(path.join(outputDirectory, 'source-declarations.json'));
} catch {
  // The source audit is optional for ad-hoc graph inspection, but the production
  // generation script always runs the Roslyn audit before building the overlay.
}

const enById = new Map(enGraph.nodes.map(node => [node.id, node]));
const normalizePath = value => value.replace(/\\/g, '/');
const sourceTypeByGraphId = new Map(Object.entries(sourceDeclarationAudit.nodeTypeOverrides || {}));
const sourcePathOverrides = new Map(Object.entries(sourceDeclarationAudit.sourcePathOverrides || {})
  .map(([raw, current]) => [normalizePath(raw), normalizePath(current)]));
const excludedGraphNodeIds = new Set(sourceDeclarationAudit.excludedGraphNodeIds || []);
const eventComponentNodeIds = new Set(sourceDeclarationAudit.eventComponentNodeIds || []);
const eventComponentFilePaths = new Set((sourceDeclarationAudit.eventComponentFilePaths || []).map(normalizePath));
const effectiveSourcePath = node => {
  const raw = normalizePath(node.filePath || '');
  return sourcePathOverrides.get(raw) || raw;
};
const classifyType = node => {
  if (node.type === 'pipeline') return 'Pipeline';
  if (node.type === 'document') return 'Document';
  if (node.type === 'file' && eventComponentFilePaths.has(effectiveSourcePath(node))) return 'EventComponentFile';
  if (node.type === 'file') return 'SourceFile';
  if (node.type === 'config') return 'Configuration';
  if (node.type === 'domain') return 'Domain';
  if (node.type === 'flow') return 'Flow';
  if (node.type === 'step') return 'Step';
  if (node.type === 'scenario') return 'Scenario';
  if (node.type === 'function') {
    if (node.apiMeta?.kind === 'constructor') return 'Constructor';
    if (/\/200\. Tests\//i.test(`/${node.filePath || ''}/`)) return 'TestCase';
    return 'Method';
  }
  if (node.type !== 'class') return 'SoftwareElement';

  const kind = node.apiMeta?.kind;
  const name = node.name || '';
  const filePath = effectiveSourcePath(node);
  const bases = node.apiMeta?.baseTypes || [];
  const signature = node.apiMeta?.signature || '';
  const typeText = `${signature} ${bases.join(' ')}`;
  const sourceDeclaredType = sourceTypeByGraphId.get(node.id);
  if (eventComponentNodeIds.has(node.id)) return 'DreamineEventComponent';
  if (sourceDeclaredType && sourceDeclaredType !== 'CodeClass') return sourceDeclaredType;
  if (!sourceDeclaredType && kind === 'interface') return 'CodeInterface';
  if (!sourceDeclaredType && kind === 'record') return 'CodeRecord';
  if (!sourceDeclaredType && kind === 'enum') return 'CodeEnum';
  if (/Attribute$/i.test(name) || /:\s*[^\n]*Attribute\b/.test(signature)) return 'CodeAttribute';
  if (/Exception$/i.test(name) || bases.some(base => /Exception$/i.test(base))) return 'CodeException';
  if (/ViewModel$/i.test(name) || /ViewModel(Base)?\b/.test(typeText) || /[/\\]ViewModels[/\\]/i.test(filePath)) return 'ViewModel';
  if (/Generator$/i.test(name) || /(Incremental|Source)Generator\b/.test(typeText)) return 'Generator';
  if (/Service$/i.test(name) || /[/\\]Services[/\\]/i.test(filePath)) return 'Service';
  if (/[/\\]Views[/\\]/i.test(filePath) || /(Window|Page|View|UserControl)$/i.test(name) || /\b(Window|Page|UserControl|ContentView)\b/.test(typeText)) return 'View';
  return 'CodeClass';
};
const ancestorTypes = {
  Pipeline: ['SourceArtifact', 'SoftwareElement'], Document: ['SourceArtifact', 'SoftwareElement'],
  SourceFile: ['SourceArtifact', 'SoftwareElement'], Configuration: ['SourceArtifact', 'SoftwareElement'],
  EventComponentFile: ['SourceFile', 'SourceArtifact', 'SoftwareElement'],
  CodeClass: ['CodeType', 'CodeElement', 'SoftwareElement'], CodeInterface: ['CodeType', 'CodeElement', 'SoftwareElement'],
  CodeRecord: ['CodeType', 'CodeElement', 'SoftwareElement'], CodeStruct: ['CodeType', 'CodeElement', 'SoftwareElement'],
  CodeEnum: ['CodeType', 'CodeElement', 'SoftwareElement'],
  View: ['CodeClass', 'CodeType', 'CodeElement', 'SoftwareElement'],
  ViewModel: ['CodeClass', 'CodeType', 'CodeElement', 'SoftwareElement'],
  Service: ['CodeClass', 'CodeType', 'CodeElement', 'SoftwareElement'],
  Generator: ['CodeClass', 'CodeType', 'CodeElement', 'SoftwareElement'],
  CodeAttribute: ['CodeClass', 'CodeType', 'CodeElement', 'SoftwareElement'],
  CodeException: ['CodeClass', 'CodeType', 'CodeElement', 'SoftwareElement'],
  DreamineEventComponent: ['CodeClass', 'CodeType', 'CodeElement', 'SoftwareElement'],
  Method: ['CodeMember', 'CodeElement', 'SoftwareElement'], Constructor: ['CodeMember', 'CodeElement', 'SoftwareElement'],
  CodeProperty: ['CodeMember', 'CodeElement', 'SoftwareElement'], Event: ['CodeMember', 'CodeElement', 'SoftwareElement'],
  TestCase: ['CodeMember', 'CodeElement', 'SoftwareElement'],
  Project: ['SoftwareElement'], Module: ['SoftwareElement'], Layer: ['SoftwareElement'],
  Domain: ['SoftwareElement'], Flow: ['SoftwareElement'], Step: ['SoftwareElement'], Scenario: ['SoftwareElement']
};
const layerOf = node => {
  const filePath = effectiveSourcePath(node);
  if (/20_SOURCES[\\/]200\. Tests[\\/]/i.test(filePath)) return 'test';
  if (/20_SOURCES[\\/]998\. DEMO[\\/]/i.test(filePath)) return 'sample';
  if (/20_SOURCES[\\/]100\. Library[\\/]/i.test(filePath)) return 'library';
  if (/20_SOURCES[\\/]000\. Project[\\/]/i.test(filePath)) return 'application';
  return 'tooling';
};
const relationTypes = new Set([
  'contains', 'contains_flow', 'contains_step', 'depends_on', 'documents', 'exports',
  'implements', 'inherits', 'tested_by', 'calls', 'related', 'belongs_to_domain',
  'participates_in_flow', 'precedes', 'uses', 'hasEventComponent', 'forwardsTo',
  'declaredIn', 'companionOf', 'usesModel', 'dependsOn', 'invokesNavigation', 'controlsView'
]);
const clean = value => value === undefined || value === null || value === '' ? undefined : value;
const hash = value => createHash('sha256').update(value).digest('hex').slice(0, 24);
const resourceUri = (kind, key) => `https://dreamine.kr/resource/${kind.toLowerCase()}/${hash(key)}`;
const projectOf = node => node.apiMeta?.project || node.fileMeta?.project || {};
const evidenceSource = node => {
  const trust = node.apiMeta?.trust?.signature || node.fileMeta?.trust || 'unknown';
  return trust === 'code' || trust === 'code-derived' ? 'code' : trust === 'code-inferred' ? 'code_inferred' : 'generated_graph';
};
const canonicalKey = node => {
  const project = projectOf(node);
  const identity = [project.packageId || project.project, node.apiMeta?.namespace, node.apiMeta?.containingType,
    node.apiMeta?.signature || node.name, node.filePath, node.type, node.id].filter(Boolean).join('|');
  return identity || node.id;
};
const stableByGraphId = new Map();

const convertNode = (node, source = 'generated_graph') => {
  const en = enById.get(node.id);
  const elementType = classifyType(node);
  const stableId = resourceUri('element', canonicalKey(node));
  stableByGraphId.set(node.id, stableId);
  const rawSourcePath = clean(node.filePath ? normalizePath(node.filePath) : undefined);
  const verifiedSourcePath = clean(effectiveSourcePath(node));
  const isRemapped = rawSourcePath && verifiedSourcePath && rawSourcePath !== verifiedSourcePath;
  if (node.type === 'file' && verifiedSourcePath) stableByGraphId.set(`file:${verifiedSourcePath}`, stableId);
  const project = projectOf(node);
  const labels = [{ '@type': 'LocalizedText', language: 'ko', text: node.name }];
  if (en?.name && en.name !== node.name) labels.push({ '@type': 'LocalizedText', language: 'en', text: en.name });
  else labels.push({ '@type': 'LocalizedText', language: 'en', text: node.name });
  const summaries = [];
  if (node.summary) summaries.push({ '@type': 'LocalizedText', language: 'ko', text: node.summary });
  if (en?.summary) summaries.push({ '@type': 'LocalizedText', language: 'en', text: en.summary });
  const lineRange = node.lineRange || [];
  return Object.fromEntries(Object.entries({
    '@type': [elementType, ...(ancestorTypes[elementType] || []), 'owl:NamedIndividual'],
    stable_id: stableId,
    source_graph_id: node.id,
    canonical_name: node.name,
    labels,
    summaries,
    element_type: elementType,
    element_layer: layerOf(node),
    source_path: verifiedSourcePath,
    raw_source_path: isRemapped ? rawSourcePath : undefined,
    raw_element_type: node.type === 'class' ? (node.apiMeta?.kind || 'class') : node.type,
    source_verification_status: excludedGraphNodeIds.has(node.id) ? 'excluded_generated'
      : isRemapped ? 'source_remapped'
        : sourceTypeByGraphId.has(node.id) ? 'source_verified' : 'raw',
    default_search_visible: !excludedGraphNodeIds.has(node.id),
    line_start: lineRange[0],
    line_end: lineRange[1],
    project_name: clean(project.project),
    package_id: clean(project.packageId),
    project_version: clean(project.version),
    target_frameworks: project.targetFrameworks || [],
    namespace: clean(node.apiMeta?.namespace),
    signature: clean(node.apiMeta?.signature),
    accessibility: clean(node.apiMeta?.accessibility),
    complexity: ['simple', 'moderate', 'complex'].includes(node.complexity) ? node.complexity : 'unknown',
    tags: node.tags || [],
    evidence: [{ '@type': 'Evidence', evidence_source: source === 'domain_analysis' ? source : evidenceSource(node), evidence_value: node.id, confidence: source === 'domain_analysis' ? 0.9 : 1 }]
  }).filter(([, value]) => value !== undefined));
};

const codeElements = koGraph.nodes.map(node => convertNode(node));
const domainElements = domainGraph.nodes.map(node => convertNode(node, 'domain_analysis'));
const allElements = [...codeElements, ...domainElements];
const elementByStableId = new Map(allElements.map(element => [element.stable_id, element]));
const graphNodeById = new Map(koGraph.nodes.map(node => [node.id, node]));
const directAssertions = new Map();
const addDirectAssertion = (source, predicate, target) => {
  if (!source || !target) return;
  if (!directAssertions.has(source)) directAssertions.set(source, new Map());
  const predicates = directAssertions.get(source);
  if (!predicates.has(predicate)) predicates.set(predicate, new Set());
  predicates.get(predicate).add(target);
};

const allEdges = [...koGraph.edges, ...domainGraph.edges];
const relations = allEdges.flatMap((edge, index) => {
  const source = stableByGraphId.get(edge.source);
  const target = stableByGraphId.get(edge.target);
  if (!source || !target) return [];
  const relationType = relationTypes.has(edge.type) ? edge.type : 'related';
  const stableId = resourceUri('relation', `${source}|${relationType}|${target}|${index}`);
  return [{
    '@type': ['SemanticRelation', 'owl:NamedIndividual'], stable_id: stableId, source, target, relation_type: relationType,
    evidence: [{ '@type': 'Evidence', evidence_source: domainGraph.edges.includes(edge) ? 'domain_analysis' : 'code_inferred', evidence_value: `${edge.source} -> ${edge.target}`, confidence: Number(edge.weight ?? 0.75) }]
  }];
});
for (const relation of relations) {
  if (relation.relation_type === 'depends_on') addDirectAssertion(relation.source, 'dependsOn', relation.target);
  if (relation.relation_type === 'contains') {
    addDirectAssertion(relation.source, 'contains', relation.target);
    addDirectAssertion(relation.target, 'containedBy', relation.source);
  }
}

const addEvidenceRelation = (relationType, source, target, evidenceValue, evidenceKind = 'source_attribute') => {
  if (!source || !target) return;
  const stableId = resourceUri('relation', `${source}|${relationType}|${target}|${evidenceValue}`);
  if (relations.some(relation => relation.stable_id === stableId)) return;
  relations.push({
    '@type': ['SemanticRelation', 'owl:NamedIndividual'],
    stable_id: stableId,
    source,
    target,
    relation_type: relationType,
    evidence: [{ '@type': 'Evidence', evidence_source: evidenceKind, evidence_value: evidenceValue, confidence: 1 }]
  });
  addDirectAssertion(source, relationType, target);
  if (relationType === 'contains') addDirectAssertion(target, 'containedBy', source);
};

const sourceFileStableByPath = new Map();
for (const node of koGraph.nodes.filter(node => node.type === 'file')) {
  const stableId = stableByGraphId.get(node.id);
  if (stableId) sourceFileStableByPath.set(effectiveSourcePath(node), stableId);
}
const projectNodeForPath = sourcePath => koGraph.nodes.find(node =>
  effectiveSourcePath(node) === sourcePath && (projectOf(node).project || projectOf(node).packageId));
const ensureSourceFile = sourcePath => {
  const normalizedPath = normalizePath(sourcePath);
  if (sourceFileStableByPath.has(normalizedPath)) return sourceFileStableByPath.get(normalizedPath);
  const sourceGraphId = `derived:source-file:${normalizedPath}`;
  const stableId = resourceUri('element', `source-file|${normalizedPath}`);
  if (!elementByStableId.has(stableId)) {
    const referenceNode = projectNodeForPath(normalizedPath);
    const project = referenceNode ? projectOf(referenceNode) : {};
    const element = {
      '@type': ['SourceFile', ...(ancestorTypes.SourceFile || []), 'owl:NamedIndividual'],
      stable_id: stableId, source_graph_id: sourceGraphId, canonical_name: path.basename(normalizedPath),
      labels: [
        { '@type': 'LocalizedText', language: 'ko', text: path.basename(normalizedPath) },
        { '@type': 'LocalizedText', language: 'en', text: path.basename(normalizedPath) }
      ],
      element_type: 'SourceFile', element_layer: layerOf({ filePath: normalizedPath }), source_path: normalizedPath,
      source_verification_status: 'derived_source_verified', default_search_visible: true,
      project_name: clean(project.project), package_id: clean(project.packageId), project_version: clean(project.version),
      target_frameworks: project.targetFrameworks || [], complexity: 'unknown', tags: ['source-verified-overlay'],
      evidence: [{ '@type': 'Evidence', evidence_source: 'source_declaration', evidence_value: normalizedPath, confidence: 1 }]
    };
    codeElements.push(element); allElements.push(element); elementByStableId.set(stableId, element);
    stableByGraphId.set(sourceGraphId, stableId);
  }
  sourceFileStableByPath.set(normalizedPath, stableId);
  stableByGraphId.set(`file:${normalizedPath}`, stableId);
  return stableId;
};

const declarationStableByKey = new Map();
const declarationKey = (sourcePath, qualifiedName, line) => `${normalizePath(sourcePath)}|${qualifiedName}|${line}`;
const declarationLooseKey = (sourcePath, qualifiedName) => `${normalizePath(sourcePath)}|${qualifiedName}`;
for (const mapping of sourceDeclarationAudit.nodeMappings || []) {
  const stableId = stableByGraphId.get(mapping.nodeId);
  if (!stableId) continue;
  declarationStableByKey.set(declarationKey(mapping.path, mapping.qualifiedName, mapping.line), stableId);
  declarationStableByKey.set(declarationLooseKey(mapping.path, mapping.qualifiedName), stableId);
}
const eventComponentDeclarationKeys = new Set((sourceDeclarationAudit.eventComponentDeclarations || [])
  .map(declaration => declarationLooseKey(declaration.path, declaration.qualifiedName)));
const ensureSourceDeclaration = declaration => {
  const exactKey = declarationKey(declaration.path, declaration.qualifiedName, declaration.line);
  const looseKey = declarationLooseKey(declaration.path, declaration.qualifiedName);
  const known = declarationStableByKey.get(exactKey) || declarationStableByKey.get(looseKey);
  if (known) return known;

  const elementType = eventComponentDeclarationKeys.has(looseKey) ? 'DreamineEventComponent' : declaration.ontologyType;
  const stableId = resourceUri('element', `source-declaration|${exactKey}`);
  const sourceGraphId = `derived:roslyn-declaration:${hash(exactKey)}`;
  if (!elementByStableId.has(stableId)) {
    const referenceNode = projectNodeForPath(normalizePath(declaration.path));
    const project = referenceNode ? projectOf(referenceNode) : {};
    const namespaceName = declaration.qualifiedName.includes('.')
      ? declaration.qualifiedName.slice(0, declaration.qualifiedName.lastIndexOf('.')) : undefined;
    const element = {
      '@type': [elementType, ...(ancestorTypes[elementType] || []), 'owl:NamedIndividual'],
      stable_id: stableId, source_graph_id: sourceGraphId, canonical_name: declaration.name,
      labels: [
        { '@type': 'LocalizedText', language: 'ko', text: declaration.name },
        { '@type': 'LocalizedText', language: 'en', text: declaration.name }
      ],
      element_type: elementType, element_layer: layerOf({ filePath: declaration.path }),
      source_path: normalizePath(declaration.path), line_start: declaration.line, line_end: declaration.line,
      project_name: clean(project.project), package_id: clean(project.packageId), project_version: clean(project.version),
      target_frameworks: project.targetFrameworks || [], namespace: clean(namespaceName),
      signature: `${declaration.syntaxKind} ${declaration.qualifiedName}`, complexity: 'unknown',
      source_verification_status: 'derived_source_verified', default_search_visible: true,
      tags: ['source-verified-overlay', 'roslyn-declaration'],
      evidence: [{ '@type': 'Evidence', evidence_source: 'source_declaration',
        evidence_value: `${declaration.path}:${declaration.line}:${declaration.syntaxKind} ${declaration.qualifiedName}`, confidence: 1 }]
    };
    codeElements.push(element); allElements.push(element); elementByStableId.set(stableId, element);
    stableByGraphId.set(sourceGraphId, stableId);
  }
  declarationStableByKey.set(exactKey, stableId);
  declarationStableByKey.set(looseKey, stableId);
  const fileStableId = ensureSourceFile(declaration.path);
  addEvidenceRelation('contains', fileStableId, stableId,
    `${declaration.path}:${declaration.line}:${declaration.syntaxKind} ${declaration.qualifiedName}`, 'source_declaration');
  addDirectAssertion(stableId, 'declaredIn', fileStableId);
  return stableId;
};

for (const declaration of sourceDeclarationAudit.declarationsMissingGraphNodes || []) ensureSourceDeclaration(declaration);

const stableForType = (sourcePath, qualifiedName, line = undefined) =>
  (line ? declarationStableByKey.get(declarationKey(sourcePath, qualifiedName, line)) : undefined)
  || declarationStableByKey.get(declarationLooseKey(sourcePath, qualifiedName));
const partialStableByKey = new Map();
const partialKey = method => `${normalizePath(method.path)}|${method.containingTypeQualifiedName}|${method.name}|${method.line}|${method.parameterCount}`;
const ensurePartialMethod = method => {
  const key = partialKey(method);
  if (partialStableByKey.has(key)) return partialStableByKey.get(key);
  const graphNode = koGraph.nodes.find(node => node.type === 'function'
    && effectiveSourcePath(node) === normalizePath(method.path)
    && node.name === method.name
    && (!node.lineRange || (method.line >= node.lineRange[0] && method.line <= node.lineRange[1])));
  if (graphNode) {
    const stableId = stableByGraphId.get(graphNode.id);
    partialStableByKey.set(key, stableId);
    return stableId;
  }

  const stableId = resourceUri('element', `partial-method|${key}`);
  const sourceGraphId = `derived:partial-method:${hash(key)}`;
  const ownerStableId = stableForType(method.path, method.containingTypeQualifiedName);
  const ownerElement = elementByStableId.get(ownerStableId);
  const fileStableId = ensureSourceFile(method.path);
  const tags = ['source-verified-overlay', 'declaration-only-partial', method.classification];
  if (method.generatorAttribute) tags.push(`generator:${method.generatorAttribute}`);
  const element = {
    '@type': ['Method', ...(ancestorTypes.Method || []), 'owl:NamedIndividual'],
    stable_id: stableId, source_graph_id: sourceGraphId, canonical_name: method.name,
    labels: [
      { '@type': 'LocalizedText', language: 'ko', text: method.name },
      { '@type': 'LocalizedText', language: 'en', text: method.name }
    ],
    element_type: 'Method', element_layer: layerOf({ filePath: method.path }), source_path: normalizePath(method.path),
    line_start: method.line, line_end: method.line, project_name: ownerElement?.project_name,
    package_id: ownerElement?.package_id, project_version: ownerElement?.project_version,
    target_frameworks: ownerElement?.target_frameworks || [], namespace: ownerElement?.namespace,
    signature: `partial method ${method.name}(${method.parameterCount} parameter(s))`, accessibility: 'private', complexity: 'simple',
    source_verification_status: 'derived_source_verified', default_search_visible: true, tags,
    partialClassification: method.classification, generatorAttribute: clean(method.generatorAttribute),
    forwardTargetName: clean(method.forwardTargetName),
    evidence: [{ '@type': 'Evidence', evidence_source: method.attributeText ? 'source_attribute' : 'source_declaration',
      evidence_value: `${method.path}:${method.line}:${method.attributeText || `partial ${method.name}`}`, confidence: 1 }]
  };
  codeElements.push(element); allElements.push(element); elementByStableId.set(stableId, element);
  stableByGraphId.set(sourceGraphId, stableId); partialStableByKey.set(key, stableId);
  addEvidenceRelation('contains', ownerStableId, stableId, `${method.path}:${method.line}:partial ${method.name}`, 'source_declaration');
  addDirectAssertion(stableId, 'declaredIn', fileStableId);
  return stableId;
};
for (const method of sourceDeclarationAudit.partialMethods || []) ensurePartialMethod(method);

const eventPattern = {
  eventComponents: (sourceDeclarationAudit.eventComponentDeclarations || []).length,
  eventFiles: eventComponentFilePaths.size,
  eventBindings: (sourceDeclarationAudit.eventBindings || []).length,
  forwardingDeclarations: (sourceDeclarationAudit.partialMethods || []).filter(method => method.classification === 'dreamine_event_forwarding').length,
  resolvedForwardings: 0, unresolvedForwardings: []
};
for (const binding of sourceDeclarationAudit.eventBindings || []) {
  const viewModelStableId = stableForType(binding.path, binding.containingTypeQualifiedName);
  const eventComponentStableId = binding.componentPath
    ? stableForType(binding.componentPath, binding.componentTypeQualifiedName, binding.componentLine) : undefined;
  if (!viewModelStableId || !eventComponentStableId) {
    eventPattern.unresolvedForwardings.push({ binding, reason: 'DreamineEvent binding endpoint was not source-resolved' });
    continue;
  }
  addEvidenceRelation('hasEventComponent', viewModelStableId, eventComponentStableId,
    `${binding.path}:${binding.line}:${binding.attributeText} ${binding.componentTypeName} ${binding.fieldName}`);
  const viewModelFileStableId = ensureSourceFile(binding.path);
  const eventFileStableId = ensureSourceFile(binding.componentPath);
  addDirectAssertion(eventComponentStableId, 'declaredIn', eventFileStableId);
  addDirectAssertion(eventFileStableId, 'companionOf', viewModelFileStableId);
  if (/\.Event\.cs$/i.test(binding.componentPath)) {
    const xamlPath = binding.componentPath.replace(/\.Event\.cs$/i, '');
    addDirectAssertion(eventFileStableId, 'companionOf', sourceFileStableByPath.get(xamlPath));
  }

  const sourceText = await readFile(path.join(repositoryRoot, ...normalizePath(binding.path).split('/')), 'utf8');
  const modelField = sourceText.match(/\[DreamineModel\]\s*(?:private|protected|public|internal)\s+([A-Za-z_][A-Za-z0-9_.<>]*)\s+[A-Za-z_][A-Za-z0-9_]*\s*;/s);
  if (modelField) {
    const modelName = modelField[1].split('.').pop();
    const ownerProject = elementByStableId.get(viewModelStableId)?.project_name;
    const modelClassNode = koGraph.nodes.find(node => node.type === 'class' && node.name === modelName
      && (!ownerProject || projectOf(node).project === ownerProject));
    addDirectAssertion(viewModelStableId, 'usesModel', stableByGraphId.get(modelClassNode?.id));
  }

  const viewCodePath = binding.componentPath.replace(/\.Event\.cs$/i, '.cs');
  const viewName = path.basename(binding.componentPath).replace(/\.xaml\.Event\.cs$/i, '');
  const viewClassNode = koGraph.nodes.find(node => node.type === 'class'
    && effectiveSourcePath(node) === viewCodePath && node.name === viewName);
  addDirectAssertion(eventComponentStableId, 'controlsView', stableByGraphId.get(viewClassNode?.id));

  const eventClassNode = koGraph.nodes.find(node => node.type === 'class'
    && effectiveSourcePath(node) === normalizePath(binding.componentPath)
    && node.name === binding.componentTypeName);
  if (eventClassNode) {
    const eventSourceText = await readFile(path.join(repositoryRoot, ...normalizePath(binding.componentPath).split('/')), 'utf8');
    for (const eventMethodId of eventClassNode.apiMeta?.methods || []) {
      const eventMethodNode = graphNodeById.get(eventMethodId);
      if (!eventMethodNode?.lineRange) continue;
      const lines = eventSourceText.split(/\r?\n/).slice(eventMethodNode.lineRange[0] - 1, eventMethodNode.lineRange[1]);
      for (const navigation of lines.join('\n').matchAll(/_viewManager\.Show<([A-Za-z_][A-Za-z0-9_.]*)>/g)) {
        const targetViewModelName = navigation[1].split('.').pop();
        const targetViewModelNode = koGraph.nodes.find(node => node.type === 'class' && node.name === targetViewModelName);
        addDirectAssertion(stableByGraphId.get(eventMethodNode.id), 'invokesNavigation', stableByGraphId.get(targetViewModelNode?.id));
      }
    }
  }
}

for (const method of (sourceDeclarationAudit.partialMethods || []).filter(item => item.classification === 'dreamine_event_forwarding')) {
  const binding = (sourceDeclarationAudit.eventBindings || []).find(candidate =>
    normalizePath(candidate.path) === normalizePath(method.path)
    && candidate.containingTypeQualifiedName === method.containingTypeQualifiedName);
  const sourceMethodStableId = ensurePartialMethod(method);
  const targetMethodNode = binding?.componentPath ? koGraph.nodes.find(node => node.type === 'function'
    && effectiveSourcePath(node) === normalizePath(binding.componentPath)
    && node.name === method.forwardTargetName
    && (!node.apiMeta?.containingType
      || node.apiMeta.containingType === binding.componentTypeName
      || node.apiMeta.containingType.endsWith(`:${binding.componentTypeName}`))) : undefined;
  const targetMethodStableId = stableByGraphId.get(targetMethodNode?.id);
  if (!binding || !sourceMethodStableId || !targetMethodStableId) {
    eventPattern.unresolvedForwardings.push({ method, binding: binding || null, reason: 'Forwarding target method was not found on the connected Event Component' });
    continue;
  }
  eventPattern.resolvedForwardings++;
  addEvidenceRelation('forwardsTo', sourceMethodStableId, targetMethodStableId,
    `${method.path}:${method.line}:[DreamineCommand("Event.${method.forwardTargetName}")]`);
  addDirectAssertion(targetMethodStableId, 'declaredIn', ensureSourceFile(binding.componentPath));
}

for (const element of codeElements) {
  if (!element.source_path || !element['@type'].includes('CodeElement')) continue;
  addDirectAssertion(element.stable_id, 'declaredIn', stableByGraphId.get(`file:${normalizePath(element.source_path)}`));
}
const elementIds = new Set(allElements.map(element => element.stable_id));
if (elementIds.size !== allElements.length) {
  throw new Error(`Stable URI collision detected: ${allElements.length - elementIds.size} duplicate element identifiers.`);
}
const brokenRelations = relations.filter(relation => !elementIds.has(relation.source) || !elementIds.has(relation.target));
if (brokenRelations.length > 0) {
  throw new Error(`Relationship integrity failed: ${brokenRelations.length} endpoint(s) are missing.`);
}
const directAssertionNodes = [...directAssertions.entries()].map(([source, predicates]) => ({
  '@id': source,
  ...Object.fromEntries([...predicates.entries()].map(([predicate, targets]) => [
    predicate,
    [...targets].map(target => ({ '@id': target }))
  ]))
}));

const generatedAt = new Date().toISOString();
const instance = { ontology_version: '1.0.0', generated_at: generatedAt, elements: allElements, relations };
const context = {
  '@vocab': 'https://dreamine.kr/ontology/',
  dreamine: 'https://dreamine.kr/ontology/', xsd: 'http://www.w3.org/2001/XMLSchema#', owl: 'http://www.w3.org/2002/07/owl#', prov: 'http://www.w3.org/ns/prov#',
  stable_id: '@id', labels: { '@id': 'dreamine:labels' }, summaries: { '@id': 'dreamine:summaries' },
  confidence: { '@id': 'dreamine:confidence', '@type': 'xsd:decimal' },
  source: { '@type': '@id' }, target: { '@type': '@id' }
};
const decimalLexical = value => Number.isInteger(Number(value)) ? Number(value).toFixed(1) : String(value);
const asJsonLdResource = resource => {
  const { stable_id: stableId, ...properties } = resource;
  return {
    '@id': stableId,
    ...properties,
    evidence: resource.evidence?.map(item => ({
      ...item,
      confidence: item.confidence === undefined ? undefined : decimalLexical(item.confidence)
    }))
  };
};
const jsonld = { '@context': context, '@graph': [
  ...allElements.map(asJsonLdResource),
  ...relations.map(asJsonLdResource),
  ...directAssertionNodes
] };
const sampleIds = new Set();
for (const type of new Set(allElements.map(element => element.element_type))) {
  allElements.filter(element => element.element_type === type).slice(0, 5)
    .forEach(element => sampleIds.add(element.stable_id));
}
domainElements.forEach(element => sampleIds.add(element.stable_id));
const sampleRelations = [];
for (const relationType of relationTypes) {
  const relation = relations.find(candidate => candidate.relation_type === relationType);
  if (!relation) continue;
  sampleRelations.push(relation);
  sampleIds.add(relation.source);
  sampleIds.add(relation.target);
}
const sampleElements = allElements.filter(element => sampleIds.has(element.stable_id));
const validationSample = { '@context': context, '@graph': [
  ...sampleElements.map(asJsonLdResource),
  ...sampleRelations.map(asJsonLdResource)
] };
const architectureProjection = { '@context': context, '@graph': [
  ...allElements.map(element => ({
    '@id': element.stable_id,
    '@type': element['@type'],
    canonical_name: element.canonical_name,
    element_layer: element.element_layer,
    forwardTargetName: element.forwardTargetName
  })),
  ...relations.map(relation => ({
    '@id': relation.stable_id,
    '@type': relation['@type'],
    relation_type: relation.relation_type,
    source: relation.source,
    target: relation.target
  })),
  ...directAssertionNodes
] };
const overlay = Object.fromEntries(allElements.map(element => [element.source_graph_id, {
  stableId: element.stable_id,
  type: element.element_type,
  types: element['@type'],
  project: element.project_name || null,
  sourcePath: element.source_path || null,
  rawSourcePath: element.raw_source_path || null,
  rawType: element.raw_element_type || null,
  sourceVerificationStatus: element.source_verification_status || 'raw',
  defaultSearchVisible: element.default_search_visible !== false
}]));
const typeCounts = Object.fromEntries([...new Set(allElements.map(element => element.element_type))].sort()
  .map(type => [type, allElements.filter(element => element.element_type === type).length]));
const manifest = {
  ontologyVersion: '1.0.0', generatedAt, sourceGraphVersion: koGraph.version,
  counts: { elements: allElements.length, relations: relations.length, domains: domainElements.filter(x => x.element_type === 'Domain').length },
  dreamineEventPattern: eventPattern,
  types: typeCounts,
  validation: { scope: 'representative-types-and-relations', elements: sampleElements.length, relations: sampleRelations.length },
  artifacts: ['dreamine.schema.json', 'dreamine.context.jsonld', 'dreamine.owl.ttl', 'dreamine.shacl.ttl', 'instances.json', 'instances.jsonld', 'validation-sample.jsonld', 'architecture-projection.jsonld', 'overlay.json']
};

await mkdir(outputDirectory, { recursive: true });
await Promise.all([
  writeFile(path.join(outputDirectory, 'instances.json'), JSON.stringify(instance, null, 2)),
  writeFile(path.join(outputDirectory, 'instances.jsonld'), JSON.stringify(jsonld, null, 2)),
  writeFile(path.join(outputDirectory, 'validation-sample.jsonld'), JSON.stringify(validationSample, null, 2)),
  writeFile(path.join(outputDirectory, 'architecture-projection.jsonld'), JSON.stringify(architectureProjection)),
  writeFile(path.join(outputDirectory, 'overlay.json'), JSON.stringify(overlay)),
  writeFile(path.join(outputDirectory, 'manifest.json'), JSON.stringify(manifest, null, 2))
]);
console.log(JSON.stringify(manifest, null, 2));
