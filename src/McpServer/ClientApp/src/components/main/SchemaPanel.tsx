import { useGenerationStore } from '../../store/generationStore'
import { ResizeHandle } from '../shared/ResizeHandle'
import { useResize } from '../../hooks/useResize'

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
