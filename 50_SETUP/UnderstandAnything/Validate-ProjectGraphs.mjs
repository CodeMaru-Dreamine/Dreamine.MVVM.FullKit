#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const repositoryRoot = path.resolve(process.argv[2] ?? ".");
const destinationRoot = path.resolve(process.argv[3] ?? path.join(repositoryRoot, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot", "understand"));
const catalogPath = path.join(destinationRoot, "project-catalog.json");
const doxygenRoot = path.join(repositoryRoot, "10_DOCUMENTS", "Doxygen");
const webProjectRoot = path.join(repositoryRoot, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web");
const libraryStorePath = path.join(webProjectRoot, "Services", "JsonLibraryStore.cs");
const projectManifestPath = path.join(path.dirname(fileURLToPath(import.meta.url)), "project-manifest.json");
const isNonEmptyFile = (filePath, minimumBytes = 32) => fs.existsSync(filePath) && fs.statSync(filePath).isFile() && fs.statSync(filePath).size >= minimumBytes;
const excludedSegment = /(?:^|[\\/])(?:bin|obj|\.git|\.vs|node_modules|TestResults)(?:[\\/]|$)/i;
const allowedComplexities = new Set(["simple", "moderate", "complex"]);
const allowedNodeTypes = new Set([
  "file", "function", "class", "module", "concept", "config", "document", "service", "table", "endpoint",
  "pipeline", "schema", "resource", "domain", "flow", "step", "article", "entity", "topic", "claim", "source",
  "page", "screen", "component", "componentSet", "instance", "token",
]);
const allowedEdgeTypes = new Set([
  "imports", "exports", "contains", "inherits", "implements", "calls", "subscribes", "publishes", "middleware",
  "reads_from", "writes_to", "transforms", "validates", "depends_on", "tested_by", "configures", "related",
  "similar_to", "deploys", "serves", "provisions", "triggers", "migrates", "documents", "routes", "defines_schema",
  "contains_flow", "flow_step", "cross_domain", "cites", "contradicts", "builds_on", "exemplifies",
  "categorized_under", "authored_by", "instance_of", "variant_of", "uses_token",
]);
const allowedDirections = new Set(["forward", "backward", "bidirectional"]);
const slash = (value) => String(value ?? "").replaceAll("\\", "/");
const compact = (value) => String(value ?? "").replace(/\s+/g, " ").trim();
const slugify = (value) => compact(value).normalize("NFKD").replace(/[^a-zA-Z0-9]+/g, "-").replace(/^-|-$/g, "").toLowerCase() || "project";
const tag = (xml, name) => compact(xml.match(new RegExp(`<${name}(?:\\s[^>]*)?>([\\s\\S]*?)<\\/${name}>`, "i"))?.[1]);

function validateGraph(graph, label) {
  const nodeIds = new Set(graph.nodes.map((node) => node.id));
  if (nodeIds.size !== graph.nodes.length) throw new Error(`${label}: duplicate node IDs.`);
  const invalidComplexity = graph.nodes.find((node) => !allowedComplexities.has(node.complexity));
  if (invalidComplexity) {
    throw new Error(`${label}: invalid complexity '${invalidComplexity.complexity}' on ${invalidComplexity.id}.`);
  }
  const invalidNode = graph.nodes.find((node) =>
    typeof node.id !== "string" || !allowedNodeTypes.has(node.type) || typeof node.name !== "string"
    || typeof node.summary !== "string" || !Array.isArray(node.tags));
  if (invalidNode) throw new Error(`${label}: dashboard-incompatible node ${invalidNode.id ?? "<missing-id>"}.`);
  if (graph.nodes.some((node) => excludedSegment.test(node.filePath ?? ""))) {
    throw new Error(`${label}: excluded path found in graph.`);
  }
  for (const edge of graph.edges) {
    if (!allowedEdgeTypes.has(edge.type) || !allowedDirections.has(edge.direction)
      || typeof edge.weight !== "number" || edge.weight < 0 || edge.weight > 1) {
      throw new Error(`${label}: dashboard-incompatible edge ${edge.source} -[${edge.type}]-> ${edge.target}.`);
    }
    if (!nodeIds.has(edge.source) || !nodeIds.has(edge.target)) {
      throw new Error(`${label}: dangling edge ${edge.source} -> ${edge.target}.`);
    }
  }
  return nodeIds;
}

if (!fs.existsSync(catalogPath)) throw new Error(`Catalog not found: ${catalogPath}`);
const catalog = JSON.parse(fs.readFileSync(catalogPath, "utf8"));
if (!fs.existsSync(projectManifestPath)) throw new Error(`Project manifest not found: ${projectManifestPath}`);
const manifest = JSON.parse(fs.readFileSync(projectManifestPath, "utf8"));
if (manifest.schemaVersion !== 1 || !Array.isArray(manifest.sourceProjects) || !Array.isArray(manifest.syntheticProjects)) {
  throw new Error("project-manifest.json does not match schema version 1.");
}
const libraryStoreSource = fs.readFileSync(libraryStorePath, "utf8");
const documentationEntries = new Map(
  [...libraryStoreSource.matchAll(/Id\s*=\s*"([^"]+)"\s*,\s*Name\s*=\s*"([^"]+)"/g)]
    .map((match) => [match[2], match[1]]));
const sourceManifestProjects = manifest.sourceProjects.map((entry) => {
  const relativeProjectFile = slash(entry?.projectFile);
  const projectFile = path.resolve(repositoryRoot, relativeProjectFile);
  if (!relativeProjectFile || !projectFile.startsWith(path.join(repositoryRoot, "20_SOURCES") + path.sep) || !fs.existsSync(projectFile)) {
    throw new Error(`Manifest source project is missing or outside 20_SOURCES: ${relativeProjectFile || "<empty>"}`);
  }
  const xml = fs.readFileSync(projectFile, "utf8");
  const baseName = path.basename(projectFile, path.extname(projectFile));
  const name = tag(xml, "PackageId") || tag(xml, "AssemblyName") || tag(xml, "Title") || baseName;
  return { kind: "source", name, slug: slugify(name), projectFile: relativeProjectFile };
});
const syntheticManifestProjects = manifest.syntheticProjects.map((entry) => ({ ...entry, projectFile: null }));
const expectedProjects = [...sourceManifestProjects, ...syntheticManifestProjects];
const duplicateManifestPaths = sourceManifestProjects.filter((item, index) => sourceManifestProjects.findIndex((candidate) => candidate.projectFile === item.projectFile) !== index);
if (duplicateManifestPaths.length) throw new Error(`Duplicate source project paths in manifest: ${duplicateManifestPaths.map((item) => item.projectFile).join(", ")}`);
const duplicateManifestSlugs = expectedProjects.filter((item, index) => expectedProjects.findIndex((candidate) => candidate.slug === item.slug) !== index);
if (duplicateManifestSlugs.length) throw new Error(`Duplicate project slugs in manifest: ${duplicateManifestSlugs.map((item) => item.slug).join(", ")}`);
if (catalog.totalProjects !== expectedProjects.length || catalog.projects?.length !== expectedProjects.length) {
  throw new Error(`Expected ${expectedProjects.length} manifest catalog projects, found ${catalog.projects?.length ?? 0}.`);
}
const expectedProjectByName = new Map(expectedProjects.map((project) => [project.name, project]));
const requiredProductNames = [
  "Dreamine.Secs.Abstractions", "Dreamine.Secs.Com", "Dreamine.Gem.Abstractions",
  "Dreamine.Gem", "Dreamine.Gem300.Abstractions", "Dreamine.Gem300",
];
const requiredProductEntries = manifest.requiredProductProjects ?? [];
if (JSON.stringify(requiredProductEntries.map((entry) => entry.name).sort()) !== JSON.stringify([...requiredProductNames].sort())) {
  throw new Error("The manifest must declare the six SECS/GEM product projects exactly once.");
}
for (const required of requiredProductEntries) {
  const sourceProject = expectedProjectByName.get(required.name);
  if (!sourceProject || sourceProject.kind !== "source" || sourceProject.slug !== required.slug || sourceProject.projectFile !== slash(required.projectFile)) {
    throw new Error(`Required product identity does not match the source manifest: ${required.name}`);
  }
  const documentationId = documentationEntries.get(required.name);
  if (!documentationId || required.documentPageUrl !== `/docs/${encodeURIComponent(documentationId)}`) {
    throw new Error(`Required product documentation route does not match the library catalog: ${required.name}`);
  }
}
const fullKitManifest = syntheticManifestProjects.find((project) => project.name === "Dreamine.SecsGem.FullKit");
if (!fullKitManifest || fullKitManifest.kind !== "meta-package" || fullKitManifest.slug !== "dreamine-secsgem-fullkit"
    || JSON.stringify([...(fullKitManifest.dependencies ?? [])].sort()) !== JSON.stringify([...requiredProductNames].sort())) {
  throw new Error("Dreamine.SecsGem.FullKit must be a synthetic meta-package over the six SECS/GEM products.");
}
const fullKitDocumentationId = documentationEntries.get(fullKitManifest.name);
if (!fullKitDocumentationId || fullKitManifest.documentPageUrl !== `/docs/${encodeURIComponent(fullKitDocumentationId)}`) {
  throw new Error("Dreamine.SecsGem.FullKit documentation route does not match the library catalog.");
}

let graphCount = 0;
let nodeCount = 0;
let edgeCount = 0;
let legacyInvalidLinkCount = 0;
const activeLinks = [];
const unavailable = { documentation: [], doxygen: [], knowledgeGraph: [] };
for (const project of catalog.projects) {
  const expectedProject = expectedProjectByName.get(project.name);
  if (!expectedProject || project.slug !== expectedProject.slug || (project.projectFile ?? null) !== (expectedProject.projectFile ?? null)) {
    throw new Error(`${project.name}: catalog identity does not match project-manifest.json.`);
  }
  const isSynthetic = expectedProject.kind === "meta-package";
  const languageGraphs = {};
  for (const language of ["ko", "en"]) {
    const graphPath = path.join(destinationRoot, "projects", project.slug, language, "knowledge-graph.json");
    if (!fs.existsSync(graphPath)) throw new Error(`Graph not found: ${graphPath}`);
    const graph = JSON.parse(fs.readFileSync(graphPath, "utf8"));
    languageGraphs[language] = graph;
    graphCount++;

    if (graph.project?.outputLanguage !== language) {
      throw new Error(`${project.name}/${language}: outputLanguage does not match.`);
    }
    const nodeIds = validateGraph(graph, `${project.name}/${language}`);
    if (isSynthetic) {
      const dependencyNames = graph.nodes
        .filter((node) => node.id.startsWith("module:external:"))
        .map((node) => node.name)
        .sort();
      const dependencyEdges = graph.edges.filter((edge) => edge.type === "depends_on" && edge.source === `module:${project.slug}`);
      if (JSON.stringify(dependencyNames) !== JSON.stringify([...(fullKitManifest.dependencies ?? [])].sort())
          || dependencyEdges.length !== fullKitManifest.dependencies.length) {
        throw new Error(`${project.name}/${language}: synthetic dependency graph does not match the manifest.`);
      }
    }
    for (const layer of graph.layers ?? []) {
      if ((layer.nodeIds ?? []).some((nodeId) => !nodeIds.has(nodeId))) {
        throw new Error(`${project.name}/${language}: layer contains an unknown node.`);
      }
    }
    for (const step of graph.tour ?? []) {
      if ((step.nodeIds ?? []).some((nodeId) => !nodeIds.has(nodeId))) {
        throw new Error(`${project.name}/${language}: tour contains an unknown node.`);
      }
    }
    nodeCount += graph.nodes.length;
    edgeCount += graph.edges.length;
  }

  const koIds = languageGraphs.ko.nodes.map((node) => node.id).sort();
  const enIds = languageGraphs.en.nodes.map((node) => node.id).sort();
  if (JSON.stringify(koIds) !== JSON.stringify(enIds)) {
    throw new Error(`${project.name}: Korean and English graph topology differs.`);
  }

  const documentationDirectory = path.join(webProjectRoot, "wwwroot", "xmldocs", project.name);
  const xmlPath = path.join(documentationDirectory, `${project.name}.xml`);
  const hasApiMembers = isNonEmptyFile(xmlPath) && /<member\s+name=/i.test(fs.readFileSync(xmlPath, "utf8"));
  const hasEnglishReadme = isNonEmptyFile(path.join(documentationDirectory, "README.md"), 80);
  const hasKoreanReadme = isNonEmptyFile(path.join(documentationDirectory, "README_KO.md"), 80);
  const documentationId = documentationEntries.get(project.name) ?? null;
  const expectedDocumentation = isSynthetic
    ? documentationId !== null && project.documentPageUrl === fullKitManifest.documentPageUrl
    : project.category === "100. Library" && documentationId !== null && (hasApiMembers || hasEnglishReadme || hasKoreanReadme);
  if (project.documentationAvailable !== expectedDocumentation) {
    throw new Error(`${project.name}: DocumentationAvailable does not match generated content.`);
  }
  const expectedDocumentUrl = expectedDocumentation ? `/docs/${encodeURIComponent(documentationId)}` : null;
  if ((project.documentPageUrl ?? null) !== expectedDocumentUrl) {
    throw new Error(`${project.name}: Dreamine documentation route does not match the actual catalog entry.`);
  }
  if (expectedDocumentation) activeLinks.push({ project: project.name, kind: "documentation", language: "both", url: expectedDocumentUrl });
  else unavailable.documentation.push(project.name);

  const doxygenAvailability = {};
  const graphAvailability = {};
  for (const language of ["ko", "en"]) {
    const locale = language === "ko" ? "KR" : "EN";
    const doxygenIndex = path.join(doxygenRoot, project.category, project.name, locale, "html", "index.html");
    doxygenAvailability[language] = !isSynthetic && isNonEmptyFile(doxygenIndex, 100);
    const doxygenUrl = project.doxygenUrls?.[language] ?? "";
    if (doxygenAvailability[language] !== Boolean(doxygenUrl)) {
      throw new Error(`${project.name}/${language}: Doxygen availability and URL differ.`);
    }
    if (doxygenUrl) {
      if (!doxygenUrl.includes(`/${locale}/`) || !doxygenUrl.endsWith("/html/index.html")) {
        throw new Error(`${project.name}/${language}: localized Doxygen URL is invalid.`);
      }
      activeLinks.push({ project: project.name, kind: "doxygen", language, url: doxygenUrl });
    } else {
      legacyInvalidLinkCount += 1;
    }

    const graphDirectory = path.join(destinationRoot, "projects", project.slug, language);
    graphAvailability[language] = languageGraphs[language].nodes.length > 1
      && isNonEmptyFile(path.join(graphDirectory, "knowledge-graph.json"), 100)
      && isNonEmptyFile(path.join(graphDirectory, "config.json"), 2)
      && isNonEmptyFile(path.join(graphDirectory, "meta.json"), 20);
    const graphUrl = project.graphUrls?.[language] ?? "";
    if (graphAvailability[language] !== Boolean(graphUrl)) {
      throw new Error(`${project.name}/${language}: knowledge-graph availability and URL differ.`);
    }
    if (graphUrl) {
      if (!graphUrl.endsWith(`lang=${language}`)) throw new Error(`${project.name}/${language}: localized graph URL is invalid.`);
      activeLinks.push({ project: project.name, kind: "knowledgeGraph", language, url: graphUrl });
    } else {
      legacyInvalidLinkCount += 1;
    }
  }
  if (project.doxygenAvailable !== (doxygenAvailability.ko || doxygenAvailability.en)) {
    throw new Error(`${project.name}: DoxygenAvailable summary is invalid.`);
  }
  if (project.knowledgeGraphAvailable !== (graphAvailability.ko || graphAvailability.en)) {
    throw new Error(`${project.name}: KnowledgeGraphAvailable summary is invalid.`);
  }
  const expectedKoDocumentation = isSynthetic ? expectedDocumentation : expectedDocumentation ? (hasKoreanReadme || hasEnglishReadme) : doxygenAvailability.ko;
  const expectedEnDocumentation = isSynthetic ? expectedDocumentation : expectedDocumentation ? (hasEnglishReadme || hasKoreanReadme) : doxygenAvailability.en;
  if (project.koreanDocumentationAvailable !== expectedKoDocumentation
      || project.englishDocumentationAvailable !== expectedEnDocumentation) {
    throw new Error(`${project.name}: localized documentation availability is invalid.`);
  }
  if (!project.doxygenAvailable) unavailable.doxygen.push(project.name);
  if (!project.knowledgeGraphAvailable) unavailable.knowledgeGraph.push(project.name);

  if (!isSynthetic && project.category === "100. Library" && isNonEmptyFile(xmlPath) && expectedDocumentUrl !== `/docs/${encodeURIComponent(project.slug.replace(/^dreamine-/, ""))}`) {
    legacyInvalidLinkCount += 1;
  }
}

const expectedGraphCount = catalog.projects.length * 2;
if (graphCount !== expectedGraphCount) throw new Error(`Expected ${expectedGraphCount} graphs, found ${graphCount}.`);
for (const language of ["ko", "en"]) {
  const fullGraphPath = path.join(destinationRoot, "full", language, "knowledge-graph.json");
  if (!fs.existsSync(fullGraphPath)) throw new Error(`Full ${language} graph not found: ${fullGraphPath}`);
  const fullGraph = JSON.parse(fs.readFileSync(fullGraphPath, "utf8"));
  if (fullGraph.project?.outputLanguage !== language) throw new Error(`Full ${language} graph language does not match.`);
  validateGraph(fullGraph, `Full/${language}`);
}
const validationReport = {
  generatedAt: new Date().toISOString(),
  totalProjects: catalog.projects.length,
  documentationProjects: catalog.projects.filter((project) => project.documentationAvailable).length,
  doxygenProjects: catalog.projects.filter((project) => project.doxygenAvailable).length,
  knowledgeGraphProjects: catalog.projects.filter((project) => project.knowledgeGraphAvailable).length,
  unavailable,
  activeLinkCount: activeLinks.length,
  activeLinks,
  legacyInvalidLinkCount,
  remainingInvalidLinkCount: 0,
};
fs.writeFileSync(path.join(destinationRoot, "project-catalog-validation.json"), JSON.stringify(validationReport, null, 2));
process.stdout.write(`Validated projects=${catalog.projects.length} graphs=${graphCount} nodes=${nodeCount} edges=${edgeCount} activeLinks=${activeLinks.length} legacyInvalidLinks=${legacyInvalidLinkCount} remainingInvalidLinks=0\n`);
