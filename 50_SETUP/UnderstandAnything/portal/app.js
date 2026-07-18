(() => {
  'use strict';
  const BASE = new URL('./', document.baseURI).pathname;
  const state = { graph: null, content: null, apiIndex: [], nodeById: new Map(), edgesByNode: new Map(), page: 1, query: '', kind: 'all' };
  const app = document.querySelector('#app');
  const loading = document.querySelector('#loading');
  const esc = value => String(value ?? '').replace(/[&<>"']/g, ch => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[ch]));
  const routeUrl = (name, value) => `#/${name}${value ? '/' + encodeURIComponent(value) : ''}`;
  const sourceUrl = path => `${BASE}source/${String(path).split('/').map(encodeURIComponent).join('/')}.json`;
  const unique = values => [...new Set(values.filter(Boolean))];
  const fmt = n => new Intl.NumberFormat('ko-KR').format(n || 0);
  const trustText = value => ({xml:'XML 문서',code:'실제 코드','xml+code':'XML + 코드',ai:'AI 분석','code-inferred':'코드 관계','signature':'시그니처',undocumented:'문서 없음'}[value] || value || '출처 미상');
  const codeBlock = (code, language = '') => `<div class="code-shell"><button type="button" class="copy-code" aria-label="코드 복사">코드 복사</button><pre><code class="language-${esc(language)}">${esc(code)}</code></pre></div>`;

  async function boot() {
    try {
      const [graph, content, apiIndex] = await Promise.all([
        fetch(`${BASE}knowledge-graph.json`).then(check).then(r => r.json()),
        fetch(`${BASE}onboarding.json`).then(check).then(r => r.json()),
        fetch(`${BASE}api-index.json`).then(check).then(r => r.json())
      ]);
      state.graph = graph; state.content = content; state.apiIndex = apiIndex;
      graph.nodes.forEach(n => state.nodeById.set(n.id, n));
      graph.edges.forEach(e => {
        [e.source, e.target].forEach(id => { if (!state.edgesByNode.has(id)) state.edgesByNode.set(id, []); state.edgesByNode.get(id).push(e); });
      });
      loading.hidden = true; app.hidden = false;
      window.addEventListener('hashchange', render);
      document.querySelector('#menuButton').addEventListener('click', toggleMenu);
      app.addEventListener('click', copyCode);
      render();
    } catch (error) {
      loading.innerHTML = `<div class="empty"><strong>지식 허브를 불러오지 못했습니다.</strong><p>${esc(error.message)}</p></div>`;
    }
  }
  const check = response => { if (!response.ok) throw new Error(`${response.url} (${response.status})`); return response; };
  function toggleMenu() { const nav = document.querySelector('#mainNav'); const open = nav.classList.toggle('open'); this.setAttribute('aria-expanded', open); }
  async function copyCode(event) {
    const button = event.target.closest('.copy-code');
    if (!button) return;
    const code = button.closest('.code-shell, .source-code-shell')?.querySelector('code');
    if (!code) return;
    let copied = false;
    button.textContent = '복사 중…';
    try {
      await navigator.clipboard.writeText(code.textContent);
      copied = true;
    } catch {
      try {
        const textarea = document.createElement('textarea');
        textarea.value = code.textContent; textarea.setAttribute('readonly', '');
        textarea.style.cssText = 'position:fixed;left:-9999px;top:0';
        document.body.appendChild(textarea); textarea.select();
        copied = document.execCommand('copy'); textarea.remove();
      } catch { copied = false; }
    }
    if (!copied) {
      const range = document.createRange(); range.selectNodeContents(code);
      const selection = window.getSelection(); selection.removeAllRanges(); selection.addRange(range);
    }
    button.textContent = copied ? '복사됨 ✓' : '선택됨 · Ctrl+C';
    setTimeout(() => { button.textContent = '코드 복사'; }, 1800);
  }
  function closeMenu() { document.querySelector('#mainNav').classList.remove('open'); document.querySelector('#menuButton').setAttribute('aria-expanded','false'); }
  function parseRoute() { const parts = (location.hash.replace(/^#\/?/, '') || 'start').split('/').map(decodeURIComponent); return {name: parts[0], id: parts.slice(1).join('/')}; }
  function setActive(name) { document.querySelectorAll('nav [data-route]').forEach(a => a.classList.toggle('active', a.dataset.route === name)); }
  function render() {
    const route = parseRoute(); setActive(route.name); closeMenu(); window.scrollTo(0,0);
    const views = {start: renderStart, recipes: () => route.id ? renderRecipe(route.id) : renderRecipes(), recipe: () => renderRecipe(route.id), api: () => route.id ? renderApiDetail(route.id) : renderApiList(), architecture: renderArchitecture, glossary: renderGlossary, troubleshooting: renderTroubleshooting};
    (views[route.name] || renderNotFound)();
    document.title = `${app.querySelector('h1')?.textContent || '지식 허브'} · Dreamine`;
  }
  function pageTitle(kicker, title, description) { return `<header class="page-title"><span class="eyebrow">${esc(kicker)}</span><h1>${esc(title)}</h1><p>${esc(description)}</p></header>`; }
  function tags(values) { return `<div class="tags">${(values || []).map(v => `<span class="tag">${esc(v)}</span>`).join('')}</div>`; }
  function recipeCard(r) { return `<a class="card card-link" href="${routeUrl('recipe',r.id)}"><span class="number">${esc(r.icon)}</span><h3>${esc(r.title)}</h3><p>${esc(r.summary)}</p>${tags([r.difficulty,r.time,...r.platforms.slice(0,2)])}</a>`; }
  function renderMiniGraph() {
    const layers = state.graph.layers || [];
    const cx = 450, cy = 210, rx = 330, ry = 160;
    const points = layers.map((layer, index) => {
      const angle = -Math.PI / 2 + (Math.PI * 2 * index / layers.length);
      return {layer, x: cx + Math.cos(angle) * rx, y: cy + Math.sin(angle) * ry, count: layer.nodeIds?.length || 0};
    });
    const edges = points.map(p => `<line x1="${cx}" y1="${cy}" x2="${p.x.toFixed(1)}" y2="${p.y.toFixed(1)}" stroke="#477d75" stroke-width="1.5" stroke-dasharray="5 6" opacity=".8" />`).join('');
    const nodes = points.map((p,index) => { const label=p.layer.name.replace(/\s*레이어$/,''); const short=label.length>15?`${label.slice(0,15)}…`:label; return `<a href="./graph/" target="_blank" rel="noopener" aria-label="${esc(p.layer.name)} 전체 그래프에서 보기"><g class="mini-node" style="--delay:${index*45}ms"><circle cx="${p.x.toFixed(1)}" cy="${p.y.toFixed(1)}" r="${Math.min(38,25+Math.sqrt(p.count)*.5).toFixed(1)}" fill="#17293a" stroke="#6c9fba" stroke-width="2"/><text class="mini-count" x="${p.x.toFixed(1)}" y="${(p.y-3).toFixed(1)}" fill="#eff6ff" text-anchor="middle" font-size="13" font-weight="700">${fmt(p.count)}</text><text class="mini-label" x="${p.x.toFixed(1)}" y="${(p.y+16).toFixed(1)}" fill="#b9cede" text-anchor="middle" font-size="9">${esc(short)}</text><title>${esc(p.layer.name)} · ${fmt(p.count)}개 노드</title></g></a>` }).join('');
    return `<div class="graph-preview"><div class="graph-canvas"><svg viewBox="0 0 900 420" role="img" aria-label="Dreamine FullKit 10개 아키텍처 레이어 지식 그래프"><g class="mini-edges">${edges}</g><g class="mini-center"><circle cx="${cx}" cy="${cy}" r="66" fill="#13332f" stroke="#7ef0c5" stroke-width="3"/><text x="${cx}" y="${cy-7}" fill="#eff6ff" text-anchor="middle" font-size="18" font-weight="700">Dreamine</text><text class="mini-center-sub" x="${cx}" y="${cy+19}" fill="#7ef0c5" text-anchor="middle" font-size="12">${fmt(state.graph.nodes.length)} nodes</text></g>${nodes}</svg></div><div class="graph-legend"><div><span class="eyebrow">LIVE ARCHITECTURE MAP</span><h3>10개 레이어 한눈에 보기</h3><p>원의 크기는 각 레이어의 노드 규모를 나타냅니다. 아래 레이어를 누르면 책임과 구성요소 설명을 볼 수 있습니다.</p></div><div class="layer-pills">${layers.map((l,i)=>`<a href="#/architecture" title="${esc(l.description)}"><i style="--i:${i}"></i><span>${esc(l.name)}</span><strong>${fmt(l.nodeIds?.length)}개</strong></a>`).join('')}</div><a class="button primary" href="./graph/" target="_blank" rel="noopener">전체 인터랙티브 그래프 열기 ↗</a></div></div>`;
  }

  function renderStart() {
    const c = state.content; const nodes = state.graph.nodes; const methods = state.apiIndex.filter(x => ['method','constructor'].includes(x.kind)).length; const classes = state.apiIndex.length - methods;
    app.innerHTML = `<div class="hero"><div><span class="eyebrow">OPEN SOURCE · BEGINNER FIRST</span><h1>코드를 읽기 전에<br><em>사용 흐름부터</em> 보세요.</h1><p>${esc(c.subtitle)}</p><div class="cta-row"><a class="button primary" href="${routeUrl('recipe',c.quickStart.recipeId)}">5분 빠른 시작 →</a><a class="button" href="#/api">API 검색</a><a class="button" href="./graph/" target="_blank" rel="noopener">전체 관계 그래프 ↗</a></div></div><div class="hero-stats"><div class="stat"><strong>${fmt(nodes.length)}</strong><span>분석 노드</span></div><div class="stat"><strong>${fmt(state.graph.edges.length)}</strong><span>코드 관계</span></div><div class="stat"><strong>${fmt(classes)}</strong><span>클래스·인터페이스</span></div><div class="stat"><strong>${fmt(methods)}</strong><span>메서드 API</span></div></div></div>
    <section class="home-graph"><div class="section-head"><div><h2>솔루션 지식 그래프</h2><p>FullKit 전체 구조와 레이어 규모를 메인 화면에서 먼저 살펴보세요.</p></div><a href="./graph/" target="_blank" rel="noopener">전체 그래프 열기 ↗</a></div>${renderMiniGraph()}</section>
    <section><div class="section-head"><div><h2>처음이라면 이 순서로</h2><p>큰 구조를 외우지 말고 작은 성공부터 연결합니다.</p></div></div><div class="path"><article><strong>01 · RUN</strong><h3>첫 화면 연결</h3><p>초기화, ViewModel, AutoWire 세 단계로 실행 결과를 먼저 만듭니다.</p></article><article><strong>02 · COPY</strong><h3>목적별 레시피</h3><p>통신·DB·PLC·Hybrid 중 지금 필요한 흐름과 샘플을 따라갑니다.</p></article><article><strong>03 · UNDERSTAND</strong><h3>API와 관계 확인</h3><p>매개변수·반환값·예외를 확인한 뒤 전체 그래프에서 영향 범위를 넓혀 봅니다.</p></article></div></section>
    <section><div class="section-head"><div><h2>무엇을 만들고 싶나요?</h2><p>완성하려는 작업에 가까운 카드를 고르세요.</p></div><a href="#/recipes">레시피 전체 보기</a></div><div class="cards">${c.recipes.slice(0,6).map(recipeCard).join('')}</div></section>
    <section><div class="section-head"><div><h2>문제가 생겼나요?</h2><p>자주 막히는 지점부터 확인할 수 있습니다.</p></div><a href="#/troubleshooting">진단 목록 열기</a></div><div class="cards">${c.troubleshooting.slice(0,3).map((x,i)=>`<a class="card card-link" href="#/troubleshooting"><span class="number">CHECK ${i+1}</span><h3>${esc(x.symptom)}</h3><p>${esc(x.checks[0])} 외 ${x.checks.length-1}개 확인</p></a>`).join('')}</div></section>`;
  }
  function renderRecipes() { const c=state.content; app.innerHTML=pageTitle('TASK RECIPES','목적별 사용 레시피','초보자가 실행 결과까지 도달하도록 준비물, 단계, 예상 결과와 흔한 실수를 함께 정리했습니다.')+`<section><div class="cards">${c.recipes.map(recipeCard).join('')}</div></section>`; }
  function renderRecipe(id) {
    const r=state.content.recipes.find(x=>x.id===id); if(!r) return renderNotFound();
    const related=findRelated(r.relatedNodeNames);
    app.innerHTML=pageTitle(`${r.difficulty} · ${r.time}`,r.title,r.summary)+`<div class="detail-grid"><div><section class="panel"><h2>시작 전 확인</h2>${tags([...r.platforms,...r.packages])}<ul>${r.prerequisites.map(x=>`<li>${esc(x)}</li>`).join('')}</ul></section><section class="panel"><h2>따라 하기</h2>${r.steps.map((s,i)=>`<article class="step"><span class="step-index">${i+1}</span><div><h3>${esc(s.title)}</h3><p>${esc(s.description)}</p>${codeBlock(s.code,s.language)}</div></article>`).join('')}</section><section class="panel"><h2>성공 기준</h2><div class="notice">${esc(r.expectedResult)}</div></section><section class="panel"><h2>자주 하는 실수</h2><ul class="error-list">${r.commonErrors.map(x=>`<li>${esc(x)}</li>`).join('')}</ul></section></div><aside><section class="panel"><h3>관련 개념</h3>${tags(r.relatedTerms)}<p><a href="#/glossary">용어집에서 설명 보기 →</a></p></section><section class="panel"><h3>관련 API·소스</h3><div class="relations">${related.length?related.map(linkNode).join(''):'<span class="source-path">검색 결과가 없습니다.</span>'}</div></section></aside></div>`;
  }
  function findRelated(names) { const lower=names.map(x=>x.toLowerCase()); return state.graph.nodes.filter(n=>lower.some(x=>n.name.toLowerCase().includes(x))).slice(0,18); }

  function renderApiList() {
    app.innerHTML=pageTitle('API REFERENCE','API 계약 탐색','클래스와 메서드의 실제 시그니처, 매개변수, 반환값, 예외, 샘플·테스트·소스 관계를 검색합니다.')+`<div class="toolbar"><label class="search"><span class="skip">API 검색</span><input id="apiSearch" type="search" placeholder="예: DreamineAppBuilder, ConnectAsync, ViewModel…" value="${esc(state.query)}" autocomplete="off"></label><select id="kindFilter" aria-label="API 종류"><option value="all">전체</option><option value="class">클래스</option><option value="method">메서드</option></select></div><div id="apiResults"></div>`;
    const input=document.querySelector('#apiSearch'), filter=document.querySelector('#kindFilter'); filter.value=state.kind;
    let timer; input.addEventListener('input',e=>{clearTimeout(timer);timer=setTimeout(()=>{state.query=e.target.value;state.page=1;paintApiResults();},120)}); filter.addEventListener('change',e=>{state.kind=e.target.value;state.page=1;paintApiResults()}); paintApiResults(); input.focus({preventScroll:true});
  }
  function paintApiResults(){
    const host=document.querySelector('#apiResults'); if(!host)return; const q=state.query.trim().toLowerCase(); const words=q.split(/\s+/).filter(Boolean);
    const groupMatches=x=>state.kind==='all'||(state.kind==='method'?['method','constructor'].includes(x.kind):!['method','constructor'].includes(x.kind));
    const rows=state.apiIndex.filter(x=>groupMatches(x)&&words.every(w=>`${x.name} ${x.signature||''} ${x.summary||''} ${x.project||''} ${(x.tags||[]).join(' ')}`.toLowerCase().includes(w)));
    const per=60,max=Math.max(1,Math.ceil(rows.length/per));state.page=Math.min(state.page,max);const shown=rows.slice((state.page-1)*per,state.page*per);
    host.innerHTML=`<div class="result-count">${fmt(rows.length)}개 중 ${shown.length?fmt((state.page-1)*per+1):0}–${fmt(Math.min(state.page*per,rows.length))}</div><div class="api-list">${shown.map(apiRow).join('')}</div>${max>1?`<div class="pager"><button class="button" id="prevPage" ${state.page===1?'disabled':''}>← 이전</button><span class="button">${state.page} / ${max}</span><button class="button" id="nextPage" ${state.page===max?'disabled':''}>다음 →</button></div>`:''}`;
    document.querySelector('#prevPage')?.addEventListener('click',()=>{state.page--;paintApiResults();window.scrollTo(0,250)});document.querySelector('#nextPage')?.addEventListener('click',()=>{state.page++;paintApiResults();window.scrollTo(0,250)});
  }
  function apiRow(x){return `<a class="api-row" href="${routeUrl('api',x.id)}"><span class="api-kind">${esc(x.kind)}</span><span class="api-name"><strong>${esc(x.name)}</strong><small>${esc(x.signature||x.summary||'설명 없음')}</small></span><span class="api-project">${esc(x.project||'')}</span></a>`}
  function renderApiDetail(id){
    const n=state.nodeById.get(id);if(!n)return renderNotFound();const m=n.apiMeta||{};const edges=state.edgesByNode.get(id)||[];const related=edges.map(e=>state.nodeById.get(e.source===id?e.target:e.source)).filter(Boolean).slice(0,80);
    const params=m.parameters||[], exceptions=m.exceptions||[], trust=m.trust||{}, project=m.project||{};
    app.innerHTML=pageTitle(`${m.kind||n.type} · ${project.project||n.type}`,n.name,n.summary||'아직 요약 설명이 없습니다.')+`<div class="detail-grid"><div><section class="panel"><div class="source-head"><h2>선언</h2><span class="trust">${esc(trustText(trust.signature))}</span></div><div class="signature">${esc(m.signature||n.name)}</div>${m.remarks?`<p>${esc(m.remarks)}</p>`:''}</section>${params.length?`<section class="panel"><div class="source-head"><h2>매개변수</h2><span class="trust">${esc(trustText(trust.parameters))}</span></div><div class="table-wrap"><table><thead><tr><th>이름</th><th>형식</th><th>설명 / 기본값</th></tr></thead><tbody>${params.map(p=>`<tr><td><code>${esc(p.name)}</code></td><td><code>${esc([...(p.modifiers||[]),p.type].join(' '))}</code></td><td>${esc(p.description||'설명 없음')}${p.defaultValue?`<br><small>기본값: <code>${esc(p.defaultValue)}</code></small>`:''}</td></tr>`).join('')}</tbody></table></div></section>`:''}${m.returns?`<section class="panel"><div class="source-head"><h2>반환값</h2><span class="trust">${esc(trustText(trust.returns))}</span></div><p><code>${esc(m.returns.type)}</code> — ${esc(m.returns.description|| (m.returns.type==='void'?'반환값이 없습니다.':'설명 없음'))}</p></section>`:''}${exceptions.length?`<section class="panel"><div class="source-head"><h2>예외</h2><span class="trust">${esc(trustText(trust.exceptions))}</span></div><div class="table-wrap"><table><thead><tr><th>형식</th><th>발생 조건</th></tr></thead><tbody>${exceptions.map(x=>`<tr><td><code>${esc(x.type)}</code></td><td>${esc(x.description||x.condition||'코드에서 발생')}</td></tr>`).join('')}</tbody></table></div></section>`:''}${m.sideEffects?.length?`<section class="panel"><h2>부수 효과</h2><ul>${m.sideEffects.map(x=>`<li>${esc(x)}</li>`).join('')}</ul></section>`:''}${m.example?`<section class="panel"><h2>예제</h2>${codeBlock(m.example,'csharp')}</section>`:''}<section class="panel"><div class="source-head"><h2>소스 코드</h2><button class="button" id="loadSource">불러오기</button></div><p class="source-path">${esc(n.filePath||'파일 경로 없음')}${n.lineRange?` · ${n.lineRange[0]}–${n.lineRange[1]}행`:''}</p><div id="sourceBody"></div></section><section class="panel"><h2>관계 (${edges.length})</h2><div class="relations">${related.length?related.map(linkNode).join(''):'연결된 노드가 없습니다.'}</div></section></div><aside><section class="panel"><h3>API 정보</h3><dl class="meta-list"><div><dt>네임스페이스</dt><dd>${esc(m.namespace||'-')}</dd></div><div><dt>접근성</dt><dd>${esc(m.accessibility||'-')}</dd></div><div><dt>프로젝트</dt><dd>${esc(project.project||'-')}</dd></div><div><dt>패키지</dt><dd>${esc(project.packageId||'-')}</dd></div><div><dt>대상 프레임워크</dt><dd>${esc((project.targetFrameworks||[]).join(', ')||'-')}</dd></div><div><dt>취소 지원</dt><dd>${m.cancellationSupported?'CancellationToken 지원':'명시적 지원 없음'}</dd></div><div><dt>플랫폼</dt><dd>${project.windowsOnly?'Windows 전용 또는 의존':'플랫폼 중립/미확인'}</dd></div></dl>${tags(n.tags||[])}</section>${m.relatedSamples?.length?`<section class="panel"><h3>관련 샘플</h3>${m.relatedSamples.map(x=>`<p class="source-path">${esc(x)}</p>`).join('')}</section>`:''}${m.relatedTests?.length?`<section class="panel"><h3>관련 테스트</h3>${m.relatedTests.map(x=>`<p class="source-path">${esc(x)}</p>`).join('')}</section>`:''}<section class="panel"><h3>설명 근거</h3><div class="meta-list">${Object.entries(trust).map(([k,v])=>`<div><dt>${esc(k)}</dt><dd><span class="trust">${esc(trustText(v))}</span></dd></div>`).join('')}</div></section></aside></div>`;
    document.querySelector('#loadSource')?.addEventListener('click',()=>loadSource(n));
  }
  async function loadSource(n){const host=document.querySelector('#sourceBody'),btn=document.querySelector('#loadSource');btn.disabled=true;btn.textContent='불러오는 중…';try{const data=await fetch(sourceUrl(n.filePath)).then(check).then(r=>r.json());let lines=data.content.split(/\r?\n/),start=0;if(n.lineRange){start=Math.max(0,n.lineRange[0]-8);const end=Math.min(lines.length,n.lineRange[1]+7);lines=lines.slice(start,end)}const source=lines.join('\n'),numbers=lines.map((_,i)=>start+i+1).join('\n');host.innerHTML=`<div class="source-code-shell"><div class="source-code-toolbar"><span>줄 번호는 복사되지 않습니다.</span><button type="button" class="copy-code">코드 복사</button></div><div class="source-code-grid"><pre class="source-lines" aria-hidden="true">${esc(numbers)}</pre><pre class="source-code"><code>${esc(source)}</code></pre></div></div>`;btn.textContent='불러옴'}catch(e){host.innerHTML='<div class="notice">이 파일은 비밀·설정 파일 보안 정책 또는 지원 형식 때문에 공개 미리보기에서 제외되었습니다.</div>';btn.textContent='미리보기 제외'}}
  function linkNode(n){const raw=n.name||'이름 없음',parts=raw.split('_'),owner=parts.length>1?parts.shift():'';let readable=parts.length?parts.join('_'):raw;readable=readable.replace(/_/g,' ').replace(/([a-z0-9])([A-Z])/g,'$1 $2');const kind=({function:'메서드',class:'클래스',file:'파일',test:'테스트',component:'컴포넌트'}[n.type]||n.apiMeta?.kind||n.type||'코드').toUpperCase();const context=owner||n.apiMeta?.project?.project||n.filePath?.split('/').slice(-2,-1)[0]||'';return `<a class="relation" href="${n.apiMeta?routeUrl('api',n.id):`./graph/#/node/${encodeURIComponent(n.id)}`}" ${n.apiMeta?'':'target="_blank" rel="noopener"'} title="${esc(raw)}"><span class="relation-kind">${esc(kind)}</span><strong>${esc(readable)}</strong>${context?`<small>${esc(context)}</small>`:''}</a>`}

  function renderArchitecture(){const g=state.graph;app.innerHTML=pageTitle('ARCHITECTURE','솔루션 구조','레이어별 책임을 먼저 읽고, 상세 노드와 교차 관계는 전체 그래프에서 탐색하세요.')+`<section><div class="cards">${g.layers.map((l,i)=>`<article class="card"><span class="number">LAYER ${String(i+1).padStart(2,'0')}</span><h3>${esc(l.name)}</h3><p>${esc(l.description)}</p>${tags([`${fmt(l.nodeIds.length)} nodes`])}<div class="cta-row"><a class="button" href="./graph/" target="_blank" rel="noopener">그래프에서 보기 ↗</a></div></article>`).join('')}</div></section><section><div class="notice">구조 화면은 “무슨 책임이 어디에 있는가”를 설명합니다. 호출 방향, 상속, 구현, 테스트, 샘플까지 직접 탐색하려면 전체 그래프를 여세요.</div></section>`}
  function renderGlossary(){app.innerHTML=pageTitle('GLOSSARY','초보자 용어집','문서와 코드에서 자주 보이는 개념을 FullKit의 사용 맥락으로 설명합니다.')+`<section class="glossary">${state.content.glossary.map(x=>`<article><h3>${esc(x.term)}</h3><p>${esc(x.description)}</p></article>`).join('')}</section>`}
  function renderTroubleshooting(){app.innerHTML=pageTitle('TROUBLESHOOTING','문제 해결','증상과 가까운 항목에서 시작해 위에서 아래로 확인하세요.')+`<section>${state.content.troubleshooting.map((x,i)=>`<article class="panel"><span class="number">CHECK ${String(i+1).padStart(2,'0')}</span><h2>${esc(x.symptom)}</h2><ol>${x.checks.map(c=>`<li>${esc(c)}</li>`).join('')}</ol>${x.related?.length?`<div class="relations">${findRelated(x.related).slice(0,8).map(linkNode).join('')}</div>`:''}</article>`).join('')}</section><section><div class="section-head"><div><h2>플랫폼 호환성</h2><p>모듈 선택 전에 적용 범위를 확인하세요.</p></div></div><div class="panel table-wrap"><table><thead><tr><th>영역</th><th>WPF</th><th>WinForms</th><th>Blazor</th><th>MAUI</th><th>비고</th></tr></thead><tbody>${state.content.compatibility.map(x=>`<tr><td>${esc(x.area)}</td><td>${esc(x.wpf)}</td><td>${esc(x.winforms)}</td><td>${esc(x.blazor)}</td><td>${esc(x.maui)}</td><td>${esc(x.notes)}</td></tr>`).join('')}</tbody></table></div></section>`}
  function renderNotFound(){app.innerHTML=pageTitle('404','찾을 수 없는 항목','주소가 바뀌었거나 현재 그래프에 포함되지 않은 항목입니다.')+'<p><a class="button primary" href="#/start">시작 화면으로</a></p>'}
  boot();
})();
