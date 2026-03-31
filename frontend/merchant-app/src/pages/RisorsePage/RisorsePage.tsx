import { useState, useEffect, FormEvent } from 'react'
import apiClient from '../../lib/axios'
import { MerchantUser } from '../../App'
import './RisorsePage.css'

interface RisorsePageProps {
  user: MerchantUser
}

interface Employee {
  id: number
  firstName: string
  lastName: string
  email: string
  role?: string
  roleName?: string
  isActive?: boolean
}

interface Role {
  id: number
  name: string
}

interface NewEmployeeForm {
  firstName: string
  lastName: string
  email: string
  roleId: string
}

export default function RisorsePage({ user: _user }: RisorsePageProps) {
  const [employees, setEmployees] = useState<Employee[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [editEmployee, setEditEmployee] = useState<Employee | null>(null)
  const [formData, setFormData] = useState<NewEmployeeForm>({ firstName: '', lastName: '', email: '', roleId: '' })
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState('')

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

  useEffect(() => {
    fetchEmployees()
    fetchRoles()
  }, [])

  const openAddModal = () => {
    setEditEmployee(null)
    setFormData({ firstName: '', lastName: '', email: '', roleId: roles[0]?.id?.toString() ?? '' })
    setFormError('')
    setShowModal(true)
  }

  const openEditModal = (emp: Employee) => {
    setEditEmployee(emp)
    setFormData({
      firstName: emp.firstName,
      lastName: emp.lastName,
      email: emp.email,
      roleId: '',
    })
    setFormError('')
    setShowModal(true)
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
      const payload = {
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        roleId: formData.roleId ? Number(formData.roleId) : undefined,
      }
      if (editEmployee) {
        await apiClient.put(`/employees/${editEmployee.id}`, payload)
      } else {
        await apiClient.post('/employees', payload)
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

      <div className="risorse-table-card">
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Dipendente</th>
                <th>Email</th>
                <th>Ruolo</th>
                <th>Stato</th>
                <th>Azioni</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr className="loading-row">
                  <td colSpan={5}>Caricamento...</td>
                </tr>
              ) : employees.length === 0 ? (
                <tr>
                  <td colSpan={5}>
                    <div className="empty-state">
                      <div className="empty-icon">👥</div>
                      <div className="empty-text">Nessun dipendente trovato</div>
                    </div>
                  </td>
                </tr>
              ) : (
                employees.map(emp => (
                  <tr key={emp.id}>
                    <td>
                      <div className="employee-name-cell">
                        <div className="emp-avatar">{getInitials(emp)}</div>
                        <span className="emp-full-name">{emp.firstName} {emp.lastName}</span>
                      </div>
                    </td>
                    <td>{emp.email}</td>
                    <td>{emp.roleName ?? emp.role ?? '—'}</td>
                    <td>
                      <span className={`status-badge ${emp.isActive !== false ? 'active' : 'inactive'}`}>
                        <span className="status-dot" />
                        {emp.isActive !== false ? 'Attivo' : 'Inattivo'}
                      </span>
                    </td>
                    <td>
                      <div className="action-buttons">
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
