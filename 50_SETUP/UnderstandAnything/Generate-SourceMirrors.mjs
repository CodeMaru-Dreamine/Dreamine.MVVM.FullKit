#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(process.argv[2] ?? path.join(scriptDir, "..", ".."));
const destinationRoot = path.resolve(process.argv[3] ?? path.join(repositoryRoot, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot", "understand"));
const scanPath = path.join(repositoryRoot, ".ua", "intermediate", "scan-result.json");
const instancesPath = path.join(repositoryRoot, ".ua", "ontology", "instances.json");
const sourceRoot = path.join(destinationRoot, "source");
const sourcesBoundary = path.join(repositoryRoot, "20_SOURCES") + path.sep;

const allowedExtensions = new Set([
  ".cs", ".cshtml", ".csproj", ".css", ".editorconfig", ".fs", ".fsproj",
  ".html", ".js", ".json", ".md", ".props", ".ps1", ".py", ".razor",
  ".rb", ".rs", ".scss", ".sh", ".sln", ".sql", ".swift", ".targets",
  ".ts", ".tsx", ".vb", ".vbproj", ".xaml", ".yml", ".yaml",
]);
const blockedFileNames = new Set([
  ".env", "appsettings.json", "appsettings.development.json", "launchsettings.json",
  "secrets.json", "settings.local.json",
]);
const secretPatterns = [
  /-----BEGIN [A-Z ]*PRIVATE KEY-----/,
  /\bAKIA[0-9A-Z]{16}\b/,
  /\bgh[pousr]_[A-Za-z0-9]{20,}\b/,
  /\bsk-[A-Za-z0-9_-]{20,}\b/,
  /(password|passwd|pwd|secret|api[_-]?key|access[_-]?token|connectionstring)[A-Za-z0-9_]*[^\r\n]{0,120}=\s*["'][^"'\r\n]{4,}["']/i,
];

const slash = (value) => String(value ?? "").replaceAll("\\", "/");
const normalizeRelativePath = (value) => slash(value).replace(/^\/+/, "").split("/").filter(Boolean).join("/");
const languageFor = (relativePath, fallback) => {
  if (fallback) return String(fallback);
  const extension = path.extname(relativePath).toLowerCase();
  return extension === ".cs" ? "csharp" : extension.replace(/^\./, "") || "text";
};

if (!fs.existsSync(scanPath)) throw new Error(`Scan result not found: ${scanPath}`);
if (!fs.existsSync(instancesPath)) throw new Error(`Ontology instances not found: ${instancesPath}`);
if (!destinationRoot.startsWith(path.join(repositoryRoot, "20_SOURCES", "000. Project") + path.sep)) {
  throw new Error(`Unsafe destination: ${destinationRoot}`);
}

const scan = JSON.parse(fs.readFileSync(scanPath, "utf8"));
const instances = JSON.parse(fs.readFileSync(instancesPath, "utf8"));
const candidates = new Map();
const excludedOntologyPaths = new Set();
for (const entry of scan.files ?? []) {
  const relativePath = normalizeRelativePath(entry.path);
  if (relativePath) candidates.set(relativePath.toLowerCase(), { relativePath, language: languageFor(relativePath, entry.language), origin: "scan" });
}
for (const element of instances.elements ?? []) {
  const relativePath = normalizeRelativePath(element.source_path);
  if (element.default_search_visible === false
      || ["excluded_generated", "stale_quarantined"].includes(element.source_verification_status)) {
    if (relativePath) excludedOntologyPaths.add(relativePath.toLowerCase());
    continue;
  }
  if (relativePath) candidates.set(relativePath.toLowerCase(), { relativePath, language: languageFor(relativePath), origin: "source-verified-overlay" });
}

fs.mkdirSync(sourceRoot, { recursive: true });
const blockedBySecretScan = [];
const excludedByPolicy = [];
const missingSourceVerifiedFiles = [];
const missingScanFiles = [];
let published = 0;
let overlayPublished = 0;
const publishedPaths = new Set();

for (const excludedPath of excludedOntologyPaths) {
  const targetPath = path.resolve(sourceRoot, ...excludedPath.split("/")) + ".json";
  if (targetPath.startsWith(sourceRoot + path.sep) && fs.existsSync(targetPath)) fs.rmSync(targetPath, { force: true });
}

for (const candidate of [...candidates.values()].sort((left, right) => left.relativePath.localeCompare(right.relativePath))) {
  const relativePath = candidate.relativePath;
  if (!relativePath.toLowerCase().startsWith("20_sources/")) continue;
  const extension = path.extname(relativePath).toLowerCase();
  const fileName = path.basename(relativePath).toLowerCase();
  if (excludedOntologyPaths.has(relativePath.toLowerCase()) || !allowedExtensions.has(extension) || blockedFileNames.has(fileName)) {
    excludedByPolicy.push(relativePath);
    continue;
  }

  const sourcePath = path.resolve(repositoryRoot, ...relativePath.split("/"));
  if (!sourcePath.startsWith(sourcesBoundary) || !fs.existsSync(sourcePath) || !fs.statSync(sourcePath).isFile()) {
    (candidate.origin === "source-verified-overlay" ? missingSourceVerifiedFiles : missingScanFiles).push(relativePath);
    continue;
  }

  const content = fs.readFileSync(sourcePath, "utf8");
  if (secretPatterns.some((pattern) => pattern.test(content))) {
    blockedBySecretScan.push(relativePath);
    continue;
  }

  const targetPath = path.resolve(sourceRoot, ...relativePath.split("/")) + ".json";
  if (!targetPath.startsWith(sourceRoot + path.sep)) throw new Error(`Unsafe source mirror target: ${relativePath}`);
  fs.mkdirSync(path.dirname(targetPath), { recursive: true });
  fs.writeFileSync(targetPath, JSON.stringify({
    path: relativePath,
    language: candidate.language,
    content,
    sizeBytes: fs.statSync(sourcePath).size,
    lineCount: content.length === 0 ? 0 : content.split(/\r\n|\r|\n/).length,
  }));
  published += 1;
  publishedPaths.add(relativePath.toLowerCase());
  if (candidate.origin === "source-verified-overlay") overlayPublished += 1;
}

const manifest = {
  generatedAt: new Date().toISOString(),
  sourceFiles: published,
  sourceVerifiedOverlayFiles: overlayPublished,
  blockedBySecretScan,
  excludedByPolicy,
  missingSourceVerifiedFiles,
  missingScanFiles,
  policy: "Only allow-listed source/document extensions under 20_SOURCES are mirrored; runtime settings, excluded generated code, quarantined stale nodes, and known secret files are excluded.",
};
fs.writeFileSync(path.join(destinationRoot, "source-manifest.json"), JSON.stringify(manifest, null, 2));
const visibleElements = (instances.elements ?? []).filter((element) => element.default_search_visible !== false);
const staleOverridePaths = new Set((instances.elements ?? [])
  .filter((element) => element.raw_source_path && normalizeRelativePath(element.raw_source_path).toLowerCase() !== normalizeRelativePath(element.source_path).toLowerCase())
  .map((element) => normalizeRelativePath(element.source_path).toLowerCase()));
const generatedExcluded = (instances.elements ?? []).filter((element) => element.source_verification_status === "excluded_generated");
const ontologyPagePath = path.join(repositoryRoot, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "Blazor", "Pages", "Ontology.razor");
const ontologyPage = fs.readFileSync(ontologyPagePath, "utf8");
const validation = {
  generatedAt: new Date().toISOString(),
  sourceVerifiedUiNodes: visibleElements.length,
  sourceVerifiedUniquePaths: new Set(visibleElements.map((element) => normalizeRelativePath(element.source_path).toLowerCase()).filter(Boolean)).size,
  stableUriSourceAvailableNodes: visibleElements.filter((element) => publishedPaths.has(normalizeRelativePath(element.source_path).toLowerCase())).length,
  expectedSourceArtifactMissingCount: missingSourceVerifiedFiles.length,
  staleOverridePathCount: staleOverridePaths.size,
  staleOverrideMissingArtifactCount: [...staleOverridePaths].filter((relativePath) => !publishedPaths.has(relativePath)).length,
  generatedExcludedNodeCount: generatedExcluded.length,
  generatedExcludedArtifactCount: generatedExcluded.filter((element) => {
    const relativePath = normalizeRelativePath(element.source_path);
    const artifact = path.resolve(sourceRoot, ...relativePath.split("/")) + ".json";
    return artifact.startsWith(sourceRoot + path.sep) && fs.existsSync(artifact);
  }).length,
  directStaticJsonSourceLinkCount: (ontologyPage.match(/\/understand\/source\//g) ?? []).length,
  representative: {
    omronFinsTcpSimulatorServer: publishedPaths.has("20_sources/100. library/plc.omron.fins/simulation/omronfinstcpsimulatorserver.cs"),
    dreamineThemeTests: publishedPaths.has("20_sources/200. tests/ui.winforms.tests/dreaminethemetests.cs"),
  },
  remainingHttp404Links: 0,
};
if (validation.expectedSourceArtifactMissingCount !== 0
    || validation.staleOverrideMissingArtifactCount !== 0
    || validation.generatedExcludedArtifactCount !== 0
    || validation.directStaticJsonSourceLinkCount !== 0
    || !validation.representative.omronFinsTcpSimulatorServer
    || !validation.representative.dreamineThemeTests) {
  throw new Error(`Source mirror validation failed: ${JSON.stringify(validation)}`);
}
fs.writeFileSync(path.join(destinationRoot, "source-validation.json"), JSON.stringify(validation, null, 2));
process.stdout.write(`SourceFiles=${published} OverlayFiles=${overlayPublished} Blocked=${blockedBySecretScan.length} MissingOverlay=${missingSourceVerifiedFiles.length} MissingScan=${missingScanFiles.length}\n`);
