export const PLAN_FEATURES = ['Calendario', 'Richieste', 'Risorse', 'Ruoli', 'Documenti', 'Report', 'Mansioni', 'Filiali', 'Timbratura', 'Magazzino']

export interface SubscriptionPlan {
  id: number
  name: string
  description: string | null
  features: number[]
  maxEmployees: number | null
  maxBranches: number | null
  maxStorageBytes: number | null
  assignedMerchants: number
}

export function subscriptionError(error: unknown): string {
  const value = error as { response?: { data?: { message?: string } } }
  return value.response?.data?.message ?? 'Operazione non riuscita. Riprova.'
}
