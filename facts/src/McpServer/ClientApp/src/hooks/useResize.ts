import { useState, useRef, useEffect, useCallback } from 'react'

interface ResizeOpts {
  direction: 'col' | 'row'
  min: number
  max: number
  initial: number
}

export function useResize({ direction, min, max, initial }: ResizeOpts) {
  const [size, setSize] = useState(initial)
  const [isDragging, setIsDragging] = useState(false)
  const ref = useRef({ dragging: false, startPos: 0, startSize: 0 })

  const onMouseDown = useCallback(
    (e: React.MouseEvent) => {
      const pos = direction === 'col' ? e.clientX : e.clientY
      ref.current = { dragging: true, startPos: pos, startSize: size }
      setIsDragging(true)
      document.body.style.cursor = direction === 'col' ? 'col-resize' : 'row-resize'
      document.body.style.userSelect = 'none'
      e.preventDefault()
    },
    [direction, size]
  )

  useEffect(() => {
    const onMove = (e: MouseEvent) => {
      if (!ref.current.dragging) return
      const pos = direction === 'col' ? e.clientX : e.clientY
      // col: absolute clientX as sidebar width; row: delta from start
      const newSize =
        direction === 'col'
          ? pos
          : ref.current.startSize + (pos - ref.current.startPos)
      setSize(Math.max(min, Math.min(max, newSize)))
    }
    const onUp = () => {
      if (!ref.current.dragging) return
      ref.current.dragging = false
      setIsDragging(false)
      document.body.style.cursor = ''
      document.body.style.userSelect = ''
    }
    document.addEventListener('mousemove', onMove)
    document.addEventListener('mouseup', onUp)
    return () => {
      document.removeEventListener('mousemove', onMove)
      document.removeEventListener('mouseup', onUp)
    }
  }, [direction, min, max])

  return { size, isDragging, onMouseDown }
}
