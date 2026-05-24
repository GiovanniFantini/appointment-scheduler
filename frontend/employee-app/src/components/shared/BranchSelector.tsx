import { useBranch } from '../../contexts/BranchContext'
import './BranchSelector.css'

interface BranchSelectorProps {
  /** Se true mostra anche il selettore reparto. Default: true. */
  showDepartment?: boolean
  /** Se true mostra l'opzione "Tutte le filiali". Default: true. */
  allowAll?: boolean
}

/**
 * Selettore filiale + reparto riusabile (Calendario, Pianificazione, Report, Risorse).
 * Si auto-nasconde per i merchant mono-sede senza reparti: in quel caso non c'è
 * nulla da selezionare e l'esperienza resta identica a prima della feature.
 */
export default function BranchSelector({ showDepartment = true, allowAll = true }: BranchSelectorProps) {
  const {
    activeBranches,
    isMultiBranch,
    activeBranchId,
    activeDepartmentId,
    setActiveBranch,
    setActiveDepartment,
  } = useBranch()

  // Merchant mono-sede senza reparti: niente selettore.
  if (!isMultiBranch) return null

  // Il dropdown filiale ha senso solo con più di una filiale: con una sola sede
  // (es. fabbrica con soli reparti) sarebbe un menu inutile a una voce.
  const showBranchPicker = activeBranches.length > 1

  // Filiale di cui mostrare i reparti: quella selezionata, oppure — se c'è una sola
  // filiale — l'unica esistente (così il selettore reparto funziona anche mono-sede).
  const selectedBranch = activeBranchId != null
    ? activeBranches.find(b => b.id === activeBranchId)
    : (activeBranches.length === 1 ? activeBranches[0] : undefined)
  const departments = selectedBranch?.departments.filter(d => d.isActive) ?? []

  return (
    <div className="branch-selector">
      {showBranchPicker && (
        <div className="branch-selector-field">
          <span className="branch-selector-icon">🏢</span>
          <select
            className="branch-selector-select"
            value={activeBranchId ?? ''}
            onChange={e => setActiveBranch(e.target.value ? Number(e.target.value) : null)}
            aria-label="Filiale"
          >
            {allowAll && <option value="">Tutte le filiali</option>}
            {activeBranches.map(b => (
              <option key={b.id} value={b.id}>
                {b.name}{b.isHeadquarters ? ' (sede principale)' : ''}
              </option>
            ))}
          </select>
        </div>
      )}

      {showDepartment && selectedBranch && departments.length > 0 && (
        <div className="branch-selector-field">
          <span className="branch-selector-icon">🗂</span>
          <select
            className="branch-selector-select"
            value={activeDepartmentId ?? ''}
            onChange={e => setActiveDepartment(e.target.value ? Number(e.target.value) : null)}
            aria-label="Reparto"
          >
            <option value="">Tutti i reparti</option>
            {departments.map(d => (
              <option key={d.id} value={d.id}>{d.name}</option>
            ))}
          </select>
        </div>
      )}
    </div>
  )
}
