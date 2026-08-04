import { useMemo } from 'react'
import hljs from 'highlight.js/lib/core'
import json from 'highlight.js/lib/languages/json'
import 'highlight.js/styles/github-dark-dimmed.css'
import { useGenerationStore } from '../../store/generationStore'
import { ResizeHandle } from '../shared/ResizeHandle'
import { useResize } from '../../hooks/useResize'

hljs.registerLanguage('json', json)

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

interface HighlightedCodeProps {
  code: string
  language?: 'json' | 'plaintext'
}

function HighlightedCode({ code, language = 'plaintext' }: HighlightedCodeProps) {
  const highlighted = useMemo(() => {
    if (!code) return ''
    try {
      if (language === 'json') {
        return hljs.highlight(code, { language: 'json', ignoreIllegals: true }).value
      }
      return code
    } catch {
      return code
    }
  }, [code, language])

  return (
    <pre
      style={{ margin: 0, padding: 10, fontSize: 11, background: 'transparent', borderRadius: 0, minHeight: 0 }}
      dangerouslySetInnerHTML={{ __html: highlighted }}
    />
  )
}

export function SchemaPanel() {
  const schema = useGenerationStore(s => s.schema)
  const typeSchema = useGenerationStore(s => s.typeSchema)
  const { size: colsHeight, isDragging, onMouseDown } = useResize({
    direction: 'row',
    min: 60,
    max: 700,
    initial: 220,
  })

  // Show typeSchema if it's visible, otherwise show schema
  const displaySchema = typeSchema.visible ? typeSchema : schema
  
  if (!displaySchema.visible) return null

  const renderHeader = () => {
    if (displaySchema.loading)
      return <span style={{ color: '#484f58' }}>Loading <b style={{ color: '#8b949e' }}>{displaySchema.loadedFor}</b>...</span>
    if (displaySchema.error)
      return <span style={{ color: '#f85149' }}>{displaySchema.error}</span>
    if (!displaySchema.data) return null
    const { data } = displaySchema
    return (
      <>
        <b style={{ color: '#c9d1d9' }}>{displaySchema.loadedFor}</b>
        {' '}
        <span className={`ctier ${data.tier}`}>{data.tier.toUpperCase()}</span>
        {' '}
        <span style={{ color: '#484f58', fontSize: 10 }}>
          score: <b style={{ color: '#8b949e' }}>{data.complexityScore}</b>
        </span>
        {displaySchema.composition && displaySchema.composition.length > 0 && (
          <span style={{ marginLeft: 10, display: 'inline-flex', flexWrap: 'wrap', gap: 3 }}>
            {displaySchema.composition.map(kind => (
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
          <HighlightedCode code={displaySchema.data?.json ?? ''} language="json" />
        </div>
        <div className="schema-col">
          <div className="schema-col-hdr">Toon</div>
          <pre style={{ margin: 0, padding: 10, fontSize: 11, background: 'transparent', borderRadius: 0, minHeight: 0 }}>
            {displaySchema.data?.toon ?? ''}
          </pre>
        </div>
      </div>
      <ResizeHandle direction="row" isDragging={isDragging} onMouseDown={onMouseDown} />
    </div>
  )
}
