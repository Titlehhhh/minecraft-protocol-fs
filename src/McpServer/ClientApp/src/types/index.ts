export type Tier = 'tiny' | 'easy' | 'medium' | 'heavy'
export type TierFilter = Tier | 'all'
export type ReasoningEffort = '' | 'low' | 'medium' | 'high' | 'xhigh'
export type SaveState = 'idle' | 'dirty' | 'saving' | 'error'
export type InputFormat = 'toon' | 'json'

export interface TierConfig {
  model: string
  reasoningEffort: ReasoningEffort
  endpoint: string
  maxConcurrency: number
}

export interface AssessorConfig {
  enabled: boolean
  model: string
  endpoint: string
  maxOutputTokens: number
  reasoningEffort: ReasoningEffort
}

export interface ModelConfig {
  tiny: TierConfig
  easy: TierConfig
  medium: TierConfig
  heavy: TierConfig
  tinyComplexityThreshold: number
  easyComplexityThreshold: number
  heavyComplexityThreshold: number
  temperature: number
  maxOutputTokens: number
  inputFormat: InputFormat
  outputBaseDir: string
  assessor: AssessorConfig
  dynamicContext: boolean
}

export interface PacketStat {
  id: string
  score: number
  tier: Tier
}

export interface StatsResponse {
  total: number
  tiers: Record<Tier, number>
  packets: PacketStat[]
}

export interface SchemaData {
  json: string
  toon: string
  complexityScore: number
  tier: Tier
}

export interface GenerateRequest {
  id: string
  outputBaseDir: string | null
}

export interface GenerateResponse {
  code?: string
  model?: string
  elapsedMs?: number
  reasoningTokens?: number
  systemTokenCount?: number
  userTokenCount?: number
  inputTokens?: number
  outputTokens?: number
  totalTokens?: number
  cachedTokens?: number
  savedTo?: string
  // heavy packet passthrough
  systemPrompt?: string
  userPrompt?: string
  complexityScore?: number
  tokenCount?: number
}

export interface PromptResponse {
  system: string
  user: string
  systemTokens: number
  userTokens: number
  tokenCount: number
}

export interface AssessResponse {
  structuralScore: number
  tier: Tier
  llmScore?: number
  reason?: string
}

export type BatchEvent =
  | { type: 'packet'; id: string; success: boolean; model?: string; elapsedMs?: number; savedTo?: string; error?: string }
  | { type: 'done'; total: number; ok: number; err: number }
