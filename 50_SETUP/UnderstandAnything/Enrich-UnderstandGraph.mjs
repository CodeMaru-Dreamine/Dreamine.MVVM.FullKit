#!/usr/bin/env node

import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(process.argv[2] ?? path.join(scriptDir, "..", ".."));
const outputLanguage = String(process.argv[3] ?? "ko").toLowerCase() === "en" ? "en" : "ko";
const uaDir = path.join(repositoryRoot, ".ua");
const graphPath = path.resolve(process.argv[4] ?? path.join(uaDir, "knowledge-graph.json"));
const outputGraphPath = path.resolve(process.argv[5] ?? graphPath);
const fingerprintPath = path.join(uaDir, "fingerprints.json");
const scanPath = path.join(uaDir, "intermediate", "scan-result.json");
const backupPath = path.join(uaDir, "knowledge-graph.pre-enrichment.json");

for (const required of [graphPath, fingerprintPath, scanPath]) {
  if (!fs.existsSync(required)) throw new Error(`Required input not found: ${required}`);
}

const graph = JSON.parse(fs.readFileSync(graphPath, "utf8"));
const fingerprints = JSON.parse(fs.readFileSync(fingerprintPath, "utf8"));
const scan = JSON.parse(fs.readFileSync(scanPath, "utf8"));

if (!graph.project?.enrichmentVersion && !fs.existsSync(backupPath)) {
  fs.copyFileSync(graphPath, backupPath);
}

const slash = (value) => String(value ?? "").replaceAll("\\", "/");
const absolute = (relative) => path.resolve(repositoryRoot, slash(relative));
const compact = (value) => String(value ?? "").replace(/\s+/g, " ").trim();
const selectLanguageSection = (value) => {
  const source = String(value ?? "");
  const sections = [...source.matchAll(/\\if\s+(KO|EN)\s*([\s\S]*?)\\endif/gi)];
  if (!sections.length) return source;
  const wanted = outputLanguage === "en" ? "EN" : "KO";
  return sections.filter((match) => match[1].toUpperCase() === wanted).map((match) => match[2]).join(" ");
};
const decodeXml = (value) => compact(selectLanguageSection(value)
  .replace(/<see\s+cref=["'](?:[A-Z]:)?([^"']+)["']\s*\/>/gi, "$1")
  .replace(/<paramref\s+name=["']([^"']+)["']\s*\/>/gi, "$1")
  .replace(/<c>([\s\S]*?)<\/c>/gi, "$1")
  .replace(/<[^>]+>/g, " ")
  .replaceAll("&lt;", "<").replaceAll("&gt;", ">").replaceAll("&amp;", "&")
  .replaceAll("&quot;", '"').replaceAll("&apos;", "'"));
const hash = (value) => crypto.createHash("sha256").update(value).digest("hex").slice(0, 10);
const slugify = (value) => compact(value).normalize("NFKD").replace(/[^\p{L}\p{N}]+/gu, "-").replace(/^-|-$/g, "").toLowerCase() || "api";
const unique = (values) => [...new Set(values.filter(Boolean))];

const sourceCache = new Map();
function readSource(filePath) {
  if (sourceCache.has(filePath)) return sourceCache.get(filePath);
  const fullPath = absolute(filePath);
  if (!fullPath.startsWith(repositoryRoot, 0) || !fs.existsSync(fullPath)) return null;
  const text = fs.readFileSync(fullPath, "utf8").replace(/^\uFEFF/, "");
  const value = { text, lines: text.split(/\r?\n/) };
  sourceCache.set(filePath, value);
  return value;
}

function getDocBlock(lines, startLine) {
  let index = Math.max(0, startLine - 2);
  while (index >= 0 && /^\s*\[/.test(lines[index])) index--;
  const docLines = [];
  while (index >= 0 && /^\s*\/\/\//.test(lines[index])) {
    docLines.unshift(lines[index].replace(/^\s*\/\/\/?\s?/, ""));
    index--;
  }
  const raw = docLines.join("\n");
  const one = (tag) => {
    const match = raw.match(new RegExp(`<${tag}(?:\\s[^>]*)?>([\\s\\S]*?)<\\/${tag}>`, "i"));
    return match ? decodeXml(match[1]) : "";
  };
  const parameters = {};
  for (const match of raw.matchAll(/<param\s+name=["']([^"']+)["']\s*>([\s\S]*?)<\/param>/gi)) {
    parameters[match[1]] = decodeXml(match[2]);
  }
  const exceptions = [];
  for (const match of raw.matchAll(/<exception\s+cref=["']([^"']+)["']\s*>([\s\S]*?)<\/exception>/gi)) {
    exceptions.push({ type: match[1].replace(/^[A-Z]:/, ""), description: decodeXml(match[2]), source: "xml" });
  }
  const typeParameters = {};
  for (const match of raw.matchAll(/<typeparam\s+name=["']([^"']+)["']\s*>([\s\S]*?)<\/typeparam>/gi)) {
    typeParameters[match[1]] = decodeXml(match[2]);
  }
  return {
    raw,
    summary: one("summary"),
    remarks: one("remarks"),
    returns: one("returns"),
    example: one("example"),
    parameters,
    typeParameters,
    exceptions,
  };
}

function declarationText(lines, startLine, endLine, name) {
  const pieces = [];
  let parenDepth = 0;
  let seenParen = false;
  for (let i = Math.max(0, startLine - 1); i < Math.min(lines.length, endLine, startLine + 15); i++) {
    const line = lines[i].replace(/\/\/.*$/, "").trim();
    if (!line || line.startsWith("[")) continue;
    pieces.push(line);
    for (const char of line) {
      if (char === "(") { parenDepth++; seenParen = true; }
      else if (char === ")") parenDepth--;
    }
    if ((seenParen && parenDepth <= 0 && /[;{]|=>/.test(line)) || (!seenParen && /[;{]/.test(line))) break;
  }
  let declaration = compact(pieces.join(" "));
  const arrow = declaration.indexOf("=>");
  if (arrow >= 0) declaration = declaration.slice(0, arrow).trim();
  if (seenParen) {
    const close = declaration.lastIndexOf(")");
    if (close >= 0) declaration = declaration.slice(0, close + 1) + declaration.slice(close + 1).replace(/\s*\{.*$/, "").replace(/\s*;.*$/, "");
  } else {
    declaration = declaration.replace(/\s*\{.*$/, "").replace(/\s*;.*$/, "");
  }
  return declaration || name;
}

function splitTopLevel(value) {
  const parts = [];
  let current = "", angle = 0, round = 0, square = 0;
  for (const char of value) {
    if (char === "<") angle++;
    else if (char === ">") angle = Math.max(0, angle - 1);
    else if (char === "(") round++;
    else if (char === ")") round = Math.max(0, round - 1);
    else if (char === "[") square++;
    else if (char === "]") square = Math.max(0, square - 1);
    if (char === "," && angle === 0 && round === 0 && square === 0) { parts.push(current.trim()); current = ""; }
    else current += char;
  }
  if (current.trim()) parts.push(current.trim());
  return parts;
}

const modifierSet = new Set(["public", "protected", "internal", "private", "static", "virtual", "override", "abstract", "sealed", "async", "extern", "unsafe", "new", "partial", "readonly"]);
function parseMethodDeclaration(signature, methodName, containingClass, doc, fingerprint) {
  const namePattern = methodName.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const methodMatch = signature.match(new RegExp(`\\b${namePattern}(?:<[^>]+>)?\\s*\\(`));
  const open = methodMatch ? signature.indexOf("(", methodMatch.index) : signature.indexOf("(");
  const close = signature.lastIndexOf(")");
  const rawParams = open >= 0 && close > open ? splitTopLevel(signature.slice(open + 1, close)) : [];
  const parameters = rawParams.map((raw, index) => {
    const cleaned = raw.replace(/^\s*(?:\[[^\]]+\]\s*)+/, "");
    const [left, ...defaultParts] = splitTopLevel(cleaned.replace(/=/, ",__DEFAULT__")).join(",").split(",__DEFAULT__");
    const tokens = compact(left).split(" ");
    const name = (tokens.pop() ?? fingerprint?.params?.[index] ?? `arg${index + 1}`).replace(/^@/, "");
    const modifiers = tokens.filter((token) => ["ref", "out", "in", "params", "this", "scoped"].includes(token));
    const type = tokens.filter((token) => !modifiers.includes(token)).join(" ") || "unknown";
    return { name, type, modifiers, defaultValue: defaultParts.length ? defaultParts.join("=").trim() : undefined, description: doc.parameters[name] || "", descriptionSource: doc.parameters[name] ? "xml" : "undocumented" };
  });
  const prefix = methodMatch ? signature.slice(0, methodMatch.index).trim() : "";
  const prefixTokens = prefix.split(/\s+/).filter(Boolean);
  const accessibility = ["public", "protected", "internal", "private"].find((item) => prefixTokens.includes(item)) ?? "private";
  const modifiers = prefixTokens.filter((item) => modifierSet.has(item) && item !== accessibility);
  const meaningful = prefixTokens.filter((item) => !modifierSet.has(item) && !item.startsWith("["));
  const constructor = methodName === containingClass;
  const returnType = constructor ? null : (meaningful.at(-1) ?? fingerprint?.returnType ?? "unknown");
  return { accessibility, modifiers, constructor, parameters, returnType };
}

function codeExceptions(lines, startLine, endLine) {
  const result = [];
  for (let i = Math.max(0, startLine - 1); i < Math.min(lines.length, endLine); i++) {
    const line = lines[i];
    for (const match of line.matchAll(/throw\s+new\s+([\w.]+)(?:<[^>]+>)?\s*\(/g)) {
      const previous = i > 0 ? lines[i - 1].trim() : "";
      const conditionMatch = `${previous} ${line}`.match(/if\s*\(([^)]+)\)/);
      result.push({ type: match[1], description: outputLanguage === "en" ? "Thrown directly by the implementation." : "코드에서 직접 발생시키는 예외입니다.", condition: conditionMatch ? compact(conditionMatch[1]) : "", source: "code" });
    }
  }
  return result;
}

function detectSideEffects(body) {
  const rules = [
    [["파일 시스템 변경", "File system changes"], /\b(File|Directory|FileStream)\.(Write|Create|Delete|Move|Copy|Append)|new\s+FileStream\b/],
    [["데이터 저장소 읽기/쓰기", "Data store read/write"], /\b(SaveChanges|ExecuteNonQuery|ExecuteAsync|QueryAsync|InsertAsync|UpdateAsync|DeleteAsync)\b/],
    [["네트워크 통신", "Network communication"], /\b(HttpClient|SendAsync|ConnectAsync|PublishAsync|Socket|TcpClient|RabbitMQ)\b/],
    [["이벤트 또는 메시지 발행", "Event or message publication"], /\b(Invoke|Publish|RaisePropertyChanged|OnPropertyChanged)\s*\(/],
    [["UI 상태 변경", "UI state changes"], /\b(Dispatcher|StateHasChanged|DataContext|Visibility)\b/],
    [["로깅", "Logging"], /\b(Log|Logger)\.(Trace|Debug|Information|Warning|Error|Critical)|Log[A-Z]\w*\s*\(/],
  ];
  return rules.filter(([, pattern]) => pattern.test(body)).map(([labels]) => labels[outputLanguage === "en" ? 1 : 0]);
}

const projectFiles = scan.files.map((entry) => slash(entry.path));
const csprojPaths = projectFiles.filter((file) => file.toLowerCase().endsWith(".csproj"));
const projectInfoCache = new Map();
function projectInfo(filePath) {
  const normalized = slash(filePath);
  const candidates = csprojPaths.filter((candidate) => normalized.startsWith(`${path.posix.dirname(candidate)}/`)).sort((a, b) => b.length - a.length);
  const csproj = candidates[0];
  if (!csproj) return null;
  if (projectInfoCache.has(csproj)) return projectInfoCache.get(csproj);
  const text = readSource(csproj)?.text ?? "";
  const tag = (name) => compact(text.match(new RegExp(`<${name}>([\\s\\S]*?)<\\/${name}>`, "i"))?.[1]);
  const frameworks = unique((tag("TargetFrameworks") || tag("TargetFramework")).split(";").map(compact));
  const info = {
    project: path.posix.basename(csproj, ".csproj"),
    projectFile: csproj,
    packageId: tag("PackageId") || path.posix.basename(csproj, ".csproj"),
    version: tag("Version") || tag("PackageVersion") || "",
    targetFrameworks: frameworks,
    windowsOnly: frameworks.some((item) => /-windows/i.test(item)),
  };
  projectInfoCache.set(csproj, info);
  return info;
}

function identifierIndex(paths) {
  const index = new Map();
  for (const filePath of paths) {
    const source = readSource(filePath);
    if (!source) continue;
    const identifiers = unique(source.text.match(/\b[A-Za-z_][A-Za-z0-9_]{3,}\b/g) ?? []);
    for (const identifier of identifiers) {
      const list = index.get(identifier) ?? [];
      if (list.length < 12) list.push(filePath);
      index.set(identifier, list);
    }
  }
  return index;
}

const samplePaths = projectFiles.filter((file) => /(^|\/)998\. DEMO\//i.test(file) && /\.(cs|razor|xaml|md)$/i.test(file));
const testPaths = projectFiles.filter((file) => /(^|\/)(200\. Tests|[^/]*Tests?)(\/|\.)/i.test(file) && /\.(cs|razor|xaml)$/i.test(file));
const sampleIndex = identifierIndex(samplePaths);
const testIndex = identifierIndex(testPaths);

const nodesById = new Map(graph.nodes.map((node) => [node.id, node]));
const fileNodesByPath = new Map(graph.nodes.filter((node) => node.filePath && ["file", "config", "document", "service", "pipeline", "table", "schema", "resource", "endpoint"].includes(node.type)).map((node) => [slash(node.filePath), node]));
const functions = graph.nodes.filter((node) => node.type === "function" && node.filePath?.toLowerCase().endsWith(".cs"));
const classes = graph.nodes.filter((node) => node.type === "class" && node.filePath?.toLowerCase().endsWith(".cs"));
const functionsByFile = new Map();
const classesByFile = new Map();
for (const node of functions) (functionsByFile.get(node.filePath) ?? functionsByFile.set(node.filePath, []).get(node.filePath)).push(node);
for (const node of classes) (classesByFile.get(node.filePath) ?? classesByFile.set(node.filePath, []).get(node.filePath)).push(node);

const fingerprintFiles = fingerprints.files ?? {};
const classNameIndex = new Map();
const functionNameIndex = new Map();
for (const node of classes) (classNameIndex.get(node.name) ?? classNameIndex.set(node.name, []).get(node.name)).push(node);
for (const node of functions) (functionNameIndex.get(node.name) ?? functionNameIndex.set(node.name, []).get(node.name)).push(node);

for (const node of classes) {
  const source = readSource(node.filePath);
  if (!source || !node.lineRange) continue;
  const [start, end] = node.lineRange;
  const signature = declarationText(source.lines, start, end, node.name);
  const doc = getDocBlock(source.lines, start);
  const kindMatch = signature.match(/\b(class|interface|record(?:\s+struct)?|struct|enum)\s+/);
  const baseMatch = signature.match(/:\s*([^\{]+?)(?:\s+where\b|$)/);
  const baseTypes = baseMatch ? splitTopLevel(baseMatch[1]).map((item) => item.replace(/\s+where[\s\S]*$/, "").trim()) : [];
  const prefix = signature.slice(0, kindMatch?.index ?? 0).split(/\s+/);
  const accessibility = ["public", "protected", "internal", "private"].find((item) => prefix.includes(item)) ?? "internal";
  const modifiers = prefix.filter((item) => modifierSet.has(item) && item !== accessibility);
  const body = source.lines.slice(start - 1, end).join("\n");
  const properties = unique([...body.matchAll(/\b(?:public|protected|internal)\s+(?:static\s+|virtual\s+|override\s+|required\s+|init\s+|new\s+)*([\w<>,.?\[\] ]+)\s+([A-Za-z_]\w*)\s*\{\s*(?:get|set|init)\b/g)].map((match) => `${compact(match[1])} ${match[2]}`));
  const events = unique([...body.matchAll(/\bevent\s+([\w<>,.?\[\] ]+)\s+([A-Za-z_]\w*)/g)].map((match) => `${compact(match[1])} ${match[2]}`));
  const relatedSamples = sampleIndex.get(node.name) ?? [];
  const relatedTests = testIndex.get(node.name) ?? [];
  const obsolete = /\[Obsolete(?:Attribute)?(?:\(([^\]]*)\))?\]/.exec(source.lines.slice(Math.max(0, start - 5), start).join(" "));
  node.summary = doc.summary || node.summary;
  node.apiMeta = {
    kind: kindMatch?.[1] ?? "class",
    namespace: source.text.match(/\bnamespace\s+([\w.]+)/)?.[1] ?? "",
    accessibility,
    modifiers,
    signature,
    baseTypes,
    properties,
    events,
    methods: (functionsByFile.get(node.filePath) ?? []).filter((fn) => fn.lineRange && fn.lineRange[0] >= start && fn.lineRange[1] <= end).map((fn) => fn.id),
    typeParameters: doc.typeParameters,
    remarks: doc.remarks,
    example: doc.example,
    relatedSamples,
    relatedTests,
    project: projectInfo(node.filePath),
    lifecycle: { disposable: /\bI(?:Async)?Disposable\b/.test(body), obsolete: Boolean(obsolete), obsoleteMessage: decodeXml(obsolete?.[1] ?? "") },
    trust: { summary: doc.summary ? "xml" : "ai", signature: "code", relationships: "code-inferred" },
    slug: `class/${slugify(node.name)}-${hash(node.id)}`,
  };
}

for (const node of functions) {
  const source = readSource(node.filePath);
  if (!source || !node.lineRange) continue;
  const [start, end] = node.lineRange;
  const containingClass = (classesByFile.get(node.filePath) ?? []).filter((cls) => cls.lineRange && cls.lineRange[0] <= start && cls.lineRange[1] >= end).sort((a, b) => (a.lineRange[1] - a.lineRange[0]) - (b.lineRange[1] - b.lineRange[0]))[0];
  const signature = declarationText(source.lines, start, end, node.name);
  const doc = getDocBlock(source.lines, start);
  const fpFunctions = fingerprintFiles[node.filePath]?.functions ?? [];
  const fingerprint = fpFunctions.find((item) => item.name === node.name);
  const declaration = parseMethodDeclaration(signature, node.name, containingClass?.name, doc, fingerprint);
  const body = source.lines.slice(start - 1, end).join("\n");
  const exceptions = [...doc.exceptions];
  for (const exception of codeExceptions(source.lines, start, end)) {
    if (!exceptions.some((item) => item.type === exception.type)) exceptions.push(exception);
  }
  const relatedSamples = sampleIndex.get(node.name) ?? [];
  const relatedTests = testIndex.get(node.name) ?? [];
  node.summary = doc.summary || node.summary;
  node.apiMeta = {
    kind: declaration.constructor ? "constructor" : "method",
    namespace: source.text.match(/\bnamespace\s+([\w.]+)/)?.[1] ?? "",
    containingType: containingClass?.id ?? "",
    accessibility: declaration.accessibility,
    modifiers: declaration.modifiers,
    signature,
    parameters: declaration.parameters,
    returns: declaration.constructor ? null : { type: declaration.returnType, description: doc.returns, descriptionSource: doc.returns ? "xml" : "signature" },
    exceptions,
    remarks: doc.remarks,
    example: doc.example,
    sideEffects: detectSideEffects(body),
    cancellationSupported: declaration.parameters.some((parameter) => /CancellationToken/.test(parameter.type)),
    relatedSamples,
    relatedTests,
    project: projectInfo(node.filePath),
    trust: { summary: doc.summary ? "xml" : "ai", signature: "code", parameters: Object.keys(doc.parameters).length ? "xml+code" : "code", returns: doc.returns ? "xml+code" : "code", exceptions: exceptions.length ? "xml+code" : "not-documented", relationships: "code-inferred" },
    slug: `method/${slugify(node.name)}-${hash(node.id)}`,
  };
}

const generatedEdges = [];
const edgeKey = new Set(graph.edges.filter((edge) => !String(edge.description ?? "").startsWith("dreamine-enricher:")).map((edge) => `${edge.type}|${edge.source}|${edge.target}`));
graph.edges = graph.edges.filter((edge) => !String(edge.description ?? "").startsWith("dreamine-enricher:"));
function addEdge(source, target, type, description, weight = 0.7) {
  if (!source || !target || source === target || !nodesById.has(source) || !nodesById.has(target)) return;
  const key = `${type}|${source}|${target}`;
  if (edgeKey.has(key)) return;
  edgeKey.add(key);
  generatedEdges.push({ source, target, type, direction: "forward", description: `dreamine-enricher:${description}`, weight });
}

for (const fn of functions) {
  const owner = fn.apiMeta?.containingType;
  if (owner) addEdge(owner, fn.id, "contains", "class-method", 1);
  for (const testPath of fn.apiMeta?.relatedTests ?? []) addEdge(fn.id, fileNodesByPath.get(testPath)?.id, "tested_by", "symbol-reference", 0.7);
  for (const samplePath of fn.apiMeta?.relatedSamples ?? []) addEdge(fn.id, fileNodesByPath.get(samplePath)?.id, "related", "used-by-sample", 0.6);
  const source = readSource(fn.filePath);
  if (!source || !fn.lineRange) continue;
  const body = source.lines.slice(fn.lineRange[0] - 1, fn.lineRange[1]).join("\n");
  const calls = unique([...body.matchAll(/\b([A-Za-z_]\w*)\s*(?:<[^;{}()]+>)?\s*\(/g)].map((match) => match[1])).slice(0, 40);
  for (const calledName of calls) {
    const candidates = (functionNameIndex.get(calledName) ?? []).filter((candidate) => candidate.id !== fn.id);
    const sameFile = candidates.filter((candidate) => candidate.filePath === fn.filePath);
    const target = sameFile.length === 1 ? sameFile[0] : candidates.length === 1 ? candidates[0] : null;
    if (target) addEdge(fn.id, target.id, "calls", "identifier-call", 0.65);
  }
}

for (const cls of classes) {
  for (const testPath of cls.apiMeta?.relatedTests ?? []) addEdge(cls.id, fileNodesByPath.get(testPath)?.id, "tested_by", "symbol-reference", 0.7);
  for (const samplePath of cls.apiMeta?.relatedSamples ?? []) addEdge(cls.id, fileNodesByPath.get(samplePath)?.id, "related", "used-by-sample", 0.6);
  for (const baseRaw of cls.apiMeta?.baseTypes ?? []) {
    const baseName = baseRaw.replace(/<.*>/, "").split(".").at(-1)?.trim();
    const candidates = classNameIndex.get(baseName) ?? [];
    if (candidates.length !== 1) continue;
    const target = candidates[0];
    const relationship = target.apiMeta?.kind === "interface" || /^I[A-Z]/.test(baseName) ? "implements" : "inherits";
    addEdge(cls.id, target.id, relationship, "declared-base-type", 0.9);
  }
}

graph.edges.push(...generatedEdges);

const outgoing = new Map(), incoming = new Map();
for (const edge of graph.edges) {
  (outgoing.get(edge.source) ?? outgoing.set(edge.source, []).get(edge.source)).push(edge);
  (incoming.get(edge.target) ?? incoming.set(edge.target, []).get(edge.target)).push(edge);
}
for (const node of graph.nodes.filter((item) => item.filePath && fileNodesByPath.get(item.filePath)?.id === item.id)) {
  const children = (outgoing.get(node.id) ?? []).filter((edge) => edge.type === "contains").map((edge) => nodesById.get(edge.target)).filter(Boolean);
  node.fileMeta = {
    classCount: children.filter((child) => child.type === "class").length,
    methodCount: children.filter((child) => child.type === "function").length,
    outgoingRelations: (outgoing.get(node.id) ?? []).length,
    incomingRelations: (incoming.get(node.id) ?? []).length,
    project: projectInfo(node.filePath),
    trust: "code-derived",
  };
}

const apiNodes = graph.nodes.filter((node) => node.apiMeta);
graph.project.enrichmentVersion = "dreamine-api-v1";
graph.project.enrichedAt = new Date().toISOString();
graph.project.outputLanguage = outputLanguage;
if (outputLanguage === "en") {
  graph.project.description = "Dreamine.MVVM.FullKit is a .NET 8/9 toolkit for WPF, WinForms, Blazor, and .NET MAUI applications.";
  for (const node of graph.nodes) {
    if (/[가-힣]/.test(node.summary ?? "") && !node.apiMeta) {
      const owner = node.fileMeta?.project?.project ?? "Dreamine.MVVM.FullKit";
      node.summary = `${node.name} is a ${node.type} component in ${owner}.`;
    }
    if (/[가-힣]/.test(node.languageNotes ?? "")) {
      node.languageNotes = `${node.name} is implemented as part of the ${node.type} structure.`;
    }
  }
}
graph.project.apiDocumentation = {
  classes: classes.length,
  methods: functions.length,
  withXmlSummary: apiNodes.filter((node) => node.apiMeta?.trust?.summary === "xml").length,
  withParameters: functions.filter((node) => node.apiMeta?.parameters?.length).length,
  withReturns: functions.filter((node) => node.apiMeta?.returns?.type).length,
  withExceptions: functions.filter((node) => node.apiMeta?.exceptions?.length).length,
  generatedRelationships: generatedEdges.length,
};

fs.mkdirSync(path.dirname(outputGraphPath), { recursive: true });
fs.writeFileSync(outputGraphPath, JSON.stringify(graph, null, 2));
process.stdout.write(`${JSON.stringify(graph.project.apiDocumentation, null, 2)}\n`);
