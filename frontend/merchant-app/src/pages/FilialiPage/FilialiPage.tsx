import { useEffect, useState, FormEvent } from 'react'
import { branchesApi, type Branch, type Department } from '../../lib/api/branches'
import { useBranch } from '../../contexts/BranchContext'
import BranchWizard from './BranchWizard'
import './FilialiPage.css'

const DEPT_COLORS = [
  '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6',
  '#ec4899', '#06b6d4', '#84cc16', '#f97316', '#6366f1',
]

interface BranchFormState {
  name: string
  code: string
  address: string
  city: string
  postalCode: string
  country: string
  phone: string
  isActive: boolean
}

const emptyBranchForm: BranchFormState = {
  name: '', code: '', address: '', city: '', postalCode: '', country: '', phone: '', isActive: true,
}

export default function FilialiPage() {
  const { branches, loading, refresh } = useBranch()
  const [showWizard, setShowWizard] = useState(false)
  const [branchModal, setBranchModal] = useState(false)
  const [editingBranch, setEditingBranch] = useState<Branch | null>(null)
  const [branchForm, setBranchForm] = useState<BranchFormState>(emptyBranchForm)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  // Reparto inline
  const [deptModal, setDeptModal] = useState(false)
  const [deptBranchId, setDeptBranchId] = useState<number | null>(null)
  const [editingDept, setEditingDept] = useState<Department | null>(null)
  const [deptName, setDeptName] = useState('')
  const [deptColor, setDeptColor] = useState(DEPT_COLORS[0])
  const [deptActive, setDeptActive] = useState(true)
  const [deptError, setDeptError] = useState('')

  // Apre il wizard automaticamente per un merchant mono-sede che non lo ha
  // ancora chiuso in questa sessione.
  //
  // Lo stato "wizard completato" non va persistito: una volta configurata una
  // seconda filiale la condizione `branches.length <= 1` è già falsa, quindi il
  // wizard non si riapre da solo. Il flag serve solo a non riproporlo dopo una
  // chiusura *senza configurare* (l'azienda resta mono-sede di proposito).
  // Si usa sessionStorage e non localStorage: lo stato è per-sessione, non
  // per-dispositivo — così non lo si "eredita" cross-account sullo stesso
  // browser, né lo si sopprime per sempre su un altro device.
  useEffect(() => {
    if (!loading && branches.length <= 1) {
      const seen = sessionStorage.getItem('merchant.filialiWizardSeen')
      if (!seen) setShowWizard(true)
    }
  }, [loading, branches.length])

  const openAddBranch = () => {
    setEditingBranch(null)
    setBranchForm(emptyBranchForm)
    setError('')
    setBranchModal(true)
  }

  const openEditBranch = (b: Branch) => {
    setEditingBranch(b)
    setBranchForm({
      name: b.name, code: b.code ?? '', address: b.address ?? '', city: b.city ?? '',
      postalCode: b.postalCode ?? '', country: b.country ?? '', phone: b.phone ?? '',
      isActive: b.isActive,
    })
    setError('')
    setBranchModal(true)
  }

  const handleBranchSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!branchForm.name.trim()) { setError('Il nome della filiale è obbligatorio'); return }
    setSaving(true)
    setError('')
    try {
      if (editingBranch) {
        await branchesApi.update(editingBranch.id, branchForm)
      } else {
        await branchesApi.create(branchForm)
      }
      setBranchModal(false)
      await refresh()
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setError(e.response?.data?.message ?? 'Errore durante il salvataggio')
    } finally {
      setSaving(false)
    }
  }

  const handleDeleteBranch = async (b: Branch) => {
    if (!confirm(`Eliminare la filiale "${b.name}"?`)) return
    try {
      await branchesApi.remove(b.id)
      await refresh()
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      alert(e.response?.data?.message ?? 'Errore durante l\'eliminazione')
    }
  }

  const handleSetHq = async (b: Branch) => {
    try {
      await branchesApi.setHeadquarters(b.id)
      await refresh()
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      alert(e.response?.data?.message ?? 'Errore')
    }
  }

  const openAddDept = (branchId: number) => {
    setDeptBranchId(branchId)
    setEditingDept(null)
    setDeptName('')
    setDeptColor(DEPT_COLORS[0])
    setDeptActive(true)
    setDeptError('')
    setDeptModal(true)
  }

  const openEditDept = (branchId: number, d: Department) => {
    setDeptBranchId(branchId)
    setEditingDept(d)
    setDeptName(d.name)
    setDeptColor(d.color)
    setDeptActive(d.isActive)
    setDeptError('')
    setDeptModal(true)
  }

  const handleDeptSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!deptName.trim()) { setDeptError('Il nome del reparto è obbligatorio'); return }
    if (deptBranchId == null) return
    setSaving(true)
    setDeptError('')
    try {
      if (editingDept) {
        await branchesApi.updateDepartment(editingDept.id, { name: deptName, color: deptColor, isActive: deptActive })
      } else {
        await branchesApi.createDepartment(deptBranchId, { name: deptName, color: deptColor })
      }
      setDeptModal(false)
      await refresh()
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setDeptError(e.response?.data?.message ?? 'Errore durante il salvataggio')
    } finally {
      setSaving(false)
    }
  }

  const handleDeleteDept = async (d: Department) => {
    if (!confirm(`Eliminare il reparto "${d.name}"? I turni e i dipendenti collegati resteranno, senza reparto.`)) return
    try {
      await branchesApi.removeDepartment(d.id)
      await refresh()
    } catch {
      alert('Errore durante l\'eliminazione del reparto')
    }
  }

  const closeWizard = () => {
    sessionStorage.setItem('merchant.filialiWizardSeen', '1')
    setShowWizard(false)
  }

  return (
    <div className="filiali-page">
      <div className="page-header-row">
        <div>
          <h1 className="page-title">Filiali</h1>
          <p className="page-subtitle">
            Gestisci i punti vendita / sedi della tua azienda e i reparti al loro interno.
          </p>
        </div>
        <button className="btn-primary" onClick={openAddBranch}>+ Nuova filiale</button>
      </div>

      {loading ? (
        <div className="empty-state">Caricamento...</div>
      ) : branches.length <= 1 ? (
        <div className="empty-state">
          <div className="empty-icon">🏢</div>
          <div className="empty-title">La tua azienda ha una sola sede</div>
          <div className="empty-text">
            Aggiungi una filiale per gestire più punti vendita: ogni turno potrà essere
            assegnato a una sede e a un reparto specifici.
          </div>
          <button className="btn-primary" onClick={() => setShowWizard(true)}>
            Configura le filiali
          </button>
        </div>
      ) : (
        <div className="branches-list">
          {branches.map(b => (
            <div key={b.id} className={`branch-card ${!b.isActive ? 'inactive' : ''}`}>
              <div className="branch-card-head">
                <div className="branch-title-block">
                  <span className="branch-icon">🏢</span>
                  <div>
                    <div className="branch-name">
                      {b.name}
                      {b.isHeadquarters && <span className="branch-hq-pill">Sede principale</span>}
                      {!b.isActive && <span className="branch-inactive-pill">Disattivata</span>}
                    </div>
                    <div className="branch-sub">
                      {[b.city, b.code].filter(Boolean).join(' · ') || 'Nessun indirizzo'}
                      {' · '}{b.employeeCount} {b.employeeCount === 1 ? 'dipendente' : 'dipendenti'}
                    </div>
                  </div>
                </div>
                <div className="branch-actions">
                  {!b.isHeadquarters && b.isActive && (
                    <button className="btn-ghost" onClick={() => handleSetHq(b)}>Imposta come sede principale</button>
                  )}
                  <button className="btn-edit" onClick={() => openEditBranch(b)}>Modifica</button>
                  {!b.isHeadquarters && (
                    <button className="btn-remove" onClick={() => handleDeleteBranch(b)}>Elimina</button>
                  )}
                </div>
              </div>

              <div className="branch-departments">
                <div className="dept-header">
                  <span className="dept-label">Reparti</span>
                  <button className="btn-add-dept" onClick={() => openAddDept(b.id)}>+ Reparto</button>
                </div>
                {b.departments.length === 0 ? (
                  <div className="dept-empty">
                    Nessun reparto. I turni di questa filiale non saranno suddivisi per area.
                  </div>
                ) : (
                  <div className="dept-chips">
                    {b.departments.map(d => (
                      <div
                        key={d.id}
                        className={`dept-chip ${!d.isActive ? 'inactive' : ''}`}
                        style={{ borderColor: d.color }}
                      >
                        <span className="dept-dot" style={{ background: d.color }} />
                        <span className="dept-chip-name">{d.name}</span>
                        <button className="dept-chip-btn" onClick={() => openEditDept(b.id, d)} title="Modifica">✎</button>
                        <button className="dept-chip-btn" onClick={() => handleDeleteDept(d)} title="Elimina">✕</button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Modale filiale */}
      {branchModal && (
        <div className="modal-overlay" onClick={e => { if (e.target === e.currentTarget) setBranchModal(false) }}>
          <div className="modal-box">
            <div className="modal-header">
              <h2 className="modal-title">{editingBranch ? 'Modifica filiale' : 'Nuova filiale'}</h2>
              <button className="modal-close" onClick={() => setBranchModal(false)}>✕</button>
            </div>
            <form onSubmit={handleBranchSubmit}>
              <div className="modal-body">
                {error && <div className="modal-error">{error}</div>}
                <div className="form-group">
                  <label className="form-label">Nome *</label>
                  <input
                    type="text" className="form-input" placeholder="es. Milano Centro"
                    value={branchForm.name} autoFocus required
                    onChange={e => setBranchForm(p => ({ ...p, name: e.target.value }))}
                  />
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">Codice</label>
                    <input
                      type="text" className="form-input" placeholder="es. MI01"
                      value={branchForm.code}
                      onChange={e => setBranchForm(p => ({ ...p, code: e.target.value }))}
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Città</label>
                    <input
                      type="text" className="form-input"
                      value={branchForm.city}
                      onChange={e => setBranchForm(p => ({ ...p, city: e.target.value }))}
                    />
                  </div>
                </div>
                <details className="form-details">
                  <summary>Dettagli opzionali</summary>
                  <div className="form-group">
                    <label className="form-label">Indirizzo</label>
                    <input
                      type="text" className="form-input"
                      value={branchForm.address}
                      onChange={e => setBranchForm(p => ({ ...p, address: e.target.value }))}
                    />
                  </div>
                  <div className="form-row">
                    <div className="form-group">
                      <label className="form-label">CAP</label>
                      <input
                        type="text" className="form-input"
                        value={branchForm.postalCode}
                        onChange={e => setBranchForm(p => ({ ...p, postalCode: e.target.value }))}
                      />
                    </div>
                    <div className="form-group">
                      <label className="form-label">Paese</label>
                      <input
                        type="text" className="form-input"
                        value={branchForm.country}
                        onChange={e => setBranchForm(p => ({ ...p, country: e.target.value }))}
                      />
                    </div>
                  </div>
                  <div className="form-group">
                    <label className="form-label">Telefono</label>
                    <input
                      type="text" className="form-input"
                      value={branchForm.phone}
                      onChange={e => setBranchForm(p => ({ ...p, phone: e.target.value }))}
                    />
                  </div>
                </details>
                {editingBranch && !editingBranch.isHeadquarters && (
                  <div className="form-group">
                    <label className="form-checkbox">
                      <input
                        type="checkbox"
                        checked={branchForm.isActive}
                        onChange={e => setBranchForm(p => ({ ...p, isActive: e.target.checked }))}
                      />
                      Filiale attiva (selezionabile per nuovi turni)
                    </label>
                  </div>
                )}
              </div>
              <div className="modal-footer">
                <button type="button" className="btn-cancel" onClick={() => setBranchModal(false)}>Annulla</button>
                <button type="submit" className="btn-primary" disabled={saving}>
                  {saving ? 'Salvataggio...' : 'Salva'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modale reparto */}
      {deptModal && (
        <div className="modal-overlay" onClick={e => { if (e.target === e.currentTarget) setDeptModal(false) }}>
          <div className="modal-box">
            <div className="modal-header">
              <h2 className="modal-title">{editingDept ? 'Modifica reparto' : 'Nuovo reparto'}</h2>
              <button className="modal-close" onClick={() => setDeptModal(false)}>✕</button>
            </div>
            <form onSubmit={handleDeptSubmit}>
              <div className="modal-body">
                {deptError && <div className="modal-error">{deptError}</div>}
                <div className="form-group">
                  <label className="form-label">Nome *</label>
                  <input
                    type="text" className="form-input" placeholder="es. Produzione"
                    value={deptName} autoFocus required
                    onChange={e => setDeptName(e.target.value)}
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">Colore</label>
                  <div className="color-palette">
                    {DEPT_COLORS.map(c => (
                      <button
                        key={c} type="button"
                        className={`color-swatch ${deptColor === c ? 'selected' : ''}`}
                        style={{ background: c }}
                        onClick={() => setDeptColor(c)}
                        aria-label={`Colore ${c}`}
                      />
                    ))}
                  </div>
                </div>
                {editingDept && (
                  <div className="form-group">
                    <label className="form-checkbox">
                      <input
                        type="checkbox"
                        checked={deptActive}
                        onChange={e => setDeptActive(e.target.checked)}
                      />
                      Reparto attivo
                    </label>
                  </div>
                )}
              </div>
              <div className="modal-footer">
                <button type="button" className="btn-cancel" onClick={() => setDeptModal(false)}>Annulla</button>
                <button type="submit" className="btn-primary" disabled={saving}>
                  {saving ? 'Salvataggio...' : 'Salva'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {showWizard && <BranchWizard onClose={closeWizard} onDone={async () => { await refresh(); closeWizard() }} />}
    </div>
  )
}
