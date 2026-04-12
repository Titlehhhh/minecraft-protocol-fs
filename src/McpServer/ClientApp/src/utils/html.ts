export function escHtml(s: unknown): string {
  return String(s)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
}

export function si(label: string, value: unknown): string {
  return `<span><span style="color:#484f58">${label}:</span> <b style="color:#c9d1d9">${value}</b></span>`
}

export function buildUsageHtml(data: {
  systemTokenCount?: number
  userTokenCount?: number
  cachedTokens?: number
  inputTokens?: number
  tokenCount?: number
  outputTokens?: number
  reasoningTokens?: number
  totalTokens?: number
}): string {
  const sep = `<span style="color:#30363d;margin:0 4px">│</span>`
  const systemTok = data.systemTokenCount ?? 0
  const userTok = data.userTokenCount ?? 0
  const cached = data.cachedTokens ?? 0
  const cacheRatio =
    data.inputTokens && data.inputTokens > 0
      ? Math.round((cached / data.inputTokens) * 100)
      : 0
  const cachedStr =
    cached > 0
      ? ` <span><span style="color:#484f58">Cached:</span> <b style="color:#3fb950">${cached}</b><span style="color:#484f58;font-size:10px">(${cacheRatio}%)</span></span>`
      : ''
  const promptParts =
    systemTok > 0
      ? `${sep}${si('Sys', systemTok)} ${si('Packet', userTok)}${cachedStr}`
      : `${sep}${si('~Prompt', data.tokenCount ?? '?')}${cachedStr}`
  if (data.outputTokens == null) return promptParts
  const output = data.outputTokens ?? 0
  const reasoning = data.reasoningTokens ?? 0
  const total = data.totalTokens ?? (data.inputTokens ?? 0) + output
  let out = sep + si('Total', total) + ` ${si('Out', output)}`
  if (reasoning > 0) out += ` ${si('Think', reasoning)}`
  return promptParts + out
}
