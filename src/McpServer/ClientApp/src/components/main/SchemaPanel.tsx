import { useGenerationStore } from '../../store/generationStore'
import { ResizeHandle } from '../shared/ResizeHandle'
import { useResize } from '../../hooks/useResize'

const KIND_COLORS: Record<string, string> = {
  container: '#30363d',
  void: '#30363d',
  varint: '#1f4c2e',
  varlong: '#1f4c2e',
  bool: '#1f4c2e',
  string: '#1f4c2e',
  uuid: '#1f4c2e',
  array: '#1a3a5e',
  topBitSetTerminatedArray: '#1a3a5e',
  switch: '#4a2060',
  cus_switch: '#4a2060',
  mapper: '#4a2060',
  bitfield: '#5a3010',
  bitflags: '#5a3010',
  option: '#3a3010',
  loop: '#3a3010',
  buffer: '#1a3a5e',
  registryEntryHolder: '#5a1a1a',
  registryEntryHolderSet: '#5a1a1a',
  nbt: '#2a4a2a',
}

function kindColor(kind: string): string {
  return KIND_COLORS[kind] ?? '#1a3a5e'
}

export function SchemaPanel() {
  const schema = useGenerationStore(s => s.schema)
  const { size: colsHeight, isDragging, onMouseDown } = useResize({
    direction: 'row',
    min: 60,
    max: 700,
    initial: 220,
  })

  if (!schema.visible) return null

  const renderHeader = () => {
    if (schema.loading)
      return <span style={{ color: '#484f58' }}>Loading <b style={{ color: '#8b949e' }}>{schema.loadedFor}</b>...</span>
    if (schema.error)
      return <span style={{ color: '#f85149' }}>{schema.error}</span>
    if (!schema.data) return null
    const { data } = schema
    return (
      <>
        <b style={{ color: '#c9d1d9' }}>{schema.loadedFor}</b>
        {' '}
        <span className={`ctier ${data.tier}`}>{data.tier.toUpperCase()}</span>
        {' '}
        <span style={{ color: '#484f58', fontSize: 10 }}>
          score: <b style={{ color: '#8b949e' }}>{data.complexityScore}</b>
        </span>
        {schema.composition && schema.composition.length > 0 && (
          <span style={{ marginLeft: 10, display: 'inline-flex', flexWrap: 'wrap', gap: 3 }}>
            {schema.composition.map(kind => (
              <span
                key={kind}
                title={kind}
                style={{
                  fontSize: 10,
                  padding: '1px 5px',
                  borderRadius: 3,
                  background: kindColor(kind),
                  color: '#c9d1d9',
                  border: '1px solid #30363d',
                  fontFamily: 'monospace',
                }}
              >
                {kind}
              </span>
            ))}
          </span>
        )}
      </>
    )
  }

  return (
    <div className="schema-panel">
      <div className="schema-header">{renderHeader()}</div>
      <div className="schema-cols" style={{ height: colsHeight }}>
        <div className="schema-col">
          <div className="schema-col-hdr">JSON</div>
          <pre style={{ margin: 0, padding: 10, fontSize: 11, background: 'transparent', borderRadius: 0, minHeight: 0 }}>
            {schema.data?.json ?? ''}
          </pre>
        </div>
        <div className="schema-col">
          <div className="schema-col-hdr">Toon</div>
          <pre style={{ margin: 0, padding: 10, fontSize: 11, background: 'transparent', borderRadius: 0, minHeight: 0 }}>
            {schema.data?.toon ?? ''}
          </pre>
        </div>
      </div>
      <ResizeHandle direction="row" isDragging={isDragging} onMouseDown={onMouseDown} />
    </div>
  )
}
