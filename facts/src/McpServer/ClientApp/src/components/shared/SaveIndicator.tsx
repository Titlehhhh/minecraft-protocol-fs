import { useConfigStore } from '../../store/configStore'

const LABELS: Record<string, string> = {
  idle: 'Synced',
  dirty: 'Unsaved',
  saving: 'Saving...',
  error: 'Error',
}

export function SaveIndicator() {
  const saveState = useConfigStore(s => s.saveState)
  const saveError = useConfigStore(s => s.saveError)
  const label = saveState === 'error' && saveError ? saveError : LABELS[saveState]
  return (
    <>
      <span className={`save-dot ${saveState}`} />
      <span className={`save-label ${saveState}`}>{label}</span>
    </>
  )
}
