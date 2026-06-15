using System;
using McpServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PacketGenerator.Protocol.Graph;
using PacketGenerator.Protocol.Repository;

namespace McpServer.Endpoints;

public static class GraphEndpoints
{
    public static void MapGraphApi(this WebApplication app)
    {
        app.MapGet("/api/graph", (
            IProtocolRepository repo,
            ModelConfigService modelConfig,
            string? ns,
            string? direction,
            bool? includeTypes) =>
        {
            try
            {
                var graph = new ProtocolGraphBuilder(repo, modelConfig.GetComplexityThresholds())
                    .Build(ns, direction, includeTypes ?? true);
                return Results.Ok(graph);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"{ex.GetType().Name}: {ex.Message}" });
            }
        });

        app.MapGet("/graph", () => Results.Content(GraphPage.Html, "text/html"));
    }
}

file static class GraphPage
{
    public const string Html = """
<!doctype html>
<html lang="ru">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>McProtoNet protocol graph</title>
  <script src="https://unpkg.com/cytoscape@3.30.4/dist/cytoscape.min.js"></script>
  <style>
    :root { color-scheme: dark; --bg:#0b1020; --panel:#121a33; --text:#e8eeff; --muted:#91a0c7; --line:#26314f; }
    body { margin:0; font-family: Inter, Segoe UI, system-ui, sans-serif; background: radial-gradient(circle at top left,#1b2b5f,#0b1020 42%); color:var(--text); }
    header { height:72px; display:flex; align-items:center; gap:18px; padding:0 22px; border-bottom:1px solid var(--line); background:rgba(11,16,32,.84); backdrop-filter: blur(10px); }
    h1 { font-size:20px; margin:0; letter-spacing:.3px; }
    .hint { color:var(--muted); font-size:13px; }
    .controls { margin-left:auto; display:flex; gap:10px; align-items:center; }
    input, select, button { background:#0f1730; color:var(--text); border:1px solid #31406b; border-radius:10px; padding:9px 11px; }
    button { cursor:pointer; background:#3656d4; border-color:#5874f0; font-weight:700; }
    main { display:grid; grid-template-columns: 1fr 340px; height:calc(100vh - 73px); }
    #cy { min-width:0; height:100%; }
    aside { border-left:1px solid var(--line); background:rgba(18,26,51,.9); padding:16px; overflow:auto; }
    .card { border:1px solid var(--line); border-radius:14px; padding:12px; margin-bottom:12px; background:rgba(10,15,31,.58); }
    .stat { display:grid; grid-template-columns:1fr auto; gap:6px; color:var(--muted); font-size:13px; }
    .stat b { color:var(--text); }
    code { color:#9ee7ff; word-break:break-all; }
    .pill { display:inline-block; padding:3px 8px; margin:2px; border-radius:999px; background:#26365f; color:#cfe0ff; font-size:12px; }
  </style>
</head>
<body>
<header>
  <div>
    <h1>Граф minecraft-data / PacketGenerator</h1>
    <div class="hint">Сервер строит nodes/edges; клиент только рисует и фильтрует</div>
  </div>
  <div class="controls">
    <select id="ns"><option value="play">play</option><option value="configuration">configuration</option><option value="login">login</option><option value="status">status</option><option value="handshaking">handshaking</option><option value="">all</option></select>
    <select id="direction"><option value="toClient">toClient</option><option value="toServer">toServer</option><option value="">both</option></select>
    <input id="q" placeholder="поиск: Slot, window_click..." />
    <button id="reload">Обновить</button>
  </div>
</header>
<main>
  <div id="cy"></div>
  <aside>
    <div class="card" id="summary">Загрузка…</div>
    <div class="card"><b>Top named types</b><div id="topTypes"></div></div>
    <div class="card"><b>Выбранный узел</b><div id="selected" class="hint">Кликни по узлу графа</div></div>
  </aside>
</main>
<script>
let cy;
const colors = { packet:'#4ea1ff', namedType:'#54e39b', nativeType:'#8ea0c8', shape:'#ffb454' };
async function loadGraph(){
  const ns = document.getElementById('ns').value;
  const direction = document.getElementById('direction').value;
  const url = `/api/graph?includeTypes=true${ns?`&ns=${encodeURIComponent(ns)}`:''}${direction?`&direction=${encodeURIComponent(direction)}`:''}`;
  const graph = await fetch(url).then(r=>r.json());
  const elements = [
    ...graph.nodes.map(n => ({ data: { ...n, weight: Math.max(10, Math.min(70, 10 + (n.reuseCount||0)*3 + (n.complexityScore||0)/7)) }})),
    ...graph.edges.map(e => ({ data: { ...e, source: e.from, target: e.to } }))
  ];
  cy = cytoscape({
    container: document.getElementById('cy'), elements,
    style: [
      { selector:'node', style:{ 'background-color': e=>colors[e.data('kind')]||'#fff', 'label':'data(label)', 'color':'#e8eeff', 'font-size':10, 'text-outline-width':2, 'text-outline-color':'#081022', 'width':'data(weight)', 'height':'data(weight)' }},
      { selector:'node[kind="packet"]', style:{ 'shape':'round-rectangle' }},
      { selector:'node[kind="shape"]', style:{ 'shape':'diamond' }},
      { selector:'edge', style:{ 'width':1, 'line-color':'#40517d', 'target-arrow-color':'#40517d', 'target-arrow-shape':'triangle', 'curve-style':'bezier', 'opacity':0.42 }},
      { selector:'node.faded', style:{ 'opacity':0.08, 'text-opacity':0.08 }},
      { selector:'edge.faded', style:{ 'opacity':0.04 }},
      { selector:'node.highlight', style:{ 'opacity':1 }},
      { selector:'edge.highlight', style:{ 'opacity':1, 'line-color':'#fff', 'target-arrow-color':'#fff', 'width':3 }}
    ],
    layout: { name:'breadthfirst', directed:true, circle:false, animate:false, spacingFactor:1.15 }
  });
  cy.on('tap','node', ev => selectNode(ev.target));
  document.getElementById('summary').innerHTML = `<div class="stat"><span>packets</span><b>${graph.stats.packetCount}</b><span>named types</span><b>${graph.stats.namedTypeCount}</b><span>native</span><b>${graph.stats.nativeTypeCount}</b><span>shapes</span><b>${graph.stats.shapeCount}</b><span>edges</span><b>${graph.stats.edgeCount}</b></div>`;
  document.getElementById('topTypes').innerHTML = graph.stats.topNamedTypes.slice(0,18).map(x=>`<span class="pill">${x.label}: ${x.count}</span>`).join('');
}
function selectNode(node){
  cy.elements().addClass('faded');
  const neighborhood = node.closedNeighborhood();
  neighborhood.removeClass('faded').addClass('highlight');
  const d = node.data();
  document.getElementById('selected').innerHTML = `<p><b>${d.label}</b></p><p><code>${d.id}</code></p><p>kind: ${d.kind}${d.tier?`, tier: ${d.tier}`:''}${d.complexityScore?`, score: ${d.complexityScore}`:''}</p><p>reuse: ${d.reuseCount||0}</p>`;
}
document.getElementById('reload').onclick = loadGraph;
document.getElementById('q').oninput = ev => {
  const q = ev.target.value.toLowerCase();
  if (!cy) return;
  cy.nodes().forEach(n => n.style('display', !q || (n.data('label')||'').toLowerCase().includes(q) || (n.data('id')||'').toLowerCase().includes(q) ? 'element' : 'none'));
};
loadGraph().catch(e => document.getElementById('summary').textContent = e.message);
</script>
</body>
</html>
""";
}
