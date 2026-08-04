import { useEffect, useState } from 'react'
import { usePacketsStore } from '../../store/packetsStore'
import { useGenerationStore } from '../../store/generationStore'
import { useUIStore } from '../../store/uiStore'

export function Toolbar() {
  const selectedId = usePacketsStore(s => s.selectedId)
  const selectPacket = usePacketsStore(s => s.selectPacket)
  const generate = useGenerationStore(s => s.generate)
  const buildPrompt = useGenerationStore(s => s.buildPrompt)
  const assess = useGenerationStore(s => s.assess)
  const toggleSchema = useGenerationStore(s => s.toggleSchema)
  const cancel = useGenerationStore(s => s.cancel)
  const clearOutput = useGenerationStore(s => s.clearOutput)
  const isGenerating = useGenerationStore(s => s.isGenerating)
  const isAssessing = useGenerationStore(s => s.isAssessing)
  const setSelectedOwner = useUIStore(s => s.setSelectedOwner)

  const [packetId, setPacketId] = useState('')
  useEffect(() => { setPacketId(selectedId) }, [selectedId])

  const handleChange = (v: string) => {
    setPacketId(v)
    selectPacket(v)
    setSelectedOwner(v ? { kind: 'packet', id: v } : null)
  }

  return (
    <div className="toolbar">
      <div className="toolbar-field">
        <label>Packet id</label>
        <input
          className="toolbar-id"
          type="text"
          value={packetId}
          placeholder="play.toClient.entity_metadata"
          onChange={e => handleChange(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter') generate(packetId, false) }}
        />
      </div>
      <button className="btn-primary" disabled={isGenerating} onClick={() => generate(packetId, false)}>
        {isGenerating ? <span className="spinner" /> : 'Generate'}
      </button>
      <button
        className="btn-blue"
        disabled={isGenerating}
        title="Generate and save to output directory"
        onClick={() => generate(packetId, true)}
      >
        Save
      </button>
      {isGenerating && (
        <button className="btn-danger" onClick={cancel}>
          Cancel
        </button>
      )}
      <button className="btn-blue" disabled={isGenerating} onClick={() => buildPrompt(packetId)}>
        Prompt
      </button>
      <button className="btn-ghost" disabled={isAssessing} onClick={() => assess(packetId)}>
        Assess
      </button>
      <button className="btn-ghost" onClick={() => toggleSchema(packetId)}>
        Schema
      </button>
      <button className="btn-ghost" onClick={clearOutput}>
        Clear
      </button>
    </div>
  )
}
