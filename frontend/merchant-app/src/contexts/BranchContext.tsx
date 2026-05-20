import { createContext, useContext, useEffect, useState, useCallback, ReactNode } from 'react'
import { branchesApi, type Branch } from '../lib/api/branches'

const STORAGE_KEY_BRANCH = 'merchant.activeBranchId'
const STORAGE_KEY_DEPARTMENT = 'merchant.activeDepartmentId'

/** Valore speciale del selettore: nessun filtro filiale ("Tutte le filiali"). */
export const ALL_BRANCHES = null

interface BranchContextValue {
  /** Tutte le filiali del merchant (incluse le inattive). */
  branches: Branch[]
  /** Solo le filiali attive — usate nei selettori di creazione. */
  activeBranches: Branch[]
  loading: boolean
  /** Filiale attualmente selezionata; null = "Tutte le filiali". */
  activeBranchId: number | null
  /** Reparto attualmente selezionato; null = "Tutti i reparti". */
  activeDepartmentId: number | null
  /**
   * True se il merchant ha senso che veda i selettori filiale/reparto:
   * più di una filiale, oppure almeno un reparto. I merchant mono-sede senza
   * reparti hanno UX identica a prima della feature.
   */
  isMultiBranch: boolean
  setActiveBranch: (branchId: number | null) => void
  setActiveDepartment: (departmentId: number | null) => void
  /** Ricarica la lista filiali dal server (dopo CRUD nella FilialiPage). */
  refresh: () => Promise<void>
  /** Helper: la filiale di default da pre-selezionare nei form di creazione. */
  defaultBranchId: number | null
}

const BranchContext = createContext<BranchContextValue | undefined>(undefined)

export function BranchProvider({ children }: { children: ReactNode }) {
  const [branches, setBranches] = useState<Branch[]>([])
  const [loading, setLoading] = useState(true)
  const [activeBranchId, setActiveBranchIdState] = useState<number | null>(() => {
    const raw = localStorage.getItem(STORAGE_KEY_BRANCH)
    return raw ? Number(raw) : null
  })
  const [activeDepartmentId, setActiveDepartmentIdState] = useState<number | null>(() => {
    const raw = localStorage.getItem(STORAGE_KEY_DEPARTMENT)
    return raw ? Number(raw) : null
  })

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const data = await branchesApi.list(true)
      setBranches(data)
      // Se la filiale selezionata non esiste più o è stata disattivata,
      // ricade su "Tutte le filiali" e azzera il reparto collegato.
      let branchStillValid = true
      setActiveBranchIdState(prev => {
        if (prev != null && !data.some(b => b.id === prev && b.isActive)) {
          branchStillValid = false
          localStorage.removeItem(STORAGE_KEY_BRANCH)
          return null
        }
        return prev
      })
      // Se il reparto selezionato non esiste più, è disattivato, oppure la sua
      // filiale è caduta, azzera la selezione reparto.
      setActiveDepartmentIdState(prev => {
        if (prev == null) return null
        const deptStillValid = branchStillValid && data.some(b =>
          b.isActive && b.departments.some(d => d.id === prev && d.isActive))
        if (!deptStillValid) {
          localStorage.removeItem(STORAGE_KEY_DEPARTMENT)
          return null
        }
        return prev
      })
    } catch {
      setBranches([])
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  const setActiveBranch = useCallback((branchId: number | null) => {
    setActiveBranchIdState(branchId)
    if (branchId == null) localStorage.removeItem(STORAGE_KEY_BRANCH)
    else localStorage.setItem(STORAGE_KEY_BRANCH, String(branchId))
    // Cambiare filiale invalida il reparto (i reparti sono figli della filiale).
    setActiveDepartmentIdState(null)
    localStorage.removeItem(STORAGE_KEY_DEPARTMENT)
  }, [])

  const setActiveDepartment = useCallback((departmentId: number | null) => {
    setActiveDepartmentIdState(departmentId)
    if (departmentId == null) localStorage.removeItem(STORAGE_KEY_DEPARTMENT)
    else localStorage.setItem(STORAGE_KEY_DEPARTMENT, String(departmentId))
  }, [])

  const activeBranches = branches.filter(b => b.isActive)
  // Conta solo i reparti attivi di filiali attive: una filiale disattivata non
  // deve far comparire i selettori.
  const hasDepartments = activeBranches.some(b => b.departments.some(d => d.isActive))
  const isMultiBranch = activeBranches.length > 1 || hasDepartments

  // Mono-sede: la HQ è la filiale di default per i form di creazione.
  const defaultBranchId = activeBranchId
    ?? (activeBranches.find(b => b.isHeadquarters)?.id
      ?? activeBranches[0]?.id
      ?? null)

  const value: BranchContextValue = {
    branches,
    activeBranches,
    loading,
    activeBranchId,
    activeDepartmentId,
    isMultiBranch,
    setActiveBranch,
    setActiveDepartment,
    refresh: load,
    defaultBranchId,
  }

  return <BranchContext.Provider value={value}>{children}</BranchContext.Provider>
}

export function useBranch(): BranchContextValue {
  const ctx = useContext(BranchContext)
  if (!ctx) throw new Error('useBranch deve essere usato dentro <BranchProvider>')
  return ctx
}
