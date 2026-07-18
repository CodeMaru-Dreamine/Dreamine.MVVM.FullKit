#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";

const repositoryRoot = path.resolve(process.argv[2] ?? ".");
const destinationRoot = path.resolve(process.argv[3] ?? path.join(repositoryRoot, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot", "understand"));
const catalogPath = path.join(destinationRoot, "project-catalog.json");
const excludedSegment = /(?:^|[\\/])(?:bin|obj|\.git|\.vs|node_modules|TestResults)(?:[\\/]|$)/i;

if (!fs.existsSync(catalogPath)) throw new Error(`Catalog not found: ${catalogPath}`);
const catalog = JSON.parse(fs.readFileSync(catalogPath, "utf8"));
if (catalog.totalProjects !== 80 || catalog.projects?.length !== 80) {
  throw new Error(`Expected 80 catalog projects, found ${catalog.projects?.length ?? 0}.`);
}

let graphCount = 0;
let nodeCount = 0;
let edgeCount = 0;
for (const project of catalog.projects) {
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
    const nodeIds = new Set(graph.nodes.map((node) => node.id));
    if (nodeIds.size !== graph.nodes.length) throw new Error(`${project.name}/${language}: duplicate node IDs.`);
    if (graph.nodes.some((node) => excludedSegment.test(node.filePath ?? ""))) {
      throw new Error(`${project.name}/${language}: excluded path found in graph.`);
    }
    for (const edge of graph.edges) {
      if (!nodeIds.has(edge.source) || !nodeIds.has(edge.target)) {
        throw new Error(`${project.name}/${language}: dangling edge ${edge.source} -> ${edge.target}.`);
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
  if (!project.doxygenUrls?.ko?.includes("/KR/") || !project.doxygenUrls?.en?.includes("/EN/")) {
    throw new Error(`${project.name}: localized Doxygen URLs are invalid.`);
  }
  if (!project.graphUrls?.ko?.endsWith("lang=ko") || !project.graphUrls?.en?.endsWith("lang=en")) {
    throw new Error(`${project.name}: localized graph URLs are invalid.`);
  }
}

if (graphCount !== 160) throw new Error(`Expected 160 graphs, found ${graphCount}.`);
for (const language of ["ko", "en"]) {
  const fullGraphPath = path.join(destinationRoot, "full", language, "knowledge-graph.json");
  if (!fs.existsSync(fullGraphPath)) throw new Error(`Full ${language} graph not found: ${fullGraphPath}`);
  const fullGraph = JSON.parse(fs.readFileSync(fullGraphPath, "utf8"));
  if (fullGraph.project?.outputLanguage !== language) throw new Error(`Full ${language} graph language does not match.`);
}
process.stdout.write(`Validated projects=80 graphs=${graphCount} nodes=${nodeCount} edges=${edgeCount}\n`);
