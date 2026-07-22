import { useState } from 'react'
import { searchOpenRouterModels, type OpenRouterModelKind } from '../../../api/models'
import { useConfigStore } from '../../../store/configStore'
import type { OpenRouterModelItem, Tier } from '../../../types'

type Target = 'tier' | 'assessor' | 'embedding'

const SORTS = [
  ['most-popular', 'popular'],
  ['pricing-low-to-high', 'cheap'],
  ['context-high-to-low', 'context'],
  ['newest', 'newest'],
] as const

interface Props {
  focusedTier: Tier
}

export function ModelSearchSection({ focusedTier }: Props) {
  const update = useConfigStore(s => s.update)
  const [target, setTarget] = useState<Target>('tier')
  const [query, setQuery] = useState('')
  const [sort, setSort] = useState<(typeof SORTS)[number][0]>('most-popular')
  const [models, setModels] = useState<OpenRouterModelItem[]>([])
  const [loading, setLoading] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const kind: OpenRouterModelKind = target === 'embedding' ? 'embedding' : 'chat'
  const targetLabel = target === 'tier' ? focusedTier : target

  const runSearch = async () => {
    setLoading(true)
    setError(null)
    setMessage(null)
    try {
      const result = await searchOpenRouterModels({ query, kind, sort })
      setModels(result.models)
      setMessage(`${result.models.length} models`)
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
      setModels([])
    } finally {
      setLoading(false)
    }
  }

  const apply = async (model: OpenRouterModelItem) => {
    if (target === 'tier') {
      update(prev => ({
        ...prev,
        [focusedTier]: { ...prev[focusedTier], model: model.id, endpoint: '' },
      }))
      setMessage(`selected ${model.id}`)
      return
    }

    if (target === 'assessor') {
      update(prev => ({
        ...prev,
        assessor: { ...prev.assessor, model: model.id, endpoint: '' },
      }))
      setMessage(`selected ${model.id}`)
      return
    }

    const snippet = `RAG_EMBEDDING_BASE_URL=https://openrouter.ai/api/v1\nRAG_EMBEDDING_MODEL=${model.id}`
    await navigator.clipboard?.writeText(snippet)
    setMessage(`copied ${model.id}`)
  }

  return (
    <div className="sidebar-section">
      <h2>OpenRouter Model Search</h2>
      <div className="model-search-targets">
        <button className={`effort-btn${target === 'tier' ? ' active' : ''}`} type="button" onClick={() => setTarget('tier')}>
          {focusedTier}
        </button>
        <button className={`effort-btn${target === 'assessor' ? ' active' : ''}`} type="button" onClick={() => setTarget('assessor')}>
          assessor
        </button>
        <button className={`effort-btn${target === 'embedding' ? ' active' : ''}`} type="button" onClick={() => setTarget('embedding')}>
          embedding
        </button>
      </div>

      <div className="model-search-row">
        <input
          type="text"
          value={query}
          placeholder={kind === 'embedding' ? 'embedding, qwen, openai...' : 'qwen, gpt, claude...'}
          onChange={e => setQuery(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter') void runSearch() }}
        />
        <select value={sort} onChange={e => setSort(e.target.value as typeof sort)}>
          {SORTS.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>
        <button className="btn-blue" type="button" onClick={() => void runSearch()} disabled={loading}>
          {loading ? '...' : 'Search'}
        </button>
      </div>

      <div className="model-search-meta">
        <span>{kind}</span>
        <span>{targetLabel}</span>
        {message && <span>{message}</span>}
        {error && <span className="err">{error}</span>}
      </div>

      <div className="model-results">
        {models.map(model => (
          <article className="model-result" key={model.id}>
            <div className="model-result-main">
              <strong>{model.name || model.id}</strong>
              <code>{model.id}</code>
            </div>
            <div className="model-result-meta">
              {model.contextLength && <span>{compactContext(model.contextLength)} ctx</span>}
              {model.promptPrice && <span>${pricePerMillion(model.promptPrice)}/M in</span>}
              {model.completionPrice && <span>${pricePerMillion(model.completionPrice)}/M out</span>}
            </div>
            <button className="btn-ghost" type="button" onClick={() => void apply(model)}>
              {target === 'embedding' ? 'Copy' : 'Use'}
            </button>
          </article>
        ))}
      </div>
    </div>
  )
}

function compactContext(value: number) {
  if (value >= 1_000_000) return `${Math.round(value / 1_000_000)}M`
  if (value >= 1_000) return `${Math.round(value / 1_000)}K`
  return String(value)
}

function pricePerMillion(value: string) {
  const parsed = Number(value)
  if (!Number.isFinite(parsed)) return value
  return (parsed * 1_000_000).toFixed(parsed * 1_000_000 >= 1 ? 2 : 4)
}
