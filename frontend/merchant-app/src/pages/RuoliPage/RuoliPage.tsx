import { useState, useEffect, FormEvent } from 'react'
import apiClient from '../../lib/axios'
import './RuoliPage.css'

interface Feature {
  name: string
  icon: string
  value: number
}

// Livelli di accesso operativo. Significativi solo per la feature Magazzino.
// Valori numerici allineati all'enum C# FeatureAccessLevel: l'API serializza
// e deserializza gli enum come numeri.
const enum FeatureAccessLevel {
  ReadOnly = 1,
  Operator = 2,
  Manager = 3,
}

const MAGAZZINO_FEATURE_VALUE = 10

const MAGAZZINO_LEVELS: Array<{ value: FeatureAccessLevel; label: string }> = [
  { value: FeatureAccessLevel.ReadOnly, label: 'Sola lettura' },
  { value: FeatureAccessLevel.Operator, label: 'Operatore (rettifiche, ricezioni)' },
  { value: FeatureAccessLevel.Manager, label: 'Manager (anche anagrafiche e ordini)' },
]

interface RoleFeatureDto {
  feature: number
  featureName: string
  isEnabled: boolean
  accessLevel?: FeatureAccessLevel | null
}

interface MerchantRole {
  id: number
  name: string
  isDefault?: boolean
  features: RoleFeatureDto[]
  memberCount?: number
}

const ALL_FEATURES: Feature[] = [
  { name: 'Calendario', icon: '📅', value: 1 },
  { name: 'Richieste', icon: '📋', value: 2 },
  { name: 'Risorse', icon: '👥', value: 3 },
  { name: 'Ruoli', icon: '🔑', value: 4 },
  { name: 'Documenti', icon: '📁', value: 5 },
  { name: 'Report', icon: '📊', value: 6 },
  { name: 'Mansioni', icon: '🏷', value: 7 },
  { name: 'Filiali', icon: '🏢', value: 8 },
  { name: 'Timbratura', icon: '⏱', value: 9 },
  { name: 'Magazzino', icon: '📦', value: 10 },
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
  // localFeatures: roleId -> array of enabled feature enum values (numbers)
  const [localFeatures, setLocalFeatures] = useState<Record<number, number[]>>({})
  // magazzinoLevels: roleId -> livello di accesso al Magazzino (se abilitato)
  const [magazzinoLevels, setMagazzinoLevels] = useState<Record<number, FeatureAccessLevel>>({})

  const fetchRoles = async () => {
    setLoading(true)
    try {
      const res = await apiClient.get('/merchant-roles')
      if (Array.isArray(res.data)) {
        const data = res.data as MerchantRole[]
        setRoles(data)
        const fm: Record<number, number[]> = {}
        const lm: Record<number, FeatureAccessLevel> = {}
        data.forEach(r => {
          fm[r.id] = r.features.filter(f => f.isEnabled).map(f => f.feature)
          const magazzino = r.features.find(f => f.feature === MAGAZZINO_FEATURE_VALUE)
          // Default sicuro: una feature Magazzino abilitata senza livello è ReadOnly.
          lm[r.id] = magazzino?.accessLevel ?? FeatureAccessLevel.ReadOnly
        })
        setLocalFeatures(fm)
        setMagazzinoLevels(lm)
      }
    } catch {
      setRoles([])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { fetchRoles() }, [])

  const toggleFeature = (roleId: number, featureValue: number) => {
    setLocalFeatures(prev => {
      const current = prev[roleId] ?? []
      const updated = current.includes(featureValue)
        ? current.filter(f => f !== featureValue)
        : [...current, featureValue]
      return { ...prev, [roleId]: updated }
    })
    // Abilitando il Magazzino senza un livello già scelto, parte da ReadOnly.
    if (featureValue === MAGAZZINO_FEATURE_VALUE) {
      setMagazzinoLevels(prev => ({ ...prev, [roleId]: prev[roleId] ?? FeatureAccessLevel.ReadOnly }))
    }
  }

  const setMagazzinoLevel = (roleId: number, level: FeatureAccessLevel) => {
    setMagazzinoLevels(prev => ({ ...prev, [roleId]: level }))
  }

  /** Costruisce il payload feature, includendo il livello per il Magazzino. */
  const buildFeaturesPayload = (roleId: number, enabledValues: number[]) =>
    ALL_FEATURES.map(feat => {
      const isEnabled = enabledValues.includes(feat.value)
      const base = { feature: feat.value, isEnabled }
      if (feat.value === MAGAZZINO_FEATURE_VALUE && isEnabled) {
        return { ...base, accessLevel: magazzinoLevels[roleId] ?? FeatureAccessLevel.ReadOnly }
      }
      return base
    })

  const handleSaveRole = async (role: MerchantRole) => {
    setSavingId(role.id)
    try {
      const enabledValues = localFeatures[role.id] ?? []
      await apiClient.put(`/merchant-roles/${role.id}`, {
        name: role.name,
        features: buildFeaturesPayload(role.id, enabledValues),
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
      await apiClient.post('/merchant-roles', {
        name: newRoleName.trim(),
        features: ALL_FEATURES.map(feat => ({ feature: feat.value, isEnabled: false })),
      })
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
                {ALL_FEATURES.map(feat => {
                  const isEnabled = (localFeatures[role.id] ?? []).includes(feat.value)
                  const showLevelSelector = feat.value === MAGAZZINO_FEATURE_VALUE && isEnabled
                  return (
                    <div key={feat.name}>
                      <div className="feature-toggle-row">
                        <span className="feature-name">
                          <span className="feature-icon">{feat.icon}</span>
                          {feat.name}
                        </span>
                        <label className="toggle-switch">
                          <input
                            type="checkbox"
                            checked={isEnabled}
                            onChange={() => toggleFeature(role.id, feat.value)}
                            disabled={isDefaultRole(role)}
                          />
                          <span className="toggle-slider" />
                        </label>
                      </div>
                      {showLevelSelector && (
                        <div className="feature-level-row">
                          <span className="feature-level-label">Livello di accesso</span>
                          <select
                            className="feature-level-select"
                            value={magazzinoLevels[role.id] ?? FeatureAccessLevel.ReadOnly}
                            onChange={e => setMagazzinoLevel(role.id, Number(e.target.value) as FeatureAccessLevel)}
                            disabled={isDefaultRole(role)}
                          >
                            {MAGAZZINO_LEVELS.map(level => (
                              <option key={level.value} value={level.value}>{level.label}</option>
                            ))}
                          </select>
                        </div>
                      )}
                    </div>
                  )
                })}
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
