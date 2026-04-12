import { useEffect, useState } from 'react'
import { usePacketsStore } from '../../store/packetsStore'
import { useGenerationStore } from '../../store/generationStore'

export function Toolbar() {
  const selectedId = usePacketsStore(s => s.selectedId)
  const selectPacket = usePacketsStore(s => s.selectPacket)
  const { generate, buildPrompt, assess, toggleSchema, cancel, clearOutput, isGenerating, isAssessing } =
    useGenerationStore()

  // Local input state — syncs from store when packet is selected from list
  const [packetId, setPacketId] = useState('')
  useEffect(() => { setPacketId(selectedId) }, [selectedId])

  const handleChange = (v: string) => {
    setPacketId(v)
    selectPacket(v)
  }

  return (
    <div className="toolbar">
      <input
        className="toolbar-id"
        type="text"
        value={packetId}
        placeholder="Packet ID, e.g. play.toClient.entity_metadata"
        onChange={e => handleChange(e.target.value)}
        onKeyDown={e => { if (e.key === 'Enter') generate(packetId, false) }}
      />
      <button className="btn-primary" disabled={isGenerating} onClick={() => generate(packetId, false)}>
        {isGenerating ? <span className="spinner" /> : '▶ Generate'}
      </button>
      <button
        className="btn-blue"
        disabled={isGenerating}
        title="Generate and save to Output directory"
        onClick={() => generate(packetId, true)}
      >
        💾 Save
      </button>
      {isGenerating && (
        <button
          className="btn-ghost"
          style={{ color: '#f85149', borderColor: '#da363655' }}
          onClick={cancel}
        >
          ✕ Cancel
        </button>
      )}
      <button className="btn-blue" disabled={isGenerating} onClick={() => buildPrompt(packetId)}>
        📋 Prompt
      </button>
      <button className="btn-ghost" disabled={isAssessing} onClick={() => assess(packetId)}>
        🔮 Assess
      </button>
      <button className="btn-ghost" onClick={() => toggleSchema(packetId)}>
        🔍 Schema
      </button>
      <button className="btn-ghost" onClick={clearOutput}>
        Clear
      </button>
    </div>
  )
}
