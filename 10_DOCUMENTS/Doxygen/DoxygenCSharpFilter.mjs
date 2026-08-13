#!/usr/bin/env node

import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(scriptDirectory, "..", "..");
const libraryRoot = path.join(repositoryRoot, "20_SOURCES", "100. Library");
const contractProjects = [
  "Communication.Abstractions",
  "Secs.Abstractions",
  "Secs.Com",
  "Gem.Abstractions",
  "Gem",
  "Gem300.Abstractions",
  "Gem300",
];
const excludedDirectories = new Set(["bin", "obj", "tests", "samples", ".git"]);
const cachePath = path.join(os.tmpdir(), "dreamine-doxygen-csharp-contracts-v1.json");

function walkCSharpFiles(directory, result = []) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    if (entry.isDirectory() && excludedDirectories.has(entry.name)) continue;
    const entryPath = path.join(directory, entry.name);
    if (entry.isDirectory()) walkCSharpFiles(entryPath, result);
    else if (entry.isFile() && entry.name.endsWith(".cs")) result.push(entryPath);
  }
  return result;
}

function splitTopLevel(value) {
  const parts = [];
  let start = 0;
  let angle = 0;
  let parenthesis = 0;
  let bracket = 0;
  for (let index = 0; index < value.length; index += 1) {
    switch (value[index]) {
      case "<": angle += 1; break;
      case ">": angle = Math.max(0, angle - 1); break;
      case "(": parenthesis += 1; break;
      case ")": parenthesis = Math.max(0, parenthesis - 1); break;
      case "[": bracket += 1; break;
      case "]": bracket = Math.max(0, bracket - 1); break;
      case ",":
        if (angle === 0 && parenthesis === 0 && bracket === 0) {
          parts.push(value.slice(start, index));
          start = index + 1;
        }
        break;
    }
  }
  parts.push(value.slice(start));
  return parts;
}

function removeDefaultValue(parameter) {
  let angle = 0;
  let parenthesis = 0;
  let bracket = 0;
  for (let index = 0; index < parameter.length; index += 1) {
    switch (parameter[index]) {
      case "<": angle += 1; break;
      case ">": angle = Math.max(0, angle - 1); break;
      case "(": parenthesis += 1; break;
      case ")": parenthesis = Math.max(0, parenthesis - 1); break;
      case "[": bracket += 1; break;
      case "]": bracket = Math.max(0, bracket - 1); break;
      case "=":
        if (angle === 0 && parenthesis === 0 && bracket === 0) return parameter.slice(0, index);
        break;
    }
  }
  return parameter;
}

function normalizeParameter(parameter) {
  let value = removeDefaultValue(parameter)
    .replace(/^\s*(?:\[[^\]]+\]\s*)+/, "")
    .replace(/^\s*(?:this|ref|out|in|params|scoped)\s+/, "")
    .trim();
  value = value.replace(/\s+[A-Za-z_][A-Za-z0-9_]*\s*$/, "");
  return value.replace(/\s+/g, "").replace(/global::/g, "");
}

function findClosingParenthesis(value, openIndex) {
  let depth = 0;
  for (let index = openIndex; index < value.length; index += 1) {
    if (value[index] === "(") depth += 1;
    else if (value[index] === ")" && --depth === 0) return index;
  }
  return -1;
}

function memberKey(declaration) {
  const compact = declaration.replace(/\s+/g, " ").trim();
  const openIndex = compact.indexOf("(");
  const delimiters = [compact.indexOf("=>"), compact.indexOf("{"), compact.indexOf(";")]
    .filter((index) => index >= 0);
  const declarationEnd = delimiters.length > 0 ? Math.min(...delimiters) : -1;
  if (openIndex >= 0 && (declarationEnd < 0 || openIndex < declarationEnd)) {
    const closeIndex = findClosingParenthesis(compact, openIndex);
    if (closeIndex < 0) return null;
    const prefix = compact.slice(0, openIndex);
    const nameMatch = prefix.match(/(?:operator\s+[^\s]+|[A-Za-z_][A-Za-z0-9_]*)\s*$/);
    if (!nameMatch || nameMatch[0].startsWith("operator")) return null;
    const parameters = compact.slice(openIndex + 1, closeIndex).trim();
    const parameterTypes = parameters.length === 0
      ? []
      : splitTopLevel(parameters).map(normalizeParameter);
    return `${nameMatch[0]}(${parameterTypes.join(",")})`;
  }

  const propertyPrefix = compact.split(/=>|\{|;/, 1)[0];
  const propertyMatch = propertyPrefix.match(/([A-Za-z_][A-Za-z0-9_]*)\s*$/);
  return propertyMatch ? `${propertyMatch[1]}{property}` : null;
}

function broadMemberKey(key) {
  if (key.endsWith("{property}")) return key;
  const open = key.indexOf("(");
  const parameters = key.slice(open + 1, -1);
  return `${key.slice(0, open)}#${parameters.length === 0 ? 0 : splitTopLevel(parameters).length}`;
}

function findContainer(lines, beforeIndex) {
  const prefix = lines.slice(0, beforeIndex).join("\n");
  const pattern = /\b(?:class|interface|struct|record(?:\s+(?:class|struct))?)\s+([A-Za-z_][A-Za-z0-9_]*)(?:\s*<[^>{}\r\n]+>)?\s*(?:\([^{};]*\))?\s*(?::\s*([^\{]+))?\s*\{/g;
  let match;
  let last = null;
  while ((match = pattern.exec(prefix)) !== null) {
    const bases = (match[2] ?? "")
      .split(",")
      .map((value) => value.trim().replace(/<.*$/, "").split(".").at(-1))
      .filter(Boolean);
    last = { name: match[1], bases };
  }
  return last;
}

function readDeclaration(lines, startIndex) {
  let index = startIndex;
  while (index < lines.length && (lines[index].trim() === "" || lines[index].trimStart().startsWith("["))) index += 1;
  const declaration = [];
  let parenthesis = 0;
  for (; index < lines.length; index += 1) {
    const line = lines[index].trim();
    declaration.push(line);
    for (const character of line) {
      if (character === "(") parenthesis += 1;
      else if (character === ")") parenthesis = Math.max(0, parenthesis - 1);
    }
    if (parenthesis === 0 && (line.includes("=>") || line.includes("{") || line.endsWith(";"))) break;
  }
  return declaration.join(" ");
}

function oneLineXmlComment(commentLines) {
  return commentLines
    .map((line) => line.replace(/^\s*\/\/\/\s?/, "").trim())
    .filter(Boolean)
    .join(" ")
    .replace(/\s+/g, " ")
    .trim();
}

function buildContractIndex() {
  const exact = new Map();
  const broad = new Map();
  const typeBases = new Map();
  for (const project of contractProjects) {
    const projectPath = path.join(libraryRoot, project);
    for (const sourcePath of walkCSharpFiles(projectPath)) {
      const lines = fs.readFileSync(sourcePath, "utf8").split(/\r?\n/);
      for (let lineIndex = 0; lineIndex < lines.length; lineIndex += 1) {
        const container = findContainer(lines, lineIndex + 1);
        if (container && !typeBases.has(container.name)) typeBases.set(container.name, container.bases);
      }
      for (let lineIndex = 0; lineIndex < lines.length; lineIndex += 1) {
        if (!lines[lineIndex].trimStart().startsWith("///")) continue;
        const comments = [];
        while (lineIndex < lines.length && lines[lineIndex].trimStart().startsWith("///")) {
          comments.push(lines[lineIndex]);
          lineIndex += 1;
        }
        const xml = oneLineXmlComment(comments);
        if (!xml.includes("<summary") || xml.includes("<inheritdoc")) continue;
        const key = memberKey(readDeclaration(lines, lineIndex));
        if (!key) continue;
        const container = findContainer(lines, lineIndex);
        if (!container) continue;
        const candidate = { xml, container: container.name };
        const exactValues = exact.get(key) ?? [];
        if (!exactValues.some((value) => value.xml === xml && value.container === container.name)) exactValues.push(candidate);
        exact.set(key, exactValues);
        const broadKey = broadMemberKey(key);
        const broadValues = broad.get(broadKey) ?? [];
        if (!broadValues.some((value) => value.xml === xml && value.container === container.name)) broadValues.push(candidate);
        broad.set(broadKey, broadValues);
      }
    }
  }
  return { exact, broad, typeBases };
}

function saveContractIndex(index) {
  const serialized = {
    exact: Object.fromEntries(index.exact),
    broad: Object.fromEntries(index.broad),
    typeBases: Object.fromEntries(index.typeBases),
  };
  fs.writeFileSync(cachePath, JSON.stringify(serialized), "utf8");
}

function loadContractIndex() {
  if (!fs.existsSync(cachePath)) return null;
  try {
    const serialized = JSON.parse(fs.readFileSync(cachePath, "utf8"));
    return {
      exact: new Map(Object.entries(serialized.exact)),
      broad: new Map(Object.entries(serialized.broad)),
      typeBases: new Map(Object.entries(serialized.typeBases)),
    };
  } catch {
    return null;
  }
}

const auditMode = process.argv[2] === "--audit";
const contractIndex = auditMode ? buildContractIndex() : loadContractIndex() ?? buildContractIndex();
if (auditMode || !fs.existsSync(cachePath)) saveContractIndex(contractIndex);

function contractClosure(container) {
  const result = new Set();
  const pending = [...(container?.bases ?? [])];
  while (pending.length > 0) {
    const type = pending.pop();
    if (!type || result.has(type)) continue;
    result.add(type);
    pending.push(...(contractIndex.typeBases.get(type) ?? []));
  }
  return result;
}

function resolveComment(key, container) {
  const contracts = contractClosure(container);
  const narrowToContracts = (values) => {
    const matching = values.filter((value) => contracts.has(value.container));
    return matching.length > 0 ? matching : values;
  };
  let candidates = narrowToContracts(contractIndex.exact.get(key) ?? []);
  if (new Set(candidates.map((value) => value.xml)).size !== 1) {
    candidates = narrowToContracts(contractIndex.broad.get(broadMemberKey(key)) ?? []);
  }
  const comments = [...new Set(candidates.map((value) => value.xml))];
  return { comment: comments.length === 1 ? comments[0] : null, candidates: comments.length };
}

function expandInheritdoc(source, sourcePath) {
  const lines = source.split(/(?<=\n)/);
  let expanded = 0;
  const unresolved = [];
  const logicalLines = lines.map((line) => line.replace(/\r?\n$/, ""));
  for (let lineIndex = 0; lineIndex < lines.length; lineIndex += 1) {
    if (!/^\s*\/\/\/\s*<inheritdoc\s*\/>\s*(?:\r?\n)?$/.test(lines[lineIndex])) continue;
    const declaration = readDeclaration(logicalLines, lineIndex + 1);
    const key = memberKey(declaration);
    const container = findContainer(logicalLines, lineIndex);
    const resolution = key ? resolveComment(key, container) : { comment: null, candidates: 0 };
    if (!resolution.comment) {
      unresolved.push({ line: lineIndex + 1, key: key ?? "<unknown>", candidates: resolution.candidates });
      continue;
    }
    const indentation = lines[lineIndex].match(/^\s*/)?.[0] ?? "";
    const newline = lines[lineIndex].endsWith("\r\n") ? "\r\n" : lines[lineIndex].endsWith("\n") ? "\n" : "";
    lines[lineIndex] = `${indentation}/// ${resolution.comment}${newline}`;
    expanded += 1;
  }
  return { source: lines.join(""), expanded, unresolved, sourcePath };
}

function makeDoxygenParserCompatible(source) {
  let filtered = source;

  // Doxygen 1.17 misclassifies non-positional C# records as namespace-level
  // properties. Presenting them as classes changes documentation parsing only.
  filtered = filtered.replace(
    /\brecord(?:\s+class)?(?=\s+[A-Za-z_][A-Za-z0-9_]*(?:<[^>{}\r\n]+>)?\s*(?:\r?\n\s*)?\{)/g,
    "class");

  // Doxygen 1.17 confuses expression-bodied delegating constructors with an
  // initializer list. An equivalent block body keeps source locations stable.
  filtered = filtered.replace(
    /(:\s*(?:base|this)\s*\([^\r\n]*\))\s*=>\s*([^;\r\n]+);/g,
    "$1 { $2; }");

  // Named tuple return types are currently parsed as properties by Doxygen.
  // The ValueTuple spellings below are documentation-only equivalents.
  const tupleTypes = new Map([
    ["(string ModelNumber, string SoftwareRevision)?", "System.ValueTuple<string, string>?"],
    ["(byte Acknowledgement, (string ModelNumber, string SoftwareRevision)? Identity)", "System.ValueTuple<byte, System.ValueTuple<string, string>?>"],
    ["(ulong DataId, IReadOnlyList<E30ReportDefinition> Reports)", "System.ValueTuple<ulong, IReadOnlyList<E30ReportDefinition>>"],
    ["(ulong DataId, IReadOnlyList<E30EventLink> Links)", "System.ValueTuple<ulong, IReadOnlyList<E30EventLink>>"],
    ["(bool Enabled, IReadOnlyList<ulong> CollectionEventIds)", "System.ValueTuple<bool, IReadOnlyList<ulong>>"],
    ["(string Name, IReadOnlyList<E30CommandParameter> Parameters)", "System.ValueTuple<string, IReadOnlyList<E30CommandParameter>>"],
    ["(bool Enabled, ulong? AlarmId)", "System.ValueTuple<bool, ulong?>"],
  ]);
  for (const [tupleType, replacement] of [...tupleTypes].sort(([left], [right]) => right.length - left.length)) {
    filtered = filtered.replaceAll(tupleType, replacement);
  }
  return filtered;
}

function filterFile(sourcePath) {
  const source = fs.readFileSync(sourcePath, "utf8");
  const result = expandInheritdoc(source, sourcePath);
  result.source = makeDoxygenParserCompatible(result.source);
  return result;
}

if (auditMode) {
  const roots = process.argv.slice(3);
  if (roots.length === 0) {
    process.stderr.write("DoxygenCSharpFilter --audit requires at least one project directory.\n");
    process.exit(2);
  }
  let expanded = 0;
  const unresolved = [];
  for (const root of roots) {
    for (const sourcePath of walkCSharpFiles(path.resolve(root))) {
      const result = filterFile(sourcePath);
      expanded += result.expanded;
      unresolved.push(...result.unresolved.map((item) => ({ ...item, sourcePath })));
    }
  }
  for (const item of unresolved) {
    process.stderr.write(`${path.relative(repositoryRoot, item.sourcePath)}:${item.line}: unresolved <inheritdoc/> ${item.key}; candidates=${item.candidates}\n`);
  }
  process.stdout.write(`ExpandedInheritdoc=${expanded} UnresolvedInheritdoc=${unresolved.length}\n`);
  process.exit(unresolved.length === 0 ? 0 : 1);
}

const sourcePath = process.argv[2];
if (!sourcePath) {
  process.stderr.write("DoxygenCSharpFilter requires a source-file path.\n");
  process.exit(2);
}
const result = filterFile(sourcePath);
if (result.unresolved.length > 0) {
  for (const item of result.unresolved) {
    process.stderr.write(`${sourcePath}:${item.line}: unresolved <inheritdoc/> ${item.key}; candidates=${item.candidates}\n`);
  }
  process.exit(1);
}
process.stdout.write(result.source);
