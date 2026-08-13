#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const defaultManifestPath = path.join(scriptDirectory, "secsgem-packages.json");

export const sourcePackageIds = Object.freeze([
  "Dreamine.Secs.Abstractions",
  "Dreamine.Secs.Com",
  "Dreamine.Gem.Abstractions",
  "Dreamine.Gem",
  "Dreamine.Gem300.Abstractions",
  "Dreamine.Gem300",
]);
export const fullKitPackageId = "Dreamine.SecsGem.FullKit";
export const requiredPackageIds = Object.freeze([...sourcePackageIds, fullKitPackageId]);

const slash = (value) => String(value ?? "").replaceAll("\\", "/");
const compact = (value) => String(value ?? "").replace(/\s+/g, " ").trim();
const readJson = (filePath) => JSON.parse(fs.readFileSync(filePath, "utf8"));
const readTag = (xml, name) => compact(xml.match(new RegExp(`<${name}(?:\\s[^>]*)?>([\\s\\S]*?)<\\/${name}>`, "i"))?.[1]);

function isPathInside(root, candidate) {
  const relative = path.relative(root, candidate);
  return relative !== "" && !relative.startsWith(`..${path.sep}`) && relative !== ".." && !path.isAbsolute(relative);
}

function enumerateCSharpFiles(directory) {
  const result = [];
  const pending = [directory];
  while (pending.length > 0) {
    const current = pending.pop();
    for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
      if (["bin", "obj", ".git", ".vs", "node_modules", "TestResults"].includes(entry.name)) continue;
      const fullPath = path.join(current, entry.name);
      if (entry.isDirectory()) pending.push(fullPath);
      else if (entry.isFile() && entry.name.toLowerCase().endsWith(".cs")) result.push(fullPath);
    }
  }
  return result;
}

function projectMetadata(node) {
  return node?.apiMeta?.project ?? node?.fileMeta?.project ?? {};
}

function graphOwnsSourceProject(graph, packageInfo) {
  const projectFile = slash(packageInfo.projectFile).toLowerCase();
  const projectDirectory = `${slash(path.posix.dirname(slash(packageInfo.projectFile))).toLowerCase()}/`;
  return (graph?.nodes ?? []).some((node) => {
    const metadata = projectMetadata(node);
    const metadataProjectFile = slash(metadata.projectFile).toLowerCase();
    const filePath = slash(node?.filePath).toLowerCase();
    return filePath.endsWith(".cs") && (metadata.packageId === packageInfo.packageId
      || metadataProjectFile === projectFile
      || filePath.startsWith(projectDirectory));
  });
}

export function validateManifest(manifest) {
  const errors = [];
  if (manifest?.schemaVersion !== 1) errors.push("secsgem manifest schemaVersion must be 1.");
  if (!Array.isArray(manifest?.packages)) return [...errors, "secsgem manifest packages must be an array."];

  const packages = manifest.packages;
  const ids = packages.map((item) => item?.packageId).filter(Boolean);
  const duplicateIds = ids.filter((id, index) => ids.indexOf(id) !== index);
  if (duplicateIds.length > 0) errors.push(`Duplicate package IDs: ${[...new Set(duplicateIds)].join(", ")}.`);
  for (const requiredId of requiredPackageIds) {
    if (!ids.includes(requiredId)) errors.push(`Required package is missing: ${requiredId}.`);
  }
  const unexpectedIds = ids.filter((id) => !requiredPackageIds.includes(id));
  if (unexpectedIds.length > 0) errors.push(`Unexpected package IDs: ${unexpectedIds.join(", ")}.`);

  const byId = new Map(packages.map((item) => [item.packageId, item]));
  for (const packageInfo of packages) {
    if (!packageInfo?.packageId) continue;
    if (!/^\d+\.\d+\.\d+(?:[-+].+)?$/.test(packageInfo.version ?? "")) {
      errors.push(`${packageInfo.packageId}: version is missing or invalid.`);
    }
    if (!Array.isArray(packageInfo.targetFrameworks) || packageInfo.targetFrameworks.length === 0) {
      errors.push(`${packageInfo.packageId}: targetFrameworks must not be empty.`);
    }
    if (!compact(packageInfo.summaryKo) || !compact(packageInfo.summaryEn)) {
      errors.push(`${packageInfo.packageId}: both Korean and English summaries are required.`);
    }
    if (!Array.isArray(packageInfo.dependencies)) {
      errors.push(`${packageInfo.packageId}: dependencies must be an array.`);
      continue;
    }
    for (const dependency of packageInfo.dependencies) {
      if (!byId.has(dependency)) errors.push(`${packageInfo.packageId}: unknown dependency ${dependency}.`);
      if (dependency === packageInfo.packageId) errors.push(`${packageInfo.packageId}: self-dependency is not allowed.`);
    }
  }

  for (const packageId of sourcePackageIds) {
    const packageInfo = byId.get(packageId);
    if (!packageInfo) continue;
    if (packageInfo.synthetic !== false) errors.push(`${packageId}: source package must not be synthetic.`);
    if (!compact(packageInfo.projectFile)) errors.push(`${packageId}: projectFile is required.`);
  }
  const fullKit = byId.get(fullKitPackageId);
  if (fullKit) {
    if (fullKit.synthetic !== true) errors.push(`${fullKitPackageId}: meta package must be synthetic.`);
    if (fullKit.projectFile !== null) errors.push(`${fullKitPackageId}: projectFile must be null until it is moved into the repository.`);
    const expectedDependencies = [...sourcePackageIds].sort();
    const actualDependencies = [...(fullKit.dependencies ?? [])].sort();
    if (JSON.stringify(actualDependencies) !== JSON.stringify(expectedDependencies)) {
      errors.push(`${fullKitPackageId}: dependency closure must contain all six source packages exactly once.`);
    }
  }
  if (!Array.isArray(manifest.domainTerms) || !manifest.domainTerms.some((term) => /^(?:SECS|HSMS|GEM300)$/i.test(compact(term)))) {
    errors.push("secsgem manifest must define at least one unambiguous SECS/GEM domain term.");
  }
  return errors;
}

export function validatePreflight({ repositoryRoot, manifest, koGraph, enGraph, domainGraph }) {
  const errors = validateManifest(manifest);
  const root = path.resolve(repositoryRoot);
  for (const packageInfo of manifest.packages ?? []) {
    if (packageInfo.synthetic) continue;
    const projectPath = path.resolve(root, ...slash(packageInfo.projectFile).split("/"));
    if (!isPathInside(root, projectPath)) {
      errors.push(`${packageInfo.packageId}: projectFile escapes the repository root.`);
      continue;
    }
    if (!fs.existsSync(projectPath) || !fs.statSync(projectPath).isFile()) {
      errors.push(`${packageInfo.packageId}: project file not found: ${packageInfo.projectFile}.`);
      continue;
    }
    const xml = fs.readFileSync(projectPath, "utf8");
    const actualPackageId = readTag(xml, "PackageId") || path.basename(projectPath, path.extname(projectPath));
    const actualVersion = readTag(xml, "Version") || readTag(xml, "VersionPrefix") || readTag(xml, "PackageVersion");
    const actualFrameworks = (readTag(xml, "TargetFrameworks") || readTag(xml, "TargetFramework")).split(";").filter(Boolean);
    if (actualPackageId !== packageInfo.packageId) errors.push(`${packageInfo.packageId}: project PackageId is ${actualPackageId}.`);
    if (actualVersion !== packageInfo.version) errors.push(`${packageInfo.packageId}: project version is ${actualVersion || "<missing>"}.`);
    for (const framework of packageInfo.targetFrameworks ?? []) {
      if (!actualFrameworks.includes(framework)) errors.push(`${packageInfo.packageId}: project does not target ${framework}.`);
    }
    if (enumerateCSharpFiles(path.dirname(projectPath)).length === 0) {
      errors.push(`${packageInfo.packageId}: no C# source files were found.`);
    }
    if (!graphOwnsSourceProject(koGraph, packageInfo)) errors.push(`${packageInfo.packageId}: Korean graph has no owned C# source node; run a full graph rebuild.`);
    if (!graphOwnsSourceProject(enGraph, packageInfo)) errors.push(`${packageInfo.packageId}: English graph has no owned C# source node; run a full graph rebuild.`);
  }

  const domainText = (domainGraph?.nodes ?? []).map((node) => [node.name, node.summary, ...(node.tags ?? [])].join(" ")).join("\n");
  const hasDomainCoverage = (manifest.domainTerms ?? []).some((term) => new RegExp(`\\b${String(term).replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}\\b`, "i").test(domainText));
  if (!hasDomainCoverage) errors.push("Domain graph has no SECS/HSMS/GEM300 concept; regenerate it from the full graph.");
  return errors;
}

export function validateGeneratedOntology({ manifest, instances }) {
  const errors = validateManifest(manifest);
  const elements = Array.isArray(instances?.elements) ? instances.elements : [];
  const relations = Array.isArray(instances?.relations) ? instances.relations : [];
  if (!Array.isArray(instances?.elements)) errors.push("Ontology instances.elements must be an array.");
  if (!Array.isArray(instances?.relations)) errors.push("Ontology instances.relations must be an array.");

  const projectByPackage = new Map();
  for (const packageId of requiredPackageIds) {
    const matches = elements.filter((element) => element.element_type === "Project" && element.package_id === packageId);
    if (matches.length !== 1) errors.push(`${packageId}: expected exactly one Project ontology node, found ${matches.length}.`);
    else projectByPackage.set(packageId, matches[0]);
  }
  for (const packageId of sourcePackageIds) {
    const codeElements = elements.filter((element) => element.package_id === packageId && element.element_type !== "Project");
    if (codeElements.length === 0) errors.push(`${packageId}: ontology has no package-owned source elements.`);
  }
  const fullKit = projectByPackage.get(fullKitPackageId);
  if (fullKit && fullKit.source_graph_id !== `synthetic:project:${fullKitPackageId}`) {
    errors.push(`${fullKitPackageId}: synthetic source_graph_id is invalid.`);
  }

  for (const packageInfo of manifest.packages ?? []) {
    const source = projectByPackage.get(packageInfo.packageId)?.stable_id;
    for (const dependency of packageInfo.dependencies ?? []) {
      const target = projectByPackage.get(dependency)?.stable_id;
      const match = relations.some((relation) => relation.relation_type === "depends_on"
        && relation.source === source && relation.target === target);
      if (!match) errors.push(`${packageInfo.packageId}: missing depends_on relation to ${dependency}.`);
    }
  }
  return errors;
}

function reportAndExit(phase, errors) {
  if (errors.length === 0) {
    process.stdout.write(`SECS/GEM ontology ${phase} validation passed: packages=${requiredPackageIds.length}.\n`);
    return;
  }
  process.stderr.write(`SECS/GEM ontology ${phase} validation failed (${errors.length}):\n`);
  for (const error of errors) process.stderr.write(`- ${error}\n`);
  process.exitCode = 1;
}

async function main() {
  const repositoryRoot = path.resolve(process.argv[2] ?? path.join(scriptDirectory, "..", ".."));
  const phase = String(process.argv[3] ?? "preflight").toLowerCase();
  const manifest = readJson(defaultManifestPath);
  if (phase === "manifest") {
    reportAndExit(phase, validateManifest(manifest));
    return;
  }
  if (phase === "preflight") {
    reportAndExit(phase, validatePreflight({
      repositoryRoot,
      manifest,
      koGraph: readJson(path.join(repositoryRoot, ".ua", "knowledge-graph.ko.json")),
      enGraph: readJson(path.join(repositoryRoot, ".ua", "knowledge-graph.en.json")),
      domainGraph: readJson(path.join(repositoryRoot, ".ua", "domain-graph.json")),
    }));
    return;
  }
  if (phase === "generated") {
    reportAndExit(phase, validateGeneratedOntology({
      manifest,
      instances: readJson(path.join(repositoryRoot, ".ua", "ontology", "instances.json")),
    }));
    return;
  }
  throw new Error(`Unknown validation phase '${phase}'. Expected manifest, preflight, or generated.`);
}

const isDirectExecution = process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isDirectExecution) {
  main().catch((error) => {
    process.stderr.write(`${error instanceof Error ? error.stack : String(error)}\n`);
    process.exitCode = 1;
  });
}
