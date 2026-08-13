#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";

const repositoryRoot = path.resolve(process.argv[2] ?? ".");
const destinationRoot = path.resolve(process.argv[3] ?? path.join(
  repositoryRoot,
  "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot", "understand"));
const sourceRoot = path.join(destinationRoot, "source");
const sourcesBoundary = path.join(repositoryRoot, "20_SOURCES") + path.sep;
const slash = (value) => String(value ?? "").replaceAll("\\", "/");
const normalizeRelativePath = (value) => slash(value).replace(/^\/+/, "").split("/").filter(Boolean).join("/");
const isExcludedPath = (value) => /(?:^|\/)(?:bin|obj|\.git|\.vs|node_modules|TestResults)(?:\/|$)/i.test(slash(value));

function listFiles(root, predicate) {
  const result = [];
  if (!fs.existsSync(root)) return result;
  const pending = [root];
  while (pending.length > 0) {
    const current = pending.pop();
    for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
      const fullPath = path.join(current, entry.name);
      if (entry.isDirectory()) pending.push(fullPath);
      else if (entry.isFile() && predicate(fullPath)) result.push(fullPath);
    }
  }
  return result.sort();
}

const uaRoot = path.join(repositoryRoot, ".ua");
const activeUaGraphs = fs.existsSync(uaRoot)
  ? fs.readdirSync(uaRoot, { withFileTypes: true })
      .filter((entry) => entry.isFile() && /knowledge-graph.*\.json$/i.test(entry.name))
      .map((entry) => path.join(uaRoot, entry.name))
      .sort()
  : [];
const graphFiles = [
  // Only active top-level UA graphs are authoritative. Recursive traversal
  // would incorrectly audit recoverable `.trash-*` snapshots and the bundled
  // dashboard demo graph as if they were current publication inputs.
  ...activeUaGraphs,
  ...listFiles(destinationRoot, (filePath) => path.basename(filePath).toLowerCase() === "knowledge-graph.json"),
];
const graphIssues = [];
const graphWarnings = [];
const sourceReferences = new Set();
let totalNodes = 0;
let totalEdges = 0;
let duplicateNodeIds = 0;
let danglingEdges = 0;
let danglingLayerReferences = 0;
let danglingTourReferences = 0;

for (const graphPath of graphFiles) {
  const label = slash(path.relative(repositoryRoot, graphPath));
  let graph;
  try {
    graph = JSON.parse(fs.readFileSync(graphPath, "utf8"));
  } catch (error) {
    graphIssues.push(`${label}: invalid JSON (${error instanceof Error ? error.message : String(error)})`);
    continue;
  }
  if (!Array.isArray(graph.nodes) || !Array.isArray(graph.edges)) {
    graphIssues.push(`${label}: nodes or edges is not an array`);
    continue;
  }
  totalNodes += graph.nodes.length;
  totalEdges += graph.edges.length;
  const nodeIds = new Set();
  for (const [index, node] of graph.nodes.entries()) {
    if (!node || typeof node.id !== "string" || !node.id) {
      graphIssues.push(`${label}: node[${index}] has no id`);
      continue;
    }
    if (nodeIds.has(node.id)) {
      duplicateNodeIds += 1;
      graphIssues.push(`${label}: duplicate node id ${node.id}`);
    }
    nodeIds.add(node.id);
    const sourcePath = normalizeRelativePath(node.filePath);
    if (sourcePath) {
      if (isExcludedPath(sourcePath)) graphIssues.push(`${label}: excluded path appears in node ${node.id}`);
      if (sourcePath.toLowerCase().startsWith("20_sources/")) sourceReferences.add(sourcePath);
    }
  }
  for (const [index, edge] of graph.edges.entries()) {
    if (!nodeIds.has(edge?.source) || !nodeIds.has(edge?.target)) {
      danglingEdges += 1;
      graphIssues.push(`${label}: edge[${index}] has a missing endpoint`);
    }
  }
  for (const layer of graph.layers ?? []) {
    for (const nodeId of layer?.nodeIds ?? []) {
      if (!nodeIds.has(nodeId)) {
        danglingLayerReferences += 1;
        graphIssues.push(`${label}: layer ${layer?.id ?? "<unknown>"} references ${nodeId}`);
      }
    }
  }
  for (const step of graph.tour ?? []) {
    for (const nodeId of step?.nodeIds ?? []) {
      if (!nodeIds.has(nodeId)) {
        danglingTourReferences += 1;
        graphIssues.push(`${label}: tour step ${step?.order ?? "<unknown>"} references ${nodeId}`);
      }
    }
  }
  if (graph.nodes.length === 0) graphWarnings.push(`${label}: graph has no nodes`);
}

const manifestPath = path.join(destinationRoot, "source-manifest.json");
const manifest = fs.existsSync(manifestPath) ? JSON.parse(fs.readFileSync(manifestPath, "utf8")) : {};
const policyUnavailable = new Set([
  ...(manifest.blockedBySecretScan ?? []),
  ...(manifest.excludedByPolicy ?? []),
  ...(manifest.missingSourceVerifiedFiles ?? []),
  ...(manifest.missingScanFiles ?? []),
].map((item) => normalizeRelativePath(typeof item === "string" ? item : item?.path).toLowerCase()).filter(Boolean));

let sourceFilesThatExist = 0;
let expectedSourceMirrors = 0;
let availableSourceMirrors = 0;
let policyUnavailableSources = 0;
let missingExpectedSourceMirrors = 0;
let malformedSourceMirrors = 0;
let unsafeSourcePaths = 0;
const missingExpectedExamples = [];

for (const relativePath of [...sourceReferences].sort()) {
  const normalized = normalizeRelativePath(relativePath);
  const sourcePath = path.resolve(repositoryRoot, ...normalized.split("/"));
  if (!sourcePath.startsWith(sourcesBoundary)) {
    unsafeSourcePaths += 1;
    graphIssues.push(`Unsafe source path in graph: ${normalized}`);
    continue;
  }
  if (!fs.existsSync(sourcePath) || !fs.statSync(sourcePath).isFile()) continue;
  sourceFilesThatExist += 1;
  const mirrorPath = path.resolve(sourceRoot, ...normalized.split("/")) + ".json";
  if (!mirrorPath.startsWith(sourceRoot + path.sep)) {
    unsafeSourcePaths += 1;
    graphIssues.push(`Unsafe source mirror path: ${normalized}`);
    continue;
  }
  if (policyUnavailable.has(normalized.toLowerCase())) {
    policyUnavailableSources += 1;
    continue;
  }
  expectedSourceMirrors += 1;
  if (!fs.existsSync(mirrorPath)) {
    missingExpectedSourceMirrors += 1;
    if (missingExpectedExamples.length < 25) missingExpectedExamples.push(normalized);
    continue;
  }
  try {
    const mirror = JSON.parse(fs.readFileSync(mirrorPath, "utf8"));
    if (normalizeRelativePath(mirror.path).toLowerCase() !== normalized.toLowerCase()
        || typeof mirror.content !== "string"
        || !Number.isInteger(mirror.lineCount)) {
      throw new Error("required fields are missing or inconsistent");
    }
    availableSourceMirrors += 1;
  } catch (error) {
    malformedSourceMirrors += 1;
    graphIssues.push(`${slash(path.relative(repositoryRoot, mirrorPath))}: malformed source mirror (${error instanceof Error ? error.message : String(error)})`);
  }
}

if (missingExpectedSourceMirrors > 0) {
  graphIssues.push(`Missing ${missingExpectedSourceMirrors} expected source mirrors: ${missingExpectedExamples.join(", ")}`);
}

const report = {
  generatedAt: new Date().toISOString(),
  graphFiles: graphFiles.length,
  totalNodes,
  totalEdges,
  duplicateNodeIds,
  danglingEdges,
  danglingLayerReferences,
  danglingTourReferences,
  uniqueSourcePaths: sourceReferences.size,
  sourceFilesThatExist,
  expectedSourceMirrors,
  availableSourceMirrors,
  policyUnavailableSources,
  missingExpectedSourceMirrors,
  malformedSourceMirrors,
  unsafeSourcePaths,
  issueCount: graphIssues.length,
  warningCount: graphWarnings.length,
  issues: graphIssues.slice(0, 200),
  warnings: graphWarnings.slice(0, 100),
};
fs.writeFileSync(path.join(destinationRoot, "knowledge-graph-validation.json"), JSON.stringify(report, null, 2));
process.stdout.write(`Graphs=${report.graphFiles} Nodes=${report.totalNodes} Edges=${report.totalEdges} SourcePaths=${report.uniqueSourcePaths} Mirrors=${report.availableSourceMirrors}/${report.expectedSourceMirrors} Issues=${report.issueCount}\n`);
if (graphIssues.length > 0) process.exitCode = 1;
