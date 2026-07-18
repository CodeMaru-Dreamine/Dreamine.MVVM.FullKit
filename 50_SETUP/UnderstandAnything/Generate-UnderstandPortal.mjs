import fs from 'node:fs';
import path from 'node:path';

const [graphFile, destination] = process.argv.slice(2);
if (!graphFile || !destination) {
  console.error('Usage: node Generate-UnderstandPortal.mjs <knowledge-graph.json> <destination>');
  process.exit(1);
}

const graph = JSON.parse(fs.readFileSync(graphFile, 'utf8').replace(/^\uFEFF/, ''));
const root = path.resolve(destination);
fs.mkdirSync(root, { recursive: true });

const html = value => String(value ?? '').replace(/[&<>"']/g, ch => ({
  '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
})[ch]);
const oneLine = value => String(value ?? '').replace(/\s+/g, ' ').trim();
const truncate = (value, max = 160) => {
  const text = oneLine(value);
  return text.length > max ? `${text.slice(0, max - 1)}…` : text;
};

const apiNodes = graph.nodes.filter(node => node.apiMeta?.slug);
const apiIndex = apiNodes.map(node => ({
  id: node.id,
  kind: node.apiMeta.kind,
  name: node.name,
  summary: node.summary || '',
  signature: node.apiMeta.signature || '',
  slug: node.apiMeta.slug,
  project: node.apiMeta.project?.project || '',
  packageId: node.apiMeta.project?.packageId || '',
  trust: node.apiMeta.trust?.summary || '',
  deprecated: Boolean(node.apiMeta.lifecycle?.obsolete),
  tags: node.tags || []
})).sort((a, b) => a.name.localeCompare(b.name, 'ko'));

fs.writeFileSync(path.join(root, 'api-index.json'), JSON.stringify(apiIndex), 'utf8');

const sitemap = ['https://dreamine.kr/understand/'];
for (const node of apiNodes) {
  const meta = node.apiMeta;
  const directory = path.join(root, 'api', ...meta.slug.split('/'));
  fs.mkdirSync(directory, { recursive: true });
  const canonical = `https://dreamine.kr/understand/api/${meta.slug}/`;
  const title = `${node.name} API · Dreamine FullKit`;
  const description = truncate(node.summary || meta.signature || `${node.name} API reference`);
  const redirect = `/understand/#/api/${encodeURIComponent(node.id)}`;
  const parameters = (meta.parameters || []).map(p => `<tr><td><code>${html(p.name)}</code></td><td><code>${html(p.type)}</code></td><td>${html(p.description || '설명 없음')}</td></tr>`).join('');
  const exceptions = (meta.exceptions || []).map(x => `<li><code>${html(x.type)}</code> — ${html(x.description || x.condition || '코드에서 발생')}</li>`).join('');
  const page = `<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>${html(title)}</title><meta name="description" content="${html(description)}"><link rel="canonical" href="${canonical}">
<meta property="og:type" content="article"><meta property="og:site_name" content="Dreamine"><meta property="og:locale" content="ko_KR">
<meta property="og:title" content="${html(title)}"><meta property="og:description" content="${html(description)}"><meta property="og:url" content="${canonical}">
<meta property="og:image" content="https://dreamine.kr/understand/og-knowledge-graph.png"><meta name="twitter:card" content="summary_large_image">
<meta http-equiv="refresh" content="0;url=${html(redirect)}"><style>body{font:16px/1.65 system-ui;background:#090d14;color:#eef6ff;max-width:900px;margin:4rem auto;padding:0 1rem}a{color:#7ef0c5}code{color:#aeeed9}table{border-collapse:collapse;width:100%}td,th{border-bottom:1px solid #263446;padding:.6rem;text-align:left}</style>
</head><body><main><p>Dreamine.MVVM.FullKit API</p><h1>${html(node.name)}</h1><p>${html(node.summary || '')}</p><pre><code>${html(meta.signature || '')}</code></pre>
${parameters ? `<h2>매개변수</h2><table><thead><tr><th>이름</th><th>형식</th><th>설명</th></tr></thead><tbody>${parameters}</tbody></table>` : ''}
${meta.returns ? `<h2>반환값</h2><p><code>${html(meta.returns.type)}</code> — ${html(meta.returns.description || '')}</p>` : ''}
${exceptions ? `<h2>예외</h2><ul>${exceptions}</ul>` : ''}<p><a href="${html(redirect)}">인터랙티브 지식 허브에서 열기 →</a></p></main>
<script>location.replace(${JSON.stringify(redirect)});</script></body></html>`;
  fs.writeFileSync(path.join(directory, 'index.html'), page, 'utf8');
  sitemap.push(canonical);
}

const sitemapXml = `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${sitemap.map(url => `  <url><loc>${html(url)}</loc></url>`).join('\n')}\n</urlset>\n`;
fs.writeFileSync(path.join(root, 'sitemap-understand.xml'), sitemapXml, 'utf8');

const stats = {
  generatedAt: new Date().toISOString(),
  nodes: graph.nodes.length,
  edges: graph.edges.length,
  layers: graph.layers?.length || 0,
  apiPages: apiNodes.length,
  classes: apiNodes.filter(node => !['method', 'constructor'].includes(node.apiMeta.kind)).length,
  methods: apiNodes.filter(node => ['method', 'constructor'].includes(node.apiMeta.kind)).length
};
fs.writeFileSync(path.join(root, 'site-stats.json'), JSON.stringify(stats, null, 2), 'utf8');
console.log(JSON.stringify(stats));
