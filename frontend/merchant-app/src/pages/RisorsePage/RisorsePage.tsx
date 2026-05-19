import { useState, useEffect, FormEvent } from 'react'
import apiClient from '../../lib/axios'
import { MerchantUser } from '../../App'
import { skillsApi, Skill } from '../../lib/api/skills'
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
}

export default function RisorsePage({ user: _user }: RisorsePageProps) {
  const [employees, setEmployees] = useState<Employee[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [skills, setSkills] = useState<Skill[]>([])
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [editEmployee, setEditEmployee] = useState<Employee | null>(null)
  const [formData, setFormData] = useState<NewEmployeeForm>({ firstName: '', lastName: '', email: '', phoneNumber: '', roleId: '', skillIds: [] })
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState('')
  const [shiftPanelEmployee, setShiftPanelEmployee] = useState<Employee | null>(null)
  const [filterSkillId, setFilterSkillId] = useState<number | null>(null)

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
    setFormData({ firstName: '', lastName: '', email: '', phoneNumber: '', roleId: roles[0]?.id?.toString() ?? '', skillIds: [] })
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
      if (editEmployee) {
        await apiClient.put(`/employees/${editEmployee.id}`, {
          firstName: formData.firstName,
          lastName: formData.lastName,
          phoneNumber: formData.phoneNumber || undefined,
          roleId: formData.roleId ? Number(formData.roleId) : editEmployee.roleId ?? 0,
          isActive: editEmployee.isActive,
          skillIds: formData.skillIds,
        })
      } else {
        await apiClient.post('/employees', {
          firstName: formData.firstName,
          lastName: formData.lastName,
          email: formData.email,
          phoneNumber: formData.phoneNumber || undefined,
          roleId: formData.roleId ? Number(formData.roleId) : 0,
          skillIds: formData.skillIds,
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

  const filteredEmployees = filterSkillId == null
    ? employees
    : employees.filter(e => e.skills?.some(s => s.skillId === filterSkillId))

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
                {skills.length > 0 && <th>Mansioni</th>}
                <th>Stato</th>
                <th>Azioni</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr className="loading-row">
                  <td colSpan={skills.length > 0 ? 6 : 5}>Caricamento...</td>
                </tr>
              ) : filteredEmployees.length === 0 ? (
                <tr>
                  <td colSpan={skills.length > 0 ? 6 : 5}>
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
