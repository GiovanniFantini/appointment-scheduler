export type GuideLevel = 'ReadOnly' | 'Operator' | 'Manager'
export interface GuideAccess { activeFeatures: string[]; featureLevels?: Record<string, string> }
export interface GuideStep {
  /** Stable CSS selector, normally a data-guide attribute. */
  target: string
  title: string
  text: string
  /** Advance once the user's action makes this target visible. Never clicks for the user. */
  nextWhen?: string
  optional?: boolean
  /** Explain a consequential control without letting the guide invoke it. */
  readOnly?: boolean
}
export interface GuideTour {
  id: string; title: string; description: string
  feature?: string; minLevel?: GuideLevel
  /** A record or control needed before the tour can begin. */
  requiresTarget?: string
  unavailableWhen?: string
  unavailableMessage?: string
  steps: GuideStep[]
}
export interface PageGuideDefinition {
  id: string; title: string; version: number
  feature?: string; minLevel?: GuideLevel
  tours: GuideTour[]
}
const ranks: Record<string, number> = { ReadOnly: 1, Operator: 2, Manager: 3 }
/** Mirrors application permissions, including the default read-only level. */
export function canUseGuide(access: GuideAccess, feature?: string, minLevel?: GuideLevel): boolean {
  if (feature && !access.activeFeatures.includes(feature)) return false
  return !minLevel || !!feature && (ranks[access.featureLevels?.[feature] ?? 'ReadOnly'] ?? 1) >= ranks[minLevel]
}
/** Limits the catalogue to the current subscription and role. */
export function availableGuideTours(page: PageGuideDefinition, access: GuideAccess): GuideTour[] {
  if (!canUseGuide(access, page.feature, page.minLevel)) return []
  return page.tours.filter(tour => canUseGuide(access, tour.feature ?? page.feature, tour.minLevel))
}
/** Separates history by application, account, company and content version. */
export function guideStorageKey(app: string, userId: number, merchantId: number | undefined, page: PageGuideDefinition): string {
  return `guide:${app}:${userId}:${merchantId ?? 'none'}:${page.id}:v${page.version}`
}
