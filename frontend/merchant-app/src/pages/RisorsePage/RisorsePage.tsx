import { useState, useEffect, FormEvent } from 'react'
import apiClient from '../../lib/axios'
import { MerchantUser } from '../../App'
import { skillsApi, Skill } from '../../lib/api/skills'
import { useBranch } from '../../contexts/BranchContext'
import EmployeeShiftPanel from '../../components/EmployeeShiftPanel/EmployeeShiftPanel'
import './RisorsePage.css'

interface RisorsePageProps {
  user: MerchantUser
}

interface EmployeeSkill {
  skillId: number
  skillName: string
  skillColor: string
}

interface Employee {
  id: number
  firstName: string
  lastName: string
  fullName: string
  email: string
  phoneNumber?: string
  roleName?: string
  roleId?: number
  isActive: boolean
  hasUserAccount: boolean
  skills?: EmployeeSkill[]
  homeBranchId?: number | null
  homeBranchName?: string | null
  homeDepartmentId?: number | null
  homeDepartmentName?: string | null
  allowedBranchIds?: number[]
}

interface Role {
  id: number
  name: string
}

interface NewEmployeeForm {
  firstName: string
  lastName: string
  email: string
  phoneNumber: string
  roleId: string
  skillIds: number[]
  homeBranchId: string
  homeDepartmentId: string
  allowedBranchIds: number[]
}

const emptyForm: NewEmployeeForm = {
  firstName: '', lastName: '', email: '', phoneNumber: '', roleId: '',
  skillIds: [], homeBranchId: '', homeDepartmentId: '', allowedBranchIds: [],
}

export default function RisorsePage({ user: _user }: RisorsePageProps) {
  const { activeBranches, isMultiBranch } = useBranch()
  const [employees, setEmployees] = useState<Employee[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [skills, setSkills] = useState<Skill[]>([])
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [editEmployee, setEditEmployee] = useState<Employee | null>(null)
  const [formData, setFormData] = useState<NewEmployeeForm>(emptyForm)
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState('')
  const [shiftPanelEmployee, setShiftPanelEmployee] = useState<Employee | null>(null)
  const [filterSkillId, setFilterSkillId] = useState<number | null>(null)
  const [filterBranchId, setFilterBranchId] = useState<number | null>(null)

  const fetchEmployees = async () => {
    setLoading(true)
    try {
      const res = await apiClient.get('/employees')
      if (Array.isArray(res.data)) setEmployees(res.data as Employee[])
    } catch {
      setEmployees([])
    } finally {
      setLoading(false)
    }
  }

  const fetchRoles = async () => {
    try {
      const res = await apiClient.get('/merchant-roles')
      if (Array.isArray(res.data)) setRoles(res.data as Role[])
    } catch {
      setRoles([])
    }
  }

  const fetchSkills = async () => {
    try {
      const list = await skillsApi.list()
      setSkills(list.filter(s => s.isActive))
    } catch {
      setSkills([])
    }
  }

  useEffect(() => {
    fetchEmployees()
    fetchRoles()
    fetchSkills()
  }, [])

  const openAddModal = () => {
    setEditEmployee(null)
    const hq = activeBranches.find(b => b.isHeadquarters) ?? activeBranches[0]
    setFormData({
      ...emptyForm,
      roleId: roles[0]?.id?.toString() ?? '',
      homeBranchId: hq?.id?.toString() ?? '',
    })
    setFormError('')
    setShowModal(true)
  }

  const openEditModal = (emp: Employee) => {
    setEditEmployee(emp)
    setFormData({
      firstName: emp.firstName,
      lastName: emp.lastName,
      email: emp.email,
      phoneNumber: emp.phoneNumber ?? '',
      roleId: emp.roleId?.toString() ?? '',
      skillIds: emp.skills?.map(s => s.skillId) ?? [],
      homeBranchId: emp.homeBranchId?.toString() ?? '',
      homeDepartmentId: emp.homeDepartmentId?.toString() ?? '',
      allowedBranchIds: emp.allowedBranchIds ?? [],
    })
    setFormError('')
    setShowModal(true)
  }

  const toggleSkill = (id: number) => {
    setFormData(p => ({
      ...p,
      skillIds: p.skillIds.includes(id) ? p.skillIds.filter(x => x !== id) : [...p.skillIds, id]
    }))
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!formData.firstName || !formData.lastName || !formData.email) {
      setFormError('Tutti i campi obbligatori devono essere compilati')
      return
    }
    setSaving(true)
    setFormError('')
    try {
      const branchFields = {
        homeBranchId: formData.homeBranchId ? Number(formData.homeBranchId) : null,
        homeDepartmentId: formData.homeDepartmentId ? Number(formData.homeDepartmentId) : null,
        allowedBranchIds: formData.allowedBranchIds,
      }
      if (editEmployee) {
        await apiClient.put(`/employees/${editEmployee.id}`, {
          firstName: formData.firstName,
          lastName: formData.lastName,
          phoneNumber: formData.phoneNumber || undefined,
          roleId: formData.roleId ? Number(formData.roleId) : editEmployee.roleId ?? 0,
          isActive: editEmployee.isActive,
          skillIds: formData.skillIds,
          ...branchFields,
        })
      } else {
        await apiClient.post('/employees', {
          firstName: formData.firstName,
          lastName: formData.lastName,
          email: formData.email,
          phoneNumber: formData.phoneNumber || undefined,
          roleId: formData.roleId ? Number(formData.roleId) : 0,
          skillIds: formData.skillIds,
          ...branchFields,
        })
      }
      setShowModal(false)
      await fetchEmployees()
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setFormError(e.response?.data?.message ?? 'Errore durante il salvataggio')
    } finally {
      setSaving(false)
    }
  }

  const handleRemove = async (emp: Employee) => {
    if (!confirm(`Rimuovere ${emp.firstName} ${emp.lastName}?`)) return
    try {
      await apiClient.delete(`/employees/${emp.id}`)
      await fetchEmployees()
    } catch {
      alert('Errore durante la rimozione')
    }
  }

  const getInitials = (emp: Employee) =>
    `${emp.firstName?.[0] ?? ''}${emp.lastName?.[0] ?? ''}`.toUpperCase()

  const filteredEmployees = employees.filter(e => {
    if (filterSkillId != null && !e.skills?.some(s => s.skillId === filterSkillId)) return false
    if (filterBranchId != null) {
      const inHome = e.homeBranchId === filterBranchId
      const inAllowed = e.allowedBranchIds?.includes(filterBranchId) ?? false
      if (!inHome && !inAllowed) return false
    }
    return true
  })

  // Numero colonne tabella (per il colSpan delle righe stato vuoto/loading):
  // Dipendente, Email, Ruolo, [Sede], [Mansioni], Stato, Azioni
  const tableColumnCount = 5 + (isMultiBranch ? 1 : 0) + (skills.length > 0 ? 1 : 0)

  return (
    <div className="risorse-page">
      <div className="page-header-row">
        <div>
          <h1 className="page-title">Risorse</h1>
          <p className="page-subtitle">Gestisci i dipendenti della tua azienda</p>
        </div>
        <button className="btn-primary" onClick={openAddModal}>
          + Aggiungi Dipendente
        </button>
      </div>

      {isMultiBranch && (
        <div className="risorse-filter-bar">
          <span className="filter-label">Filtra per filiale:</span>
          <button
            type="button"
            className={`filter-chip ${filterBranchId == null ? 'active' : ''}`}
            onClick={() => setFilterBranchId(null)}
          >Tutte</button>
          {activeBranches.map(b => (
            <button
              key={b.id}
              type="button"
              className={`filter-chip ${filterBranchId === b.id ? 'active' : ''}`}
              onClick={() => setFilterBranchId(b.id)}
            >
              🏢 {b.name}
            </button>
          ))}
        </div>
      )}

      {skills.length > 0 && (
        <div className="risorse-filter-bar">
          <span className="filter-label">Filtra per mansione:</span>
          <button
            type="button"
            className={`filter-chip ${filterSkillId == null ? 'active' : ''}`}
            onClick={() => setFilterSkillId(null)}
          >Tutte</button>
          {skills.map(s => (
            <button
              key={s.id}
              type="button"
              className={`filter-chip ${filterSkillId === s.id ? 'active' : ''}`}
              style={filterSkillId === s.id ? { background: s.color, borderColor: s.color } : { borderColor: s.color }}
              onClick={() => setFilterSkillId(s.id)}
            >
              <span className="filter-dot" style={{ background: s.color }} />
              {s.name}
            </button>
          ))}
        </div>
      )}

      <div className="risorse-table-card">
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Dipendente</th>
                <th>Email</th>
                <th>Ruolo</th>
                {isMultiBranch && <th>Sede</th>}
                {skills.length > 0 && <th>Mansioni</th>}
                <th>Stato</th>
                <th>Azioni</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr className="loading-row">
                  <td colSpan={tableColumnCount}>Caricamento...</td>
                </tr>
              ) : filteredEmployees.length === 0 ? (
                <tr>
                  <td colSpan={tableColumnCount}>
                    <div className="empty-state">
                      <div className="empty-icon">👥</div>
                      <div className="empty-text">Nessun dipendente trovato</div>
                    </div>
                  </td>
                </tr>
              ) : (
                filteredEmployees.map(emp => (
                  <tr key={emp.id}>
                    <td>
                      <div className="employee-name-cell">
                        <div className="emp-avatar">{getInitials(emp)}</div>
                        <span className="emp-full-name">{emp.firstName} {emp.lastName}</span>
                      </div>
                    </td>
                    <td>{emp.email}</td>
                    <td>{emp.roleName ?? '—'}</td>
                    {isMultiBranch && (
                      <td>
                        {emp.homeBranchName ? (
                          <div className="branch-cell">
                            <span className="branch-cell-name">🏢 {emp.homeBranchName}</span>
                            {emp.homeDepartmentName && (
                              <span className="branch-cell-dept">{emp.homeDepartmentName}</span>
                            )}
                            {emp.allowedBranchIds && emp.allowedBranchIds.length > 0 && (
                              <span className="branch-cell-extra">
                                +{emp.allowedBranchIds.length} {emp.allowedBranchIds.length === 1 ? 'sede' : 'sedi'}
                              </span>
                            )}
                          </div>
                        ) : (
                          <span className="dash">—</span>
                        )}
                      </td>
                    )}
                    {skills.length > 0 && (
                      <td>
                        {emp.skills && emp.skills.length > 0 ? (
                          <div className="skill-chips">
                            {emp.skills.map(s => (
                              <span
                                key={s.skillId}
                                className="skill-chip"
                                style={{ background: s.skillColor + '22', color: s.skillColor, borderColor: s.skillColor + '55' }}
                              >
                                <span className="skill-chip-dot" style={{ background: s.skillColor }} />
                                {s.skillName}
                              </span>
                            ))}
                          </div>
                        ) : (
                          <span className="dash">—</span>
                        )}
                      </td>
                    )}
                    <td>
                      <span className={`status-badge ${emp.isActive ? 'active' : 'inactive'}`}>
                        <span className="status-dot" />
                        {emp.isActive ? 'Attivo' : 'Inattivo'}
                      </span>
                    </td>
                    <td>
                      <div className="action-buttons">
                        <button className="btn-edit" onClick={() => setShiftPanelEmployee(emp)}>Pianifica turni</button>
                        <button className="btn-edit" onClick={() => openEditModal(emp)}>Modifica</button>
                        <button className="btn-remove" onClick={() => handleRemove(emp)}>Rimuovi</button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {shiftPanelEmployee && (
        <EmployeeShiftPanel
          employeeId={shiftPanelEmployee.id}
          employeeFullName={`${shiftPanelEmployee.firstName} ${shiftPanelEmployee.lastName}`}
          onClose={() => setShiftPanelEmployee(null)}
        />
      )}

      {showModal && (
        <div className="modal-overlay" onClick={e => { if (e.target === e.currentTarget) setShowModal(false) }}>
          <div className="modal-box">
            <div className="modal-header">
              <h2 className="modal-title">{editEmployee ? 'Modifica Dipendente' : 'Nuovo Dipendente'}</h2>
              <button className="modal-close" onClick={() => setShowModal(false)}>✕</button>
            </div>
            <form onSubmit={handleSubmit}>
              <div className="modal-body">
                {formError && <div className="modal-error">{formError}</div>}
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">Nome *</label>
                    <input
                      type="text"
                      className="form-input"
                      placeholder="Mario"
                      value={formData.firstName}
                      onChange={e => setFormData(p => ({ ...p, firstName: e.target.value }))}
                      required
                    />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Cognome *</label>
                    <input
                      type="text"
                      className="form-input"
                      placeholder="Rossi"
                      value={formData.lastName}
                      onChange={e => setFormData(p => ({ ...p, lastName: e.target.value }))}
                      required
                    />
                  </div>
                </div>
                <div className="form-group">
                  <label className="form-label">Email *</label>
                  <input
                    type="email"
                    className="form-input"
                    placeholder="mario.rossi@azienda.it"
                    value={formData.email}
                    onChange={e => setFormData(p => ({ ...p, email: e.target.value }))}
                    required
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">Ruolo</label>
                  <select
                    className="form-select"
                    value={formData.roleId}
                    onChange={e => setFormData(p => ({ ...p, roleId: e.target.value }))}
                  >
                    <option value="">— Nessun ruolo —</option>
                    {roles.map(r => (
                      <option key={r.id} value={r.id}>{r.name}</option>
                    ))}
                  </select>
                </div>
                {isMultiBranch && (() => {
                  const homeBranch = activeBranches.find(b => b.id === Number(formData.homeBranchId))
                  const homeDepts = homeBranch?.departments.filter(d => d.isActive) ?? []
                  const extraBranches = activeBranches.filter(b => b.id !== Number(formData.homeBranchId))
                  return (
                    <>
                      <div className="form-row">
                        {/* Dropdown sede solo con più filiali: con una sola sede
                            (es. fabbrica con soli reparti) sarebbe un menu inutile. */}
                        {activeBranches.length > 1 && (
                          <div className="form-group">
                            <label className="form-label">Sede principale</label>
                            <select
                              className="form-select"
                              value={formData.homeBranchId}
                              onChange={e => setFormData(p => ({
                                ...p,
                                homeBranchId: e.target.value,
                                homeDepartmentId: '', // il reparto dipende dalla filiale
                                allowedBranchIds: p.allowedBranchIds.filter(id => id !== Number(e.target.value)),
                              }))}
                            >
                              {activeBranches.map(b => (
                                <option key={b.id} value={b.id}>
                                  {b.name}{b.isHeadquarters ? ' (sede principale)' : ''}
                                </option>
                              ))}
                            </select>
                          </div>
                        )}
                        {homeDepts.length > 0 && (
                          <div className="form-group">
                            <label className="form-label">Reparto</label>
                            <select
                              className="form-select"
                              value={formData.homeDepartmentId}
                              onChange={e => setFormData(p => ({ ...p, homeDepartmentId: e.target.value }))}
                            >
                              <option value="">— Nessuno / Jolly —</option>
                              {homeDepts.map(d => (
                                <option key={d.id} value={d.id}>{d.name}</option>
                              ))}
                            </select>
                          </div>
                        )}
                      </div>
                      {extraBranches.length > 0 && (
                        <div className="form-group">
                          <label className="form-label">Filiali aggiuntive consentite</label>
                          <div className="skill-picker">
                            {extraBranches.map(b => {
                              const selected = formData.allowedBranchIds.includes(b.id)
                              return (
                                <button
                                  key={b.id}
                                  type="button"
                                  className={`skill-pick-chip ${selected ? 'selected' : ''}`}
                                  onClick={() => setFormData(p => ({
                                    ...p,
                                    allowedBranchIds: selected
                                      ? p.allowedBranchIds.filter(id => id !== b.id)
                                      : [...p.allowedBranchIds, b.id],
                                  }))}
                                >
                                  {selected ? '✓ ' : '+ '}{b.name}
                                </button>
                              )
                            })}
                          </div>
                          <p className="form-hint">
                            Il dipendente potrà essere assegnato a turni di queste filiali oltre alla sede principale.
                          </p>
                        </div>
                      )}
                    </>
                  )
                })()}
                {skills.length > 0 && (
                  <div className="form-group">
                    <label className="form-label">Mansioni</label>
                    <div className="skill-picker">
                      {skills.map(s => {
                        const selected = formData.skillIds.includes(s.id)
                        return (
                          <button
                            key={s.id}
                            type="button"
                            className={`skill-pick-chip ${selected ? 'selected' : ''}`}
                            style={selected
                              ? { background: s.color, borderColor: s.color, color: '#fff' }
                              : { borderColor: s.color + '88', color: s.color }
                            }
                            onClick={() => toggleSkill(s.id)}
                          >
                            {selected ? '✓ ' : '+ '}{s.name}
                          </button>
                        )
                      })}
                    </div>
                    <p className="form-hint">Opzionale. Le mansioni servono a coprire i fabbisogni dei turni.</p>
                  </div>
                )}
              </div>
              <div className="modal-footer">
                <button type="button" className="btn-cancel" onClick={() => setShowModal(false)}>Annulla</button>
                <button type="submit" className="btn-primary" disabled={saving}>
                  {saving ? 'Salvataggio...' : 'Salva'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
