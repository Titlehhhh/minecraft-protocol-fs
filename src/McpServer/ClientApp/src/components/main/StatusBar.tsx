import { useGenerationStore } from '../../store/generationStore'

export function StatusBar() {
  const statusHtml = useGenerationStore(s => s.statusHtml)
  return (
    <div
      className="status-bar"
      dangerouslySetInnerHTML={{ __html: statusHtml }}
    />
  )
}
