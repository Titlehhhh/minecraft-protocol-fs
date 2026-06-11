import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import {
  Background,
  Controls,
  Handle,
  MarkerType,
  MiniMap,
  getNodesBounds,
  getViewportForBounds,
  Position,
  ReactFlow,
  ReactFlowProvider,
  useReactFlow,
  type Edge,
  type Node,
  type NodeProps,
} from '@xyflow/react'
import dagre from '@dagrejs/dagre'
import { toPng, toSvg } from 'html-to-image'
import { fetchProtocolGraph } from '../../api/graph'
import type { ProtocolGraphEdge, ProtocolGraphNode, ProtocolGraphResponse } from '../../types'
import '@xyflow/react/dist/style.css'

type VisualKind = ProtocolGraphNode['kind'] | 'packetCard'
type VersionMode = 'common' | 'versioned'
type GraphLayoutMode = 'hub' | 'dagre'

type PacketField = {
  id: string
  name: string
  path: string
  mode: VersionMode
  ranges: string[]
  targets: Array<{ id: string; label: string; kind: ProtocolGraphNode['kind'] }>
}

type GraphNodeData = {
  label: string
  kind: VisualKind
  tier?: string
  score?: number
  reuse: number
  ranges: string[]
  fields?: PacketField[]
  raw?: ProtocolGraphNode
}

const PACKET_WIDTH = 320
const TYPE_WIDTH = 190
const TYPE_HEIGHT = 68
const FIELD_ROW_HEIGHT = 30
const MAX_PACKET_FIELDS = 12
const MAX_OVERVIEW_EDGES = 950
const MAX_NATIVE_EDGES = 1800

const kindLabels: Record<VisualKind, string> = {
  packet: 'packet',
  packetCard: 'packet',
  namedType: 'type',
  nativeType: 'native',
  shape: 'shape',
}

function firstField(path?: string) {
  if (!path) return null
  const parts = path.split('.').filter(Boolean)
  return parts.length ? parts[0] : null
}

function fieldHandle(field: string) {
  return `field:${field.replace(/[^a-zA-Z0-9_-]/g, '_')}`
}

function packetFieldNodeId(packetId: string, field: string) {
  return `${packetId}::${fieldHandle(field)}`
}

function isPacketNode(node: Node<GraphNodeData>) {
  return node.data.kind === 'packetCard'
}

function nodeSize(node: Node<GraphNodeData>) {
  if (!isPacketNode(node)) return { width: TYPE_WIDTH, height: TYPE_HEIGHT }
  const fieldCount = node.data.fields?.length ?? 0
  return { width: PACKET_WIDTH, height: 76 + Math.max(1, fieldCount) * FIELD_ROW_HEIGHT }
}

function versionBadge(mode: VersionMode) {
  return mode === 'common' ? 'common' : 'versioned'
}

function ProtocolNode({ data, selected }: NodeProps<Node<GraphNodeData>>) {
  if (data.kind === 'packetCard') {
    const fields = data.fields ?? []
    return (
      <div className={`rf-packet-card tier-${data.tier ?? 'unknown'} ${selected ? 'selected' : ''}`}>
        <div className="rf-packet-head">
          <div>
            <span className="rf-kind">packet</span>
            <strong>{data.label}</strong>
          </div>
          {data.tier && <em>{data.tier}</em>}
        </div>
        <div className="rf-packet-meta">
          <span>score {data.score ?? 0}</span>
          <span>{data.ranges.length} version ranges</span>
        </div>
        <div className="rf-field-list">
          {fields.length === 0 && <div className="rf-field-row empty">нет полей в текущем срезе</div>}
          {fields.map(field => (
            <div key={field.id} className={`rf-field-row ${field.mode}`} title={`${field.path}\n${field.ranges.join(', ')}`}>
              <div className="rf-field-main">
                <span className="rf-field-name">{field.name}</span>
                <span className="rf-field-type">→ {field.targets.map(t => t.label).join(' | ')}</span>
                <span className="rf-field-ranges">{field.mode === 'common' ? 'all ranges' : field.ranges.join(', ')}</span>
              </div>
              <span className="rf-version-pill">{versionBadge(field.mode)}</span>
              <Handle type="source" position={Position.Right} id={field.id} className="rf-field-handle" />
            </div>
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className={`rf-type-card kind-${data.kind} ${selected ? 'selected' : ''}`}>
      <Handle type="target" position={Position.Left} className="rf-type-target" />
      <div className="rf-node-top">
        <span>{kindLabels[data.kind]}</span>
        {data.tier && <em>{data.tier}</em>}
      </div>
      <strong>{data.label}</strong>
      <div className="rf-node-meta">
        {data.score ? <span>score {data.score}</span> : null}
        {data.reuse ? <span>reuse {data.reuse}</span> : null}
      </div>
    </div>
  )
}

const nodeTypes = { protocol: ProtocolNode }

function edgePriority(edge: ProtocolGraphEdge) {
  if (edge.from.startsWith('packet:') && edge.fieldPath) return 0
  if (edge.to.startsWith('type:')) return 1
  if (edge.to.startsWith('shape:')) return 2
  if (edge.to.startsWith('native:')) return 4
  return 3
}

function getLayoutedElements(nodes: Node<GraphNodeData>[], edges: Edge[], direction: 'LR' | 'TB') {
  const dagreGraph = new dagre.graphlib.Graph().setDefaultEdgeLabel(() => ({}))
  dagreGraph.setGraph({ rankdir: direction, nodesep: 62, ranksep: 150, marginx: 40, marginy: 40 })

  for (const node of nodes) dagreGraph.setNode(node.id, nodeSize(node))
  for (const edge of edges) dagreGraph.setEdge(edge.source, edge.target)
  dagre.layout(dagreGraph)

  return nodes.map(node => {
    const size = nodeSize(node)
    const position = dagreGraph.node(node.id) as { x: number; y: number } | undefined
    return {
      ...node,
      sourcePosition: direction === 'LR' ? Position.Right : Position.Bottom,
      targetPosition: direction === 'LR' ? Position.Left : Position.Top,
      position: { x: (position?.x ?? 0) - size.width / 2, y: (position?.y ?? 0) - size.height / 2 },
    }
  })
}


function getHubLayoutedElements(nodes: Node<GraphNodeData>[], edges: Edge[], graph: ProtocolGraphResponse) {
  const topRank = new Map(graph.stats.topNamedTypes.map((item, index) => [item.id, index]))
  const nodeMap = new Map(nodes.map(node => [node.id, node]))
  const hubIds = graph.stats.topNamedTypes
    .map(item => item.id)
    .filter(id => nodeMap.has(id))
    .slice(0, 10)

  if (hubIds.length === 0) return { nodes: getLayoutedElements(nodes, edges, 'LR'), edges }

  const assignedHub = new Map<string, string>()
  for (const edge of edges) {
    if (!edge.source.startsWith('packet:') || !hubIds.includes(edge.target)) continue
    const current = assignedHub.get(edge.source)
    if (!current || (topRank.get(edge.target) ?? 999) < (topRank.get(current) ?? 999)) {
      assignedHub.set(edge.source, edge.target)
    }
  }

  const filteredEdges = edges.filter(edge => assignedHub.get(edge.source) === edge.target)
  const usedIds = new Set<string>()
  for (const edge of filteredEdges) {
    usedIds.add(edge.source)
    usedIds.add(edge.target)
  }

  const visibleNodes = nodes.filter(node => usedIds.has(node.id))
  const visibleNodeIds = new Set(visibleNodes.map(node => node.id))
  const safeEdges = filteredEdges.filter(edge => visibleNodeIds.has(edge.source) && visibleNodeIds.has(edge.target))
  const groups = new Map<string, Node<GraphNodeData>[]>()

  for (const hubId of hubIds) groups.set(hubId, [])
  for (const node of visibleNodes) {
    if (!node.id.startsWith('packet:')) continue
    const hubId = assignedHub.get(node.id)
    if (hubId) groups.get(hubId)?.push(node)
  }

  const placed = visibleNodes.map(node => ({ ...node }))
  const placedMap = new Map(placed.map(node => [node.id, node]))
  const clusterGapX = 900
  const clusterGapY = 760
  const columns = Math.min(3, Math.max(1, Math.ceil(Math.sqrt(hubIds.length))))

  hubIds.forEach((hubId, index) => {
    const hub = placedMap.get(hubId)
    if (!hub) return
    const col = index % columns
    const row = Math.floor(index / columns)
    const cx = col * clusterGapX
    const cy = row * clusterGapY
    const packets = groups.get(hubId) ?? []
    const radius = Math.max(300, 170 + packets.length * 24)

    hub.position = { x: cx - TYPE_WIDTH / 2, y: cy - TYPE_HEIGHT / 2 }
    hub.sourcePosition = Position.Right
    hub.targetPosition = Position.Left

    packets.forEach((packet, packetIndex) => {
      const size = nodeSize(packet)
      const angle = (Math.PI * 2 * packetIndex) / Math.max(1, packets.length) - Math.PI / 2
      packet.position = {
        x: cx + Math.cos(angle) * radius - size.width / 2,
        y: cy + Math.sin(angle) * radius - size.height / 2,
      }
      packet.sourcePosition = Position.Right
      packet.targetPosition = Position.Left
    })
  })

  // Cheap collision pass: packet cards are large, so separate overlapping rectangles.
  for (let pass = 0; pass < 28; pass++) {
    for (let i = 0; i < placed.length; i++) {
      for (let j = i + 1; j < placed.length; j++) {
        const a = placed[i]
        const b = placed[j]
        if (!a || !b) continue
        const as = nodeSize(a)
        const bs = nodeSize(b)
        const ax = a.position.x + as.width / 2
        const ay = a.position.y + as.height / 2
        const bx = b.position.x + bs.width / 2
        const by = b.position.y + bs.height / 2
        const minX = (as.width + bs.width) / 2 + 34
        const minY = (as.height + bs.height) / 2 + 34
        const dx = bx - ax
        const dy = by - ay
        if (Math.abs(dx) >= minX || Math.abs(dy) >= minY) continue
        const pushX = (minX - Math.abs(dx)) / 2
        const pushY = (minY - Math.abs(dy)) / 2
        const sx = dx >= 0 ? 1 : -1
        const sy = dy >= 0 ? 1 : -1
        const aLocked = a.data.kind === 'namedType'
        const bLocked = b.data.kind === 'namedType'
        if (!aLocked) { a.position.x -= pushX * sx; a.position.y -= pushY * sy }
        if (!bLocked) { b.position.x += pushX * sx; b.position.y += pushY * sy }
      }
    }
  }

  return { nodes: placed, edges: safeEdges }
}

function makePacketFields(packet: ProtocolGraphNode, packetEdges: ProtocolGraphEdge[], nodeById: Map<string, ProtocolGraphNode>, showNative: boolean): PacketField[] {
  const byField = new Map<string, { ranges: Set<string>; targets: Map<string, ProtocolGraphNode>; paths: Set<string> }>()

  for (const edge of packetEdges) {
    if (!edge.fieldPath) continue
    if (!showNative && edge.to.startsWith('native:')) continue
    const name = firstField(edge.fieldPath)
    const target = nodeById.get(edge.to)
    if (!name || !target) continue
    if (!byField.has(name)) byField.set(name, { ranges: new Set(), targets: new Map(), paths: new Set() })
    const entry = byField.get(name)!
    if (edge.versionRange) entry.ranges.add(edge.versionRange)
    entry.targets.set(target.id, target)
    entry.paths.add(edge.fieldPath)
  }

  const packetRanges = new Set(packet.versionRanges)
  return Array.from(byField.entries())
    .map(([name, entry]) => {
      const ranges = Array.from(entry.ranges).sort()
      const common = packetRanges.size > 0 && packet.versionRanges.every(range => entry.ranges.has(range))
      const targets = Array.from(entry.targets.values()).map(target => ({ id: target.id, label: target.label, kind: target.kind }))
      return {
        id: fieldHandle(name),
        name,
        path: Array.from(entry.paths).sort()[0] ?? name,
        mode: common ? 'common' as const : 'versioned' as const,
        ranges,
        targets,
      }
    })
    .sort((a, b) => (a.mode === b.mode ? 0 : a.mode === 'common' ? -1 : 1) || a.name.localeCompare(b.name))
    .slice(0, MAX_PACKET_FIELDS)
}

function buildFlow(graph: ProtocolGraphResponse, showNative: boolean, query: string, direction: 'LR' | 'TB', layoutMode: GraphLayoutMode) {
  const normalized = query.trim().toLowerCase()
  const nodeById = new Map(graph.nodes.map(node => [node.id, node]))
  const maxEdges = showNative ? MAX_NATIVE_EDGES : MAX_OVERVIEW_EDGES
  const sourceEdges = graph.edges
    .filter(edge => edge.from.startsWith('packet:') && edge.fieldPath && (showNative || !edge.to.startsWith('native:')))
    .sort((a, b) => edgePriority(a) - edgePriority(b) || a.from.localeCompare(b.from) || (a.fieldPath ?? '').localeCompare(b.fieldPath ?? ''))
    .slice(0, maxEdges)

  const packetEdgeMap = new Map<string, ProtocolGraphEdge[]>()
  for (const edge of sourceEdges) {
    if (!packetEdgeMap.has(edge.from)) packetEdgeMap.set(edge.from, [])
    packetEdgeMap.get(edge.from)!.push(edge)
  }

  const visualNodes = new Map<string, Node<GraphNodeData>>()
  const visualEdges: Edge[] = []

  for (const [packetId, packetEdges] of packetEdgeMap) {
    const packet = nodeById.get(packetId)
    if (!packet) continue
    const fields = makePacketFields(packet, packetEdges, nodeById, showNative)
    if (fields.length === 0) continue

    visualNodes.set(packetId, {
      id: packetId,
      type: 'protocol',
      position: { x: 0, y: 0 },
      data: {
        label: packet.label,
        kind: 'packetCard',
        tier: packet.tier,
        score: packet.complexityScore,
        reuse: packet.reuseCount,
        ranges: packet.versionRanges,
        fields,
        raw: packet,
      },
    })
  }

  function addTypeNode(id: string) {
    const node = nodeById.get(id)
    if (!node || visualNodes.has(id)) return
    visualNodes.set(id, {
      id,
      type: 'protocol',
      position: { x: 0, y: 0 },
      data: { label: node.label, kind: node.kind, tier: node.tier, score: node.complexityScore, reuse: node.reuseCount, ranges: node.versionRanges, raw: node },
    })
  }

  const seenEdges = new Set<string>()
  for (const edge of sourceEdges) {
    const packetNode = visualNodes.get(edge.from)
    if (!packetNode || !edge.fieldPath) continue
    const field = firstField(edge.fieldPath)
    if (!field || !packetNode.data.fields?.some(f => f.name === field)) continue
    addTypeNode(edge.to)
    if (!visualNodes.has(edge.to)) continue
    const id = `${edge.from}:${field}->${edge.to}`
    if (seenEdges.has(id)) continue
    seenEdges.add(id)
    const fieldMode = packetNode.data.fields?.find(f => f.name === field)?.mode ?? 'versioned'
    visualEdges.push({
      id,
      source: edge.from,
      sourceHandle: fieldHandle(field),
      target: edge.to,
      type: 'straight',
      label: undefined,
      markerEnd: { type: MarkerType.ArrowClosed, width: 14, height: 14 },
      className: `rf-edge edge-${edge.kind} edge-${fieldMode}`,
    })
  }

  let nodes = Array.from(visualNodes.values())
  let edges = visualEdges

  if (normalized) {
    const matchedIds = new Set(nodes.filter(node => {
      const fields = node.data.fields?.flatMap(f => [f.name, f.path, f.ranges.join(' '), ...f.targets.map(t => t.label)]) ?? []
      const haystack = [node.id, node.data.label, node.data.ranges.join(' '), ...fields].join(' ').toLowerCase()
      return haystack.includes(normalized)
    }).map(node => node.id))

    for (const edge of edges) {
      if (matchedIds.has(edge.source) || matchedIds.has(edge.target)) {
        matchedIds.add(edge.source)
        matchedIds.add(edge.target)
      }
    }

    nodes = nodes.filter(node => matchedIds.has(node.id))
    const finalIds = new Set(nodes.map(node => node.id))
    edges = edges.filter(edge => finalIds.has(edge.source) && finalIds.has(edge.target))
  }

  if (layoutMode === 'hub') return getHubLayoutedElements(nodes, edges, graph)
  return { nodes: getLayoutedElements(nodes, edges, direction), edges }
}

function downloadBlob(name: string, type: string, content: BlobPart) {
  const blob = new Blob([content], { type })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = name
  a.click()
  URL.revokeObjectURL(url)
}

function GraphFlow() {
  const { fitView, toObject, getNodes } = useReactFlow()
  const flowWrapRef = useRef<HTMLDivElement | null>(null)
  const [ns, setNs] = useState('play')
  const [packetDirection, setPacketDirection] = useState('toClient')
  const [layoutDirection, setLayoutDirection] = useState<'LR' | 'TB'>('LR')
  const [layoutMode, setLayoutMode] = useState<GraphLayoutMode>('hub')
  const [query, setQuery] = useState('')
  const [showNative, setShowNative] = useState(false)
  const [graph, setGraph] = useState<ProtocolGraphResponse | null>(null)
  const [selected, setSelected] = useState<GraphNodeData | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)
    setSelected(null)
    fetchProtocolGraph({ ns, direction: packetDirection, includeTypes: true })
      .then(nextGraph => { if (!cancelled) setGraph(nextGraph) })
      .catch(e => { if (!cancelled) setError(e instanceof Error ? e.message : String(e)) })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [ns, packetDirection])

  const flow = useMemo(() => graph ? buildFlow(graph, showNative, query, layoutDirection, layoutMode) : { nodes: [], edges: [] }, [graph, showNative, query, layoutDirection, layoutMode])

  useEffect(() => {
    window.requestAnimationFrame(() => fitView({ padding: 0.12, duration: 250 }))
  }, [flow.nodes.length, flow.edges.length, fitView])

  const exportImage = useCallback(async (format: 'png' | 'svg') => {
    const viewport = flowWrapRef.current?.querySelector('.react-flow__viewport') as HTMLElement | null
    if (!viewport) return

    const nodes = getNodes()
    if (nodes.length === 0) return

    const bounds = getNodesBounds(nodes)
    const padding = 120
    const width = Math.max(1200, Math.ceil(bounds.width + padding * 2))
    const height = Math.max(800, Math.ceil(bounds.height + padding * 2))
    const transform = getViewportForBounds(bounds, width, height, 0.05, 2, padding)
    const style = {
      width: `${width}px`,
      height: `${height}px`,
      transform: `translate(${transform.x}px, ${transform.y}px) scale(${transform.zoom})`,
    }

    const file = `mcprotonet-graph-${ns || 'all'}-${packetDirection || 'both'}.${format}`
    const options = { backgroundColor: '#0d1117', width, height, style, cacheBust: true }
    const dataUrl = format === 'png'
      ? await toPng(viewport, { ...options, pixelRatio: 2 })
      : await toSvg(viewport, options)
    const a = document.createElement('a')
    a.href = dataUrl
    a.download = file
    a.click()
  }, [getNodes, ns, packetDirection])

  const exportJson = useCallback(() => {
    downloadBlob(`mcprotonet-graph-${ns || 'all'}-${packetDirection || 'both'}.json`, 'application/json', JSON.stringify({ graph, flow: toObject() }, null, 2))
  }, [graph, ns, packetDirection, toObject])

  const onNodeClick = useCallback((_: unknown, node: Node<GraphNodeData>) => setSelected(node.data), [])
  const summary = graph ? [['packets', graph.stats.packetCount], ['types', graph.stats.namedTypeCount], ['native', graph.stats.nativeTypeCount], ['shapes', graph.stats.shapeCount], ['edges', graph.stats.edgeCount], ['shown', flow.edges.length]] : null

  return (
    <section className="graph-panel rf-panel">
      <div className="graph-toolbar rf-toolbar">
        <div className="graph-title-block"><h2>Граф протокола Minecraft</h2><div className="graph-subtitle">Частые типы — центры кластеров; пакеты отталкиваются вокруг них.</div></div>
        <select value={ns} onChange={e => setNs(e.target.value)}><option value="play">play</option><option value="configuration">configuration</option><option value="login">login</option><option value="status">status</option><option value="handshaking">handshaking</option><option value="">all</option></select>
        <select value={packetDirection} onChange={e => setPacketDirection(e.target.value)}><option value="toClient">toClient</option><option value="toServer">toServer</option><option value="">both</option></select>
        <select value={layoutMode} onChange={e => setLayoutMode(e.target.value as GraphLayoutMode)}><option value="hub">кластеры типов</option><option value="dagre">прямой граф</option></select>
        <select value={layoutDirection} onChange={e => setLayoutDirection(e.target.value as 'LR' | 'TB')}><option value="LR">слева направо</option><option value="TB">сверху вниз</option></select>
        <input className="graph-search" value={query} onChange={e => setQuery(e.target.value)} placeholder="поиск: Slot, entityId, 770-772..." />
        <label className="graph-check"><input type="checkbox" checked={showNative} onChange={e => setShowNative(e.target.checked)} /> native</label>
        <button className="graph-action" type="button" onClick={() => exportImage('png')}>PNG</button>
        <button className="graph-action" type="button" onClick={() => exportImage('svg')}>SVG</button>
        <button className="graph-action" type="button" onClick={exportJson}>JSON</button>
        {loading && <span className="graph-loading"><span className="spinner" /> строю</span>}
      </div>

      <div className="graph-body">
        <div ref={flowWrapRef} className="graph-canvas-wrap rf-canvas-wrap">
          {error && <div className="graph-error">{error}</div>}
          <ReactFlow nodes={flow.nodes} edges={flow.edges} nodeTypes={nodeTypes} onNodeClick={onNodeClick} nodesDraggable nodesConnectable={false} fitView minZoom={0.035} maxZoom={2} colorMode="dark" proOptions={{ hideAttribution: true }}>
            <Background color="#263244" gap={22} />
            <Controls position="bottom-left" />
            <MiniMap position="bottom-right" pannable zoomable maskColor="rgba(13, 17, 23, 0.72)" nodeColor={node => {
              const kind = (node.data as GraphNodeData | undefined)?.kind
              if (kind === 'packetCard') return '#58a6ff'
              if (kind === 'namedType') return '#3fb950'
              if (kind === 'shape') return '#d29922'
              return '#8b949e'
            }} />
          </ReactFlow>
        </div>
        <aside className="graph-side">
          <div className="graph-card"><h2>Легенда</h2><div className="graph-legend"><span className="lg packet">packet</span><span className="lg field common">common field</span><span className="lg field versioned">versioned field</span><span className="lg type">type</span><span className="lg shape">shape</span><span className="lg native">native</span></div></div>
          <div className="graph-card"><h2>Сводка</h2>{summary ? <><div className="graph-stats">{summary.map(([label, value]) => <div key={label} className="graph-stat"><span>{label}</span><b>{value}</b></div>)}</div><div className="graph-muted graph-note">В режиме кластеров каждый пакет привязан к одному частому типу: меньше паутины, лучше для объяснения.</div></> : <div className="graph-muted">Загрузка…</div>}</div>
          <div className="graph-card"><h2>Частые типы</h2><div className="graph-pills">{graph?.stats.topNamedTypes.slice(0, 16).map(item => <button key={item.id} className="graph-pill" type="button" onClick={() => setQuery(item.label)}>{item.label}<span>{item.count}</span></button>)}</div></div>
          <div className="graph-card"><h2>Выбранное</h2>{selected ? <div className="graph-selected"><b>{selected.label}</b>{selected.raw && <code>{selected.raw.id}</code>}<p>{selected.kind}{selected.tier ? ` · tier: ${selected.tier}` : ''}{selected.score ? ` · score: ${selected.score}` : ''}{selected.reuse ? ` · reuse: ${selected.reuse}` : ''}</p>{selected.ranges.length > 0 && <p>версии: {selected.ranges.slice(0, 10).join(', ')}</p>}</div> : <div className="graph-muted">Кликни по пакету или типу.</div>}</div>
        </aside>
      </div>
    </section>
  )
}

export function GraphPanel() {
  return <ReactFlowProvider><GraphFlow /></ReactFlowProvider>
}
