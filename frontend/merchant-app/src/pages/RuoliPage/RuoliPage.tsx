import { useState, useEffect, FormEvent } from 'react'
import { EmptyState, PageHeader, Skeleton, useConfirm, useToast } from '@scheduler/ui'
import apiClient from '../../lib/axios'
import './RuoliPage.css'

interface Feature {
  name: string
  icon: string
  value: number
}

// Livelli di accesso operativo. Significativi per le feature a livelli
// (Calendario, Documenti, Timbratura e Magazzino). Valori numerici allineati all'enum C#
// FeatureAccessLevel: l'API serializza e deserializza gli enum come numeri.
const enum FeatureAccessLevel {
  ReadOnly = 1,
  Operator = 2,
  Manager = 3,
}

// Valori feature (allineati all'enum C# MerchantFeature).
const FEATURE_DOCUMENTI = 5
const FEATURE_TIMBRATURA = 9
const FEATURE_MAGAZZINO = 10
const FEATURE_RICHIESTE = 2

// Feature che usano i livelli di accesso, con le label dei rispettivi livelli.
// Le altre feature restano un semplice toggle on/off.
const LEVELED_FEATURES: Record<number, Array<{ value: FeatureAccessLevel; label: string }>> = {
  [1]: [
    { value: FeatureAccessLevel.ReadOnly, label: 'Sola lettura (riepilogo turni)' },
    { value: FeatureAccessLevel.Operator, label: 'Operatore (assegna personale ai turni esistenti)' },
    { value: FeatureAccessLevel.Manager, label: 'Manager (modifica e cancellazione turni)' },
  ],
  [FEATURE_RICHIESTE]: [
    { value: FeatureAccessLevel.ReadOnly, label: 'Base (richiede e vede solo le proprie richieste)' },
    { value: FeatureAccessLevel.Operator, label: 'Operatore (gestisce anche le richieste degli altri)' },
    { value: FeatureAccessLevel.Manager, label: 'Manager (stessi permessi operativi dell\'operatore)' },
  ],
  [FEATURE_DOCUMENTI]: [
    { value: FeatureAccessLevel.ReadOnly, label: 'Sola lettura (scarica i propri documenti)' },
    { value: FeatureAccessLevel.Operator, label: 'Operatore (carica documenti per altre risorse)' },
    { value: FeatureAccessLevel.Manager, label: 'Manager (anche cancellazione documenti)' },
  ],
  [FEATURE_TIMBRATURA]: [
    { value: FeatureAccessLevel.ReadOnly, label: 'Sola lettura (monitoraggio presenze)' },
    { value: FeatureAccessLevel.Operator, label: 'Operatore (inserimento correzioni manuali)' },
    { value: FeatureAccessLevel.Manager, label: 'Manager (configurazione e approvazione anomalie)' },
  ],
  [FEATURE_MAGAZZINO]: [
    { value: FeatureAccessLevel.ReadOnly, label: 'Sola lettura' },
    { value: FeatureAccessLevel.Operator, label: 'Operatore (rettifiche, ricezioni)' },
    { value: FeatureAccessLevel.Manager, label: 'Manager (anche anagrafiche e ordini)' },
  ],
}

const isLeveledFeature = (featureValue: number) => featureValue in LEVELED_FEATURES

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
  { name: 'Documenti', icon: '📁', value: FEATURE_DOCUMENTI },
  { name: 'Report', icon: '📊', value: 6 },
  { name: 'Mansioni', icon: '🏷', value: 7 },
  { name: 'Filiali', icon: '🏢', value: 8 },
  { name: 'Timbratura', icon: '⏱', value: FEATURE_TIMBRATURA },
  { name: 'Magazzino', icon: '📦', value: FEATURE_MAGAZZINO },
]

const DEFAULT_ROLE_NAME = 'Responsabile App'

// Livelli di accesso indicizzati per ruolo e per feature.
type RoleFeatureLevels = Record<number, Record<number, FeatureAccessLevel>>

export default function RuoliPage() {
  const toast = useToast()
  const confirm = useConfirm()
  const [roles, setRoles] = useState<MerchantRole[]>([])
  const [loading, setLoading] = useState(true)
  const [savingId, setSavingId] = useState<number | null>(null)
  const [showModal, setShowModal] = useState(false)
  const [newRoleName, setNewRoleName] = useState('')
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState('')
  // localFeatures: roleId -> array of enabled feature enum values (numbers)
  const [localFeatures, setLocalFeatures] = useState<Record<number, number[]>>({})
  // featureLevels: roleId -> { featureValue -> livello } per le feature a livelli.
  const [featureLevels, setFeatureLevels] = useState<RoleFeatureLevels>({})

  const fetchRoles = async () => {
    setLoading(true)
    try {
      const res = await apiClient.get('/merchant-roles')
      if (Array.isArray(res.data)) {
        const data = res.data as MerchantRole[]
        setRoles(data)
        const fm: Record<number, number[]> = {}
        const lm: RoleFeatureLevels = {}
        data.forEach(r => {
          fm[r.id] = r.features.filter(f => f.isEnabled).map(f => f.feature)
          // Default sicuro: una feature a livelli abilitata senza livello è ReadOnly.
          const levels: Record<number, FeatureAccessLevel> = {}
          r.features.forEach(f => {
            if (isLeveledFeature(f.feature)) {
              levels[f.feature] = f.accessLevel ?? FeatureAccessLevel.ReadOnly
            }
          })
          lm[r.id] = levels
        })
        setLocalFeatures(fm)
        setFeatureLevels(lm)
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
    // Abilitando una feature a livelli senza un livello già scelto, parte da ReadOnly.
    if (isLeveledFeature(featureValue)) {
      setFeatureLevels(prev => {
        const roleLevels = prev[roleId] ?? {}
        return {
          ...prev,
          [roleId]: {
            ...roleLevels,
            [featureValue]: roleLevels[featureValue] ?? FeatureAccessLevel.ReadOnly,
          },
        }
      })
    }
  }

  const setFeatureLevel = (roleId: number, featureValue: number, level: FeatureAccessLevel) => {
    setFeatureLevels(prev => ({
      ...prev,
      [roleId]: { ...(prev[roleId] ?? {}), [featureValue]: level },
    }))
  }

  /** Costruisce il payload feature, includendo il livello per le feature a livelli. */
  const buildFeaturesPayload = (roleId: number, enabledValues: number[]) =>
    ALL_FEATURES.map(feat => {
      const isEnabled = enabledValues.includes(feat.value)
      const base = { feature: feat.value, isEnabled }
      if (isLeveledFeature(feat.value) && isEnabled) {
        const level = featureLevels[roleId]?.[feat.value] ?? FeatureAccessLevel.ReadOnly
        return { ...base, accessLevel: level }
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
      toast.success('Ruolo aggiornato')
    } catch {
      toast.error('Errore durante il salvataggio')
    } finally {
      setSavingId(null)
    }
  }

  const handleDeleteRole = async (role: MerchantRole) => {
    if (role.name === DEFAULT_ROLE_NAME || role.isDefault) return
    const ok = await confirm({
      title: 'Eliminare ruolo',
      message: `Eliminare il ruolo "${role.name}"?`,
      variant: 'danger',
      confirmLabel: 'Elimina',
    })
    if (!ok) return
    try {
      await apiClient.delete(`/merchant-roles/${role.id}`)
      toast.success('Ruolo eliminato')
      await fetchRoles()
    } catch {
      toast.error('Errore durante l\'eliminazione')
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
      <PageHeader
        title="Ruoli"
        subtitle="Configura ruoli e funzionalità accessibili"
        actions={
          <button className="btn-primary" onClick={() => { setNewRoleName(''); setCreateError(''); setShowModal(true) }}>
            + Nuovo Ruolo
          </button>
        }
      />

      {loading ? (
        <div className="roles-grid">
          {[0, 1, 2].map(i => (
            <div key={i} className="role-card">
              <Skeleton variant="text" width="60%" />
              <Skeleton variant="box" height={180} />
              <Skeleton variant="text" width="40%" />
            </div>
          ))}
        </div>
      ) : roles.length === 0 ? (
        <EmptyState
          title="Nessun ruolo configurato"
          description="Crea il primo ruolo per assegnare permessi e funzionalità ai dipendenti."
          action={{
            label: '+ Nuovo Ruolo',
            onClick: () => { setNewRoleName(''); setCreateError(''); setShowModal(true) },
          }}
        />
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
                  const showLevelSelector = isLeveledFeature(feat.value) && isEnabled
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
                            value={featureLevels[role.id]?.[feat.value] ?? FeatureAccessLevel.ReadOnly}
                            onChange={e => setFeatureLevel(role.id, feat.value, Number(e.target.value) as FeatureAccessLevel)}
                            disabled={isDefaultRole(role)}
                          >
                            {LEVELED_FEATURES[feat.value].map(level => (
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
