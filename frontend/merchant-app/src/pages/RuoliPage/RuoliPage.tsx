import { useState, useEffect, FormEvent } from 'react'
import apiClient from '../../lib/axios'
import './RuoliPage.css'

interface Feature {
  name: string
  icon: string
}

interface MerchantRole {
  id: number
  name: string
  isDefault?: boolean
  features: string[]
}

const ALL_FEATURES: Feature[] = [
  { name: 'Calendario', icon: '📅' },
  { name: 'Richieste', icon: '📋' },
  { name: 'Risorse', icon: '👥' },
  { name: 'Ruoli', icon: '🔑' },
  { name: 'Documenti', icon: '📁' },
  { name: 'Report', icon: '📊' },
]

const DEFAULT_ROLE_NAME = 'Responsabile App'

export default function RuoliPage() {
  const [roles, setRoles] = useState<MerchantRole[]>([])
  const [loading, setLoading] = useState(true)
  const [savingId, setSavingId] = useState<number | null>(null)
  const [showModal, setShowModal] = useState(false)
  const [newRoleName, setNewRoleName] = useState('')
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState('')
  const [localFeatures, setLocalFeatures] = useState<Record<number, string[]>>({})

  const fetchRoles = async () => {
    setLoading(true)
    try {
      const res = await apiClient.get('/merchant-roles')
      if (Array.isArray(res.data)) {
        const data = res.data as MerchantRole[]
        setRoles(data)
        const fm: Record<number, string[]> = {}
        data.forEach(r => { fm[r.id] = r.features ?? [] })
        setLocalFeatures(fm)
      }
    } catch {
      setRoles([])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { fetchRoles() }, [])

  const toggleFeature = (roleId: number, feature: string) => {
    setLocalFeatures(prev => {
      const current = prev[roleId] ?? []
      const updated = current.includes(feature)
        ? current.filter(f => f !== feature)
        : [...current, feature]
      return { ...prev, [roleId]: updated }
    })
  }

  const handleSaveRole = async (role: MerchantRole) => {
    setSavingId(role.id)
    try {
      await apiClient.put(`/merchant-roles/${role.id}`, {
        name: role.name,
        features: localFeatures[role.id] ?? [],
      })
    } catch {
      alert('Errore durante il salvataggio')
    } finally {
      setSavingId(null)
    }
  }

  const handleDeleteRole = async (role: MerchantRole) => {
    if (role.name === DEFAULT_ROLE_NAME || role.isDefault) return
    if (!confirm(`Eliminare il ruolo "${role.name}"?`)) return
    try {
      await apiClient.delete(`/merchant-roles/${role.id}`)
      await fetchRoles()
    } catch {
      alert('Errore durante l\'eliminazione')
    }
  }

  const handleCreateRole = async (e: FormEvent) => {
    e.preventDefault()
    if (!newRoleName.trim()) { setCreateError('Il nome è obbligatorio'); return }
    setCreating(true)
    setCreateError('')
    try {
      await apiClient.post('/merchant-roles', { name: newRoleName.trim(), features: [] })
      setShowModal(false)
      setNewRoleName('')
      await fetchRoles()
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setCreateError(e.response?.data?.message ?? 'Errore durante la creazione')
    } finally {
      setCreating(false)
    }
  }

  const isDefaultRole = (role: MerchantRole) => role.name === DEFAULT_ROLE_NAME || role.isDefault

  return (
    <div className="ruoli-page">
      <div className="page-header-row">
        <div>
          <h1 className="page-title">Ruoli</h1>
          <p className="page-subtitle">Configura ruoli e funzionalità accessibili</p>
        </div>
        <button className="btn-primary" onClick={() => { setNewRoleName(''); setCreateError(''); setShowModal(true) }}>
          + Nuovo Ruolo
        </button>
      </div>

      {loading ? (
        <div className="loading-text">Caricamento ruoli...</div>
      ) : (
        <div className="roles-grid">
          {roles.map(role => (
            <div key={role.id} className="role-card">
              <div className="role-card-header">
                <div className="role-icon">🔑</div>
                <span className="role-name">{role.name}</span>
                {isDefaultRole(role) && (
                  <span className="role-default-badge">Predefinito</span>
                )}
              </div>
              <div className="role-card-body">
                <div className="features-label">Funzionalità</div>
                {ALL_FEATURES.map(feat => (
                  <div key={feat.name} className="feature-toggle-row">
                    <span className="feature-name">
                      <span className="feature-icon">{feat.icon}</span>
                      {feat.name}
                    </span>
                    <label className="toggle-switch">
                      <input
                        type="checkbox"
                        checked={(localFeatures[role.id] ?? []).includes(feat.name)}
                        onChange={() => toggleFeature(role.id, feat.name)}
                        disabled={isDefaultRole(role)}
                      />
                      <span className="toggle-slider" />
                    </label>
                  </div>
                ))}
              </div>
              <div className="role-card-footer">
                <button
                  className="btn-delete-role"
                  onClick={() => handleDeleteRole(role)}
                  disabled={isDefaultRole(role)}
                  title={isDefaultRole(role) ? 'Il ruolo predefinito non può essere eliminato' : ''}
                >
                  Elimina
                </button>
                <button
                  className="btn-save-role"
                  onClick={() => handleSaveRole(role)}
                  disabled={savingId === role.id || isDefaultRole(role)}
                >
                  {savingId === role.id ? 'Salvataggio...' : 'Salva'}
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {showModal && (
        <div className="modal-overlay" onClick={e => { if (e.target === e.currentTarget) setShowModal(false) }}>
          <div className="modal-box">
            <div className="modal-header">
              <h2 className="modal-title">Nuovo Ruolo</h2>
              <button className="modal-close" onClick={() => setShowModal(false)}>✕</button>
            </div>
            <form onSubmit={handleCreateRole}>
              <div className="modal-body">
                {createError && <div className="modal-error">{createError}</div>}
                <div className="form-group">
                  <label className="form-label">Nome ruolo *</label>
                  <input
                    type="text"
                    className="form-input"
                    placeholder="Es. Responsabile Turni"
                    value={newRoleName}
                    onChange={e => setNewRoleName(e.target.value)}
                    required
                    autoFocus
                  />
                </div>
              </div>
              <div className="modal-footer">
                <button type="button" className="btn-cancel" onClick={() => setShowModal(false)}>Annulla</button>
                <button type="submit" className="btn-primary" disabled={creating}>
                  {creating ? 'Creazione...' : 'Crea Ruolo'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
