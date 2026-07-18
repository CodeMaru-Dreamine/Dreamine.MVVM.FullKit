#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(process.argv[2] ?? path.join(scriptDir, "..", ".."));
const destinationRoot = path.resolve(process.argv[3] ?? path.join(repositoryRoot, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot", "understand"));
const sourcesRoot = path.join(repositoryRoot, "20_SOURCES");
const doxygenRoot = path.join(repositoryRoot, "10_DOCUMENTS", "Doxygen");
const graphPaths = {
  ko: path.resolve(process.argv[4] ?? path.join(repositoryRoot, ".ua", "knowledge-graph.ko.json")),
  en: path.resolve(process.argv[5] ?? path.join(repositoryRoot, ".ua", "knowledge-graph.en.json")),
};

const slash = (value) => String(value ?? "").replaceAll("\\", "/");
const compact = (value) => String(value ?? "").replace(/\s+/g, " ").trim();
const unique = (values) => [...new Set(values.filter(Boolean))];
const clone = (value) => JSON.parse(JSON.stringify(value));
const slugify = (value) => compact(value).normalize("NFKD").replace(/[^a-zA-Z0-9]+/g, "-").replace(/^-|-$/g, "").toLowerCase() || "project";
const encodePath = (...segments) => `/${segments.map((segment) => encodeURIComponent(segment)).join("/")}`;

for (const required of [sourcesRoot, doxygenRoot, graphPaths.ko, graphPaths.en]) {
  if (!fs.existsSync(required)) throw new Error(`Required input not found: ${required}`);
}
if (!destinationRoot.startsWith(path.join(sourcesRoot, "000. Project") + path.sep)) {
  throw new Error(`Unsafe destination: ${destinationRoot}`);
}

function walk(directory, result = []) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    if (["bin", "obj", ".git", ".vs", "node_modules", "TestResults"].includes(entry.name)) continue;
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) walk(fullPath, result);
    else if (entry.name.toLowerCase().endsWith(".csproj")) result.push(fullPath);
  }
  return result;
}

function tag(xml, name) {
  return compact(xml.match(new RegExp(`<${name}(?:\\s[^>]*)?>([\\s\\S]*?)<\\/${name}>`, "i"))?.[1]);
}

function readSummary(filePath, fallback) {
  if (!fs.existsSync(filePath)) return fallback;
  const text = fs.readFileSync(filePath, "utf8");
  return compact(text.match(/^>\s+(.+?)\s*$/m)?.[1]) || fallback;
}

const rootProps = fs.readFileSync(path.join(sourcesRoot, "Directory.Build.props"), "utf8");
const defaultVersion = tag(rootProps, "AppVersion") || tag(rootProps, "Version") || "1.0.0";
const projectFiles = walk(sourcesRoot).sort((a, b) => slash(a).localeCompare(slash(b)));
const projects = projectFiles.map((projectFile) => {
  const xml = fs.readFileSync(projectFile, "utf8");
  const directory = path.dirname(projectFile);
  const relativeProjectFile = slash(path.relative(repositoryRoot, projectFile));
  const relativeDirectory = slash(path.relative(repositoryRoot, directory));
  const sourceRelative = slash(path.relative(sourcesRoot, projectFile));
  const baseName = path.basename(projectFile, path.extname(projectFile));
  const displayName = tag(xml, "PackageId") || tag(xml, "AssemblyName") || tag(xml, "Title") || baseName;
  const assemblyName = tag(xml, "AssemblyName") || baseName;
  const category = sourceRelative.split("/")[0];
  const version = tag(xml, "Version") || tag(xml, "VersionPrefix") || tag(xml, "PackageVersion") || defaultVersion;
  const targetFrameworks = unique((tag(xml, "TargetFrameworks") || tag(xml, "TargetFramework") || "unknown").split(";").map(compact));
  const references = [...xml.matchAll(/<ProjectReference\s+Include=["']([^"']+)["']/gi)].map((match) => path.basename(match[1], path.extname(match[1])));
  const linkedFiles = [...xml.matchAll(/<(?:Compile|Page|ApplicationDefinition|Content|None|EmbeddedResource|AdditionalFiles|RazorComponent|MauiXaml|MauiImage|MauiFont|MauiAsset)\s+Include=["']([^"']+)["']/gi)]
    .map((match) => path.resolve(directory, match[1]))
    .filter((fullPath) => fullPath.startsWith(repositoryRoot + path.sep))
    .map((fullPath) => slash(path.relative(repositoryRoot, fullPath)));
  const slug = slugify(displayName);
  const summaryEn = readSummary(path.join(directory, "README.md"), `${displayName} provides APIs and components for Dreamine.MVVM.FullKit.`);
  const summaryKo = readSummary(path.join(directory, "README_KO.md"), `${displayName} 프로젝트의 API와 구성 요소를 제공합니다.`);
  const xmlDocPath = path.join(repositoryRoot, "20_SOURCES", "000. Project", "010. App", "Dreamine.Web", "wwwroot", "xmldocs", assemblyName, `${assemblyName}.xml`);
  return { projectFile, relativeProjectFile, directory, relativeDirectory, baseName, displayName, assemblyName, category, version, targetFrameworks, references, linkedFiles, slug, summaryEn, summaryKo, hasBuildDocument: fs.existsSync(xmlDocPath) };
});

const duplicateSlugs = [...new Set(projects.filter((project, index) => projects.findIndex((item) => item.slug === project.slug) !== index).map((project) => project.slug))];
if (duplicateSlugs.length) throw new Error(`Duplicate project slugs: ${duplicateSlugs.join(", ")}`);
if (projects.length !== 80) throw new Error(`Expected 80 projects under 20_SOURCES, found ${projects.length}.`);

const graphs = Object.fromEntries(Object.entries(graphPaths).map(([language, graphPath]) => [language, JSON.parse(fs.readFileSync(graphPath, "utf8"))]));
const fileLevelTypes = new Set(["file", "config", "document", "service", "pipeline", "table", "schema", "resource", "endpoint"]);

const linkedFileProjects = new Map();
for (const project of projects) {
  for (const filePath of project.linkedFiles) {
    const key = filePath.toLowerCase();
    const memberships = linkedFileProjects.get(key) ?? [];
    memberships.push(project);
    linkedFileProjects.set(key, memberships);
  }
}

function nodeProjects(node, projectByPath) {
  const metadataName = node.apiMeta?.project?.project ?? node.fileMeta?.project?.project;
  const metadataFile = slash(node.apiMeta?.project?.projectFile ?? node.fileMeta?.project?.projectFile);
  const memberships = [];
  if (metadataFile && projectByPath.has(metadataFile.toLowerCase())) memberships.push(projectByPath.get(metadataFile.toLowerCase()));
  if (node.filePath) {
    const normalized = slash(node.filePath);
    memberships.push(...(linkedFileProjects.get(normalized.toLowerCase()) ?? []));
    const matches = projects.filter((project) => normalized === project.relativeProjectFile || normalized.startsWith(`${project.relativeDirectory}/`)).sort((a, b) => b.relativeDirectory.length - a.relativeDirectory.length);
    if (matches.length) memberships.push(matches[0]);
  }
  const metadataProject = projects.find((project) => project.baseName === metadataName || project.displayName === metadataName);
  if (metadataProject) memberships.push(metadataProject);
  return unique(memberships);
}

function layerFor(node) {
  if (["config", "document", "pipeline", "resource", "schema"].includes(node.type)) return "overview";
  if (["file", "service", "endpoint", "table"].includes(node.type)) return "source";
  if (node.type === "class") return "types";
  if (node.type === "function") return "behavior";
  return "concepts";
}

const layerText = {
  ko: {
    overview: ["문서와 설정", "프로젝트 설명, 설정, 빌드 및 배포 자산입니다."],
    source: ["소스와 서비스", "프로젝트를 구성하는 소스 파일과 실행 서비스입니다."],
    types: ["클래스와 계약", "공개 클래스, 인터페이스, 레코드와 데이터 계약입니다."],
    behavior: ["메서드와 동작", "메서드, 생성자, 이벤트 처리와 실행 흐름입니다."],
    concepts: ["개념과 의존성", "프로젝트 루트, 외부 프로젝트 참조와 보조 개념입니다."],
  },
  en: {
    overview: ["Documentation and Configuration", "Project documentation, configuration, build, and deployment assets."],
    source: ["Source and Services", "Source files and runtime services that make up the project."],
    types: ["Types and Contracts", "Public classes, interfaces, records, and data contracts."],
    behavior: ["Methods and Behavior", "Methods, constructors, event handlers, and execution flows."],
    concepts: ["Concepts and Dependencies", "The project root, referenced projects, and supporting concepts."],
  },
};

const projectByPath = new Map(projects.map((project) => [slash(path.relative(repositoryRoot, project.projectFile)).toLowerCase(), project]));
const outputRoot = path.join(destinationRoot, "projects");
fs.mkdirSync(outputRoot, { recursive: true });
const catalog = [];

for (const project of projects) {
  const counts = {};
  for (const language of ["ko", "en"]) {
    const sourceGraph = graphs[language];
    const ownedNodes = sourceGraph.nodes.filter((node) => nodeProjects(node, projectByPath).some((membership) => membership.slug === project.slug));
    const nodes = clone(ownedNodes);
    const nodeIds = new Set(nodes.map((node) => node.id));
    const rootId = `module:${project.slug}`;
    nodes.push({
      id: rootId,
      type: "module",
      name: project.displayName,
      summary: language === "ko" ? project.summaryKo : project.summaryEn,
      tags: ["project", project.category, ...project.targetFrameworks],
      complexity: ownedNodes.length > 200 ? "complex" : ownedNodes.length > 60 ? "moderate" : "simple",
      projectMeta: { projectFile: project.relativeProjectFile, version: project.version, targetFrameworks: project.targetFrameworks },
    });
    nodeIds.add(rootId);

    const externalModules = [];
    for (const reference of unique(project.references)) {
      const dependency = projects.find((item) => item.baseName === reference || item.displayName === reference);
      const id = `module:external:${slugify(reference)}`;
      if (nodeIds.has(id)) continue;
      nodeIds.add(id);
      externalModules.push({ id, type: "module", name: dependency?.displayName ?? reference, summary: language === "ko" ? "이 프로젝트가 참조하는 외부 프로젝트입니다." : "An external project referenced by this project.", tags: ["external", "project-reference"], complexity: "simple" });
    }
    nodes.push(...externalModules);

    const edges = sourceGraph.edges.filter((edge) => nodeIds.has(edge.source) && nodeIds.has(edge.target)).map((edge) => clone(edge));
    const edgeKeys = new Set(edges.map((edge) => `${edge.type}|${edge.source}|${edge.target}`));
    const addEdge = (source, target, type, description, weight) => {
      const key = `${type}|${source}|${target}`;
      if (!edgeKeys.has(key)) {
        edgeKeys.add(key);
        edges.push({ source, target, type, direction: "forward", description, weight });
      }
    };
    for (const node of ownedNodes.filter((item) => fileLevelTypes.has(item.type))) addEdge(rootId, node.id, "contains", language === "ko" ? "프로젝트 파일" : "project file", 1);
    for (const external of externalModules) addEdge(rootId, external.id, "depends_on", language === "ko" ? "프로젝트 참조" : "project reference", 0.7);

    const assignments = { overview: [], source: [], types: [], behavior: [], concepts: [rootId, ...externalModules.map((node) => node.id)] };
    for (const node of ownedNodes) assignments[layerFor(node)].push(node.id);
    const layers = Object.entries(assignments).filter(([, ids]) => ids.length).map(([key, ids]) => ({ id: `layer:${key}`, name: layerText[language][key][0], description: layerText[language][key][1], nodeIds: unique(ids) }));
    const firstFiles = ownedNodes.filter((node) => fileLevelTypes.has(node.type)).slice(0, 5).map((node) => node.id);
    const firstTypes = ownedNodes.filter((node) => node.type === "class").slice(0, 5).map((node) => node.id);
    const firstMethods = ownedNodes.filter((node) => node.type === "function").slice(0, 5).map((node) => node.id);
    const tour = language === "ko" ? [
      { order: 1, title: "프로젝트 개요", description: `${project.displayName}의 목적과 대상 프레임워크를 확인합니다.`, nodeIds: [rootId] },
      { order: 2, title: "주요 파일", description: "진입점과 핵심 소스 파일을 살펴봅니다.", nodeIds: firstFiles },
      { order: 3, title: "클래스와 계약", description: "프로젝트의 핵심 형식과 공개 계약을 확인합니다.", nodeIds: firstTypes },
      { order: 4, title: "실행 흐름", description: "메서드와 호출 관계를 따라 실제 동작을 이해합니다.", nodeIds: firstMethods },
    ] : [
      { order: 1, title: "Project Overview", description: `Review the purpose and target frameworks of ${project.displayName}.`, nodeIds: [rootId] },
      { order: 2, title: "Key Files", description: "Inspect entry points and important source files.", nodeIds: firstFiles },
      { order: 3, title: "Types and Contracts", description: "Review the project's core types and public contracts.", nodeIds: firstTypes },
      { order: 4, title: "Execution Flow", description: "Follow methods and call relationships to understand runtime behavior.", nodeIds: firstMethods },
    ];
    const finalTour = tour.filter((step) => step.nodeIds.length);
    const projectGraph = {
      version: sourceGraph.version ?? "1.0.0",
      project: {
        name: project.displayName,
        languages: sourceGraph.project?.languages ?? ["csharp"],
        frameworks: project.targetFrameworks,
        description: language === "ko" ? project.summaryKo : project.summaryEn,
        analyzedAt: new Date().toISOString(),
        gitCommitHash: sourceGraph.project?.gitCommitHash ?? "",
        outputLanguage: language,
        sourceProjectFile: project.relativeProjectFile,
      },
      nodes,
      edges,
      layers,
      tour: finalTour,
    };
    const languageDirectory = path.join(outputRoot, project.slug, language);
    fs.mkdirSync(languageDirectory, { recursive: true });
    fs.writeFileSync(path.join(languageDirectory, "knowledge-graph.json"), JSON.stringify(projectGraph, null, 2));
    fs.writeFileSync(path.join(languageDirectory, "config.json"), JSON.stringify({ outputLanguage: language }, null, 2));
    fs.writeFileSync(path.join(languageDirectory, "meta.json"), JSON.stringify({ project: project.displayName, outputLanguage: language, nodes: nodes.length, edges: edges.length, generatedAt: new Date().toISOString() }, null, 2));
    counts[language] = { nodes: nodes.length, edges: edges.length };
  }

  const doxyPhysical = (language) => path.join(doxygenRoot, project.category, project.displayName, language === "ko" ? "KR" : "EN", "html", "index.html");
  if (!fs.existsSync(doxyPhysical("ko")) || !fs.existsSync(doxyPhysical("en"))) throw new Error(`Doxygen index missing for ${project.displayName}.`);
  catalog.push({
    slug: project.slug,
    name: project.displayName,
    category: project.category,
    version: project.version,
    targetFrameworks: project.targetFrameworks,
    projectFile: project.relativeProjectFile,
    descriptions: { ko: project.summaryKo, en: project.summaryEn },
    buildDocumentUrl: project.hasBuildDocument ? `/xmldocs/${encodeURIComponent(project.assemblyName)}/${encodeURIComponent(project.assemblyName)}.xml` : null,
    doxygenUrls: {
      ko: encodePath("docs", "doxygen", project.category, project.displayName, "KR", "html", "index.html"),
      en: encodePath("docs", "doxygen", project.category, project.displayName, "EN", "html", "index.html"),
    },
    graphUrls: {
      ko: `/understand/graph/?project=${encodeURIComponent(project.slug)}&lang=ko`,
      en: `/understand/graph/?project=${encodeURIComponent(project.slug)}&lang=en`,
    },
    counts,
  });
}

fs.writeFileSync(path.join(destinationRoot, "project-catalog.json"), JSON.stringify({ generatedAt: new Date().toISOString(), totalProjects: catalog.length, projects: catalog }, null, 2));
process.stdout.write(`Projects=${catalog.length} Graphs=${catalog.length * 2} Destination=${outputRoot}\n`);
