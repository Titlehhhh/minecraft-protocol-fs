import { useMemo } from 'react'
import hljs from 'highlight.js/lib/core'
import csharp from 'highlight.js/lib/languages/csharp'

hljs.registerLanguage('csharp', csharp)

interface Props {
  code: string
  language?: string
}

export function CodeHighlight({ code, language = 'csharp' }: Props) {
  const html = useMemo(() => {
    try {
      return hljs.highlight(code, { language }).value
    } catch {
      return hljs.highlight(code, { language: 'plaintext' }).value
    }
  }, [code, language])

  return (
    <pre>
      <code
        className={`hljs language-${language}`}
        dangerouslySetInnerHTML={{ __html: html }}
      />
    </pre>
  )
}
