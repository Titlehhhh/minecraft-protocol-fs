interface Props {
  direction: 'col' | 'row'
  isDragging: boolean
  onMouseDown: (e: React.MouseEvent) => void
}

export function ResizeHandle({ direction, isDragging, onMouseDown }: Props) {
  const cls = direction === 'col' ? 'resize-handle' : 'h-resize-handle'
  return (
    <div
      className={`${cls}${isDragging ? ' dragging' : ''}`}
      onMouseDown={onMouseDown}
    />
  )
}
