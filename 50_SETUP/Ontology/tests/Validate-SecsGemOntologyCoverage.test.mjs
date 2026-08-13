import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import {
  fullKitPackageId,
  sourcePackageIds,
  validateGeneratedOntology,
  validateManifest,
  validatePreflight,
} from "../Validate-SecsGemOntologyCoverage.mjs";

const testDirectory = path.dirname(fileURLToPath(import.meta.url));
const ontologyDirectory = path.dirname(testDirectory);
const manifest = JSON.parse(fs.readFileSync(path.join(ontologyDirectory, "secsgem-packages.json"), "utf8"));
const clone = (value) => JSON.parse(JSON.stringify(value));

test("manifest fixes the six source packages and FullKit dependency closure", () => {
  assert.deepEqual(validateManifest(manifest), []);

  const invalid = clone(manifest);
  invalid.packages.find((item) => item.packageId === fullKitPackageId).dependencies.pop();
  assert.ok(validateManifest(invalid).some((error) => error.includes("dependency closure")));
});

test("preflight requires owned source nodes in both language graphs and a SECS/GEM domain", (context) => {
  const repositoryRoot = fs.mkdtempSync(path.join(os.tmpdir(), "dreamine-secsgem-ontology-"));
  context.after(() => fs.rmSync(repositoryRoot, { recursive: true, force: true }));

  const graph = { nodes: [] };
  for (const packageInfo of manifest.packages.filter((item) => !item.synthetic)) {
    const projectPath = path.join(repositoryRoot, ...packageInfo.projectFile.split("/"));
    fs.mkdirSync(path.dirname(projectPath), { recursive: true });
    fs.writeFileSync(projectPath, `<Project><PropertyGroup><TargetFramework>${packageInfo.targetFrameworks[0]}</TargetFramework><PackageId>${packageInfo.packageId}</PackageId><Version>${packageInfo.version}</Version></PropertyGroup></Project>`);
    const sourcePath = path.join(path.dirname(projectPath), "Sample.cs");
    fs.writeFileSync(sourcePath, "public sealed class Sample {}\n");
    graph.nodes.push({
      id: `file:${packageInfo.packageId}`,
      type: "file",
      filePath: `${path.posix.dirname(packageInfo.projectFile)}/Sample.cs`,
      fileMeta: { project: { packageId: packageInfo.packageId, projectFile: packageInfo.projectFile } },
    });
  }
  const domainGraph = { nodes: [{ name: "SECS/GEM equipment communication", summary: "HSMS and GEM300 workflows", tags: ["secs"] }] };
  assert.deepEqual(validatePreflight({ repositoryRoot, manifest, koGraph: graph, enGraph: clone(graph), domainGraph }), []);

  const staleEnglishGraph = clone(graph);
  staleEnglishGraph.nodes = staleEnglishGraph.nodes.filter((node) => node.fileMeta.project.packageId !== sourcePackageIds[0]);
  const errors = validatePreflight({ repositoryRoot, manifest, koGraph: graph, enGraph: staleEnglishGraph, domainGraph });
  assert.ok(errors.some((error) => error.includes(`${sourcePackageIds[0]}: English graph`)));
  assert.ok(validatePreflight({ repositoryRoot, manifest, koGraph: graph, enGraph: graph, domainGraph: { nodes: [] } })
    .some((error) => error.includes("Domain graph")));
});

test("generated ontology requires seven Project nodes, package-owned code, and all declared dependencies", () => {
  const elements = [];
  const projectById = new Map();
  for (const packageInfo of manifest.packages) {
    const project = {
      stable_id: `project:${packageInfo.packageId}`,
      source_graph_id: `synthetic:project:${packageInfo.packageId}`,
      element_type: "Project",
      package_id: packageInfo.packageId,
    };
    elements.push(project);
    projectById.set(packageInfo.packageId, project);
    if (!packageInfo.synthetic) {
      elements.push({ stable_id: `code:${packageInfo.packageId}`, element_type: "CodeClass", package_id: packageInfo.packageId });
    }
  }
  const relations = manifest.packages.flatMap((packageInfo) => packageInfo.dependencies.map((dependency) => ({
    relation_type: "depends_on",
    source: projectById.get(packageInfo.packageId).stable_id,
    target: projectById.get(dependency).stable_id,
  })));
  assert.deepEqual(validateGeneratedOntology({ manifest, instances: { elements, relations } }), []);

  const missingRelation = relations.filter((relation) => !(relation.source === `project:${fullKitPackageId}`
    && relation.target === `project:${sourcePackageIds[0]}`));
  assert.ok(validateGeneratedOntology({ manifest, instances: { elements, relations: missingRelation } })
    .some((error) => error.includes(`${fullKitPackageId}: missing depends_on relation`)));
});
