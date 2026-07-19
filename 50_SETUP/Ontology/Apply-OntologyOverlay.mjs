#!/usr/bin/env node

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(process.argv[2] || path.join(scriptDirectory, '..', '..'));
const uaDirectory = path.join(repositoryRoot, '.ua');
const ontologyDirectory = path.join(uaDirectory, 'ontology');
const slash = value => String(value || '').replaceAll('\\', '/');
const readJson = filePath => JSON.parse(fs.readFileSync(filePath, 'utf8'));
const clone = value => JSON.parse(JSON.stringify(value));

const instances = readJson(path.join(ontologyDirectory, 'instances.json'));
const overlay = readJson(path.join(ontologyDirectory, 'overlay.json'));
const elementByStableId = new Map(instances.elements.map(element => [element.stable_id, element]));
const graphIdByStableId = new Map(instances.elements.map(element => [element.stable_id, element.source_graph_id]));

const codeTypeKinds = {
  CodeClass: 'class', View: 'class', ViewModel: 'class', Service: 'class', Generator: 'class',
  CodeException: 'class', DreamineEventComponent: 'class', CodeAttribute: 'attribute',
  CodeInterface: 'interface', CodeRecord: 'record', CodeStruct: 'struct', CodeEnum: 'enum',
  Method: 'method', Constructor: 'constructor', TestCase: 'method'
};
const graphTypeFor = elementType => {
  if (['Method', 'Constructor', 'TestCase', 'CodeProperty', 'Event'].includes(elementType)) return 'function';
  if (['CodeClass', 'CodeInterface', 'CodeRecord', 'CodeStruct', 'CodeEnum', 'View', 'ViewModel', 'Service',
    'Generator', 'CodeAttribute', 'CodeException', 'DreamineEventComponent'].includes(elementType)) return 'class';
  if (['SourceFile', 'EventComponentFile'].includes(elementType)) return 'file';
  return String(elementType || 'concept').toLowerCase();
};
const localized = (values, language, fallback) =>
  values?.find(value => value.language === language)?.text
  || values?.find(value => value.language === 'en')?.text
  || values?.[0]?.text || fallback;
const semanticTags = element => [...new Set([
  ...(element.tags || []),
  `ontology:${element.element_type}`,
  `verification:${element.source_verification_status || 'raw'}`
])];
const dashboardEdgeTypeFor = relationType => ({
  hasEventComponent: 'depends_on',
  forwardsTo: 'calls'
}[relationType] || relationType);

function apply(language) {
  const inputPath = path.join(uaDirectory, `knowledge-graph.${language}.json`);
  const graph = readJson(inputPath);
  const nodes = [];
  const nodeIds = new Set();

  for (const rawNode of graph.nodes) {
    const metadata = overlay[rawNode.id];
    if (metadata?.defaultSearchVisible === false) continue;
    const node = clone(rawNode);
    node.rawType = rawNode.type;
    node.rawApiKind = rawNode.apiMeta?.kind || null;
    if (metadata) {
      node.ontologyType = metadata.type;
      node.ontologyTypes = metadata.types;
      node.sourceVerificationStatus = metadata.sourceVerificationStatus;
      node.defaultSearchVisible = metadata.defaultSearchVisible;
      node.stableUri = metadata.stableId;
      node.rawFilePath = metadata.rawSourcePath || null;
      if (metadata.sourcePath) node.filePath = slash(metadata.sourcePath);
      node.tags = [...new Set([...(node.tags || []), `ontology:${metadata.type}`,
        `verification:${metadata.sourceVerificationStatus}`])];
      if (node.type === 'class' && codeTypeKinds[metadata.type]) {
        node.apiMeta = { ...(node.apiMeta || {}), kind: codeTypeKinds[metadata.type], rawKind: node.rawApiKind };
      }
    } else {
      node.sourceVerificationStatus = 'raw';
      node.defaultSearchVisible = true;
    }
    nodes.push(node);
    nodeIds.add(node.id);
  }

  for (const element of instances.elements) {
    if (!element.source_graph_id?.startsWith('derived:') || element.default_search_visible === false) continue;
    if (nodeIds.has(element.source_graph_id)) continue;
    const graphType = graphTypeFor(element.element_type);
    const project = {
      project: element.project_name || null,
      packageId: element.package_id || null,
      version: element.project_version || null,
      targetFrameworks: element.target_frameworks || []
    };
    const node = {
      id: element.source_graph_id,
      type: graphType,
      rawType: null,
      rawApiKind: null,
      ontologyType: element.element_type,
      ontologyTypes: element['@type'],
      stableUri: element.stable_id,
      sourceVerificationStatus: element.source_verification_status || 'derived_source_verified',
      defaultSearchVisible: true,
      name: localized(element.labels, language, element.canonical_name),
      summary: localized(element.summaries, language,
        language === 'ko' ? 'Roslyn 소스 선언에서 검증·보강된 요소입니다.' : 'Source-verified element enriched from a Roslyn declaration.'),
      filePath: slash(element.source_path),
      lineRange: element.line_start ? [element.line_start, element.line_end || element.line_start] : undefined,
      tags: semanticTags(element),
      complexity: ['simple', 'moderate', 'complex'].includes(element.complexity)
        ? element.complexity
        : 'simple',
      apiMeta: ['class', 'function'].includes(graphType) ? {
        kind: codeTypeKinds[element.element_type] || graphType,
        namespace: element.namespace || null,
        signature: element.signature || element.canonical_name,
        accessibility: element.accessibility || null,
        project
      } : undefined,
      fileMeta: graphType === 'file' ? { project } : undefined
    };
    nodes.push(Object.fromEntries(Object.entries(node).filter(([, value]) => value !== undefined)));
    nodeIds.add(node.id);
  }

  const edges = graph.edges.filter(edge => nodeIds.has(edge.source) && nodeIds.has(edge.target)).map(edge => clone(edge));
  const edgeKeys = new Set(edges.map(edge => `${edge.type}|${edge.source}|${edge.target}`));
  for (const relation of instances.relations) {
    const source = graphIdByStableId.get(relation.source);
    const target = graphIdByStableId.get(relation.target);
    if (!source || !target || !nodeIds.has(source) || !nodeIds.has(target)) continue;
    const key = `${relation.relation_type}|${source}|${target}`;
    if (edgeKeys.has(key)) continue;
    edgeKeys.add(key);
    edges.push({
      id: relation.source_graph_id || relation.stable_id,
      source,
      target,
      type: dashboardEdgeTypeFor(relation.relation_type),
      direction: 'forward',
      weight: 1,
      description: language === 'ko'
        ? `source-verified Overlay 관계 (${relation.relation_type})`
        : `source-verified Overlay relationship (${relation.relation_type})`,
      ontologyRelationType: relation.relation_type,
      ontologyStableUri: relation.stable_id
    });
  }

  const visibleNodeIds = new Set(nodes.map(node => node.id));
  const layers = (graph.layers || []).map(layer => ({
    ...layer,
    nodeIds: (layer.nodeIds || []).filter(id => visibleNodeIds.has(id))
  }));
  const assigned = new Set(layers.flatMap(layer => layer.nodeIds || []));
  const derivedIds = nodes.filter(node => node.id.startsWith('derived:') && !assigned.has(node.id)).map(node => node.id);
  if (derivedIds.length) {
    layers.push({
      id: 'source-verified-overlay',
      name: language === 'ko' ? '소스 검증 Overlay' : 'Source-verified Overlay',
      description: language === 'ko'
        ? 'Roslyn 선언 감사에서 원본 그래프 누락을 보강한 코드 요소입니다.'
        : 'Code elements added from the Roslyn declaration audit.',
      nodeIds: derivedIds
    });
  }
  const result = {
    ...graph,
    project: {
      ...(graph.project || {}),
      sourceAuthority: 'Roslyn source declaration -> source-verified ontology overlay -> raw graph metadata',
      ontologyOverlayApplied: true,
      ontologyOverlayGeneratedAt: instances.generated_at
    },
    nodes,
    edges,
    layers,
    diagnostics: {
      ...(graph.diagnostics || {}),
      rawNodeCount: graph.nodes.length,
      visibleNodeCount: nodes.length,
      sourceVerifiedDerivedNodeCount: nodes.filter(node => node.id.startsWith('derived:')).length,
      excludedDefaultSearchNodeCount: graph.nodes.filter(node => overlay[node.id]?.defaultSearchVisible === false).length
    }
  };
  const outputPath = path.join(uaDirectory, `knowledge-graph.source-verified.${language}.json`);
  fs.writeFileSync(outputPath, JSON.stringify(result, null, 2));
  return { language, outputPath, nodes: nodes.length, edges: edges.length, diagnostics: result.diagnostics };
}

const results = ['ko', 'en'].map(apply);
fs.copyFileSync(path.join(uaDirectory, 'knowledge-graph.source-verified.ko.json'), path.join(uaDirectory, 'knowledge-graph.json'));
fs.writeFileSync(path.join(ontologyDirectory, 'consumer-precedence.json'), JSON.stringify({
  generatedAt: new Date().toISOString(),
  authority: 'Roslyn source declaration -> source-verified ontology overlay -> raw graph metadata',
  chatbot: '.ua/knowledge-graph.json',
  ui: {
    ko: '.ua/knowledge-graph.source-verified.ko.json',
    en: '.ua/knowledge-graph.source-verified.en.json'
  },
  raw: {
    ko: '.ua/knowledge-graph.ko.json',
    en: '.ua/knowledge-graph.en.json'
  },
  results
}, null, 2));
console.log(JSON.stringify(results, null, 2));
