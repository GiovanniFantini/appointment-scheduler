import { useEffect, useState } from 'react'
import apiClient from '../lib/axios'
import './MerchantFeaturesTab.css'

// Allineato a backend/AppointmentScheduler.Shared/Enums/MerchantFeature.cs
const FEATURES: { id: number; key: string; label: string; description: string }[] = [
  { id: 1, key: 'Calendario', label: 'Calendario', description: 'Calendario eventi, turni e pianificazione' },
  { id: 2, key: 'Richieste', label: 'Richieste', description: 'Ferie, permessi, malattia, cambio turno' },
  { id: 3, key: 'Risorse', label: 'Risorse', description: 'Risorse aziendali (sale, attrezzature)' },
  { id: 4, key: 'Ruoli', label: 'Ruoli', description: 'Gestione ruoli e permessi del merchant' },
  { id: 5, key: 'Documenti', label: 'Documenti', description: 'Archivio documenti HR su Azure Blob' },
  { id: 6, key: 'Report', label: 'Report', description: 'Report e analytics di sede' },
  { id: 7, key: 'Mansioni', label: 'Mansioni', description: 'Skill / mansioni dipendenti' },
  { id: 8, key: 'Filiali', label: 'Filiali', description: 'Gestione filiali e reparti' },
  { id: 9, key: 'Timbratura', label: 'Timbratura', description: 'Time clock, anomalie, geofencing' },
  { id: 10, key: 'Magazzino', label: 'Magazzino', description: 'Inventario, fornitori, ordini' },
]

// Feature che usano AccessLevel (vedi AuthService.ResolveAccessLevel).
const LEVELED_FEATURES = new Set([1, 2, 5, 9, 10]) // Calendario, Richieste, Documenti, Timbratura, Magazzino

type AccessLevel = 1 | 2 | 3 // ReadOnly, Operator, Manager
const ACCESS_LEVEL_LABELS: Record<AccessLevel, string> = {
  1: 'ReadOnly',
  2: 'Operator',
  3: 'Manager',
}

interface FeatureState {
  feature: number
  isEnabled: boolean
  accessLevel: AccessLevel | null
}

interface FeatureResponseItem {
  feature: number
  isEnabled: boolean
  accessLevel: AccessLevel | null
}

interface Props {
  merchantId: number
}

export default function MerchantFeaturesTab({ merchantId }: Props) {
  const [items, setItems] = useState<FeatureState[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)
  const [saveSuccess, setSaveSuccess] = useState(false)
  const [saveError, setSaveError] = useState('')
  const [dirty, setDirty] = useState(false)

  const fetchFeatures = async () => {
    setLoading(true)
    setError('')
    try {
      const res = await apiClient.get<FeatureResponseItem[]>(`/merchants/${merchantId}/features`)
      const byId = new Map(res.data.map((f) => [f.feature, f]))
      const ordered: FeatureState[] = FEATURES.map((meta) => {
        const existing = byId.get(meta.id)
        return existing
          ? { feature: meta.id, isEnabled: existing.isEnabled, accessLevel: existing.accessLevel }
          : { feature: meta.id, isEnabled: false, accessLevel: null }
      })
      setItems(ordered)
      setDirty(false)
    } catch {
      setError('Impossibile caricare le feature del merchant.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchFeatures()
  }, [merchantId])

  const toggleEnabled = (featureId: number) => {
    setSaveSuccess(false)
    setItems((prev) => prev.map((it) => {
      if (it.feature !== featureId) return it
      const nextEnabled = !it.isEnabled
      let nextLevel = it.accessLevel
      if (LEVELED_FEATURES.has(featureId)) {
        nextLevel = nextEnabled ? (it.accessLevel ?? 1) : null
      } else {
        nextLevel = null
      }
      return { ...it, isEnabled: nextEnabled, accessLevel: nextLevel }
    }))
    setDirty(true)
  }

  const setLevel = (featureId: number, level: AccessLevel) => {
    setSaveSuccess(false)
    setItems((prev) => prev.map((it) => it.feature === featureId ? { ...it, accessLevel: level } : it))
    setDirty(true)
  }

  const save = async () => {
    setSaving(true)
    setSaveError('')
    try {
      const payload = items.map((it) => ({
        feature: it.feature,
        isEnabled: it.isEnabled,
        accessLevel: it.accessLevel,
      }))
      await apiClient.put(`/merchants/${merchantId}/features`, payload)
      await fetchFeatures()
      setSaveSuccess(true)
      setTimeout(() => setSaveSuccess(false), 3000)
    } catch {
      setSaveError('Errore durante il salvataggio.')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <div className="features-loading">Caricamento feature…</div>
  if (error) return <div className="error-banner">{error}</div>

  return (
    <div className="info-card">
      <div className="info-card-header">
        <span className="info-card-title">Feature abilitate (ruolo predefinito)</span>
        <span className="info-card-hint">
          Modifica direttamente il ruolo "Responsabile App" del merchant.
        </span>
      </div>

      {saveSuccess && <div className="success-banner features-banner">Feature aggiornate.</div>}
      {saveError && <div className="error-banner features-banner">{saveError}</div>}

      <div className="features-list">
        {FEATURES.map((meta) => {
          const item = items.find((i) => i.feature === meta.id)
          if (!item) return null
          const leveled = LEVELED_FEATURES.has(meta.id)
          return (
            <div key={meta.id} className="feature-row">
              <div className="feature-info">
                <div className="feature-name">{meta.label}</div>
                <div className="feature-desc">{meta.description}</div>
              </div>
              <div className="feature-controls">
                {leveled && item.isEnabled && (
                  <select
                    className="feature-level"
                    value={item.accessLevel ?? 1}
                    onChange={(e) => setLevel(meta.id, Number(e.target.value) as AccessLevel)}
                  >
                    {([1, 2, 3] as AccessLevel[]).map((lv) => (
                      <option key={lv} value={lv}>{ACCESS_LEVEL_LABELS[lv]}</option>
                    ))}
                  </select>
                )}
                <label className="toggle">
                  <input
                    type="checkbox"
                    checked={item.isEnabled}
                    onChange={() => toggleEnabled(meta.id)}
                  />
                  <span className="toggle-slider" aria-hidden="true" />
                </label>
              </div>
            </div>
          )
        })}
      </div>

      <div className="features-footer">
        <button className="btn-primary" onClick={save} disabled={!dirty || saving}>
          {saving ? 'Salvataggio…' : 'Salva modifiche'}
        </button>
        {dirty && <span className="features-dirty-hint">Modifiche non salvate</span>}
      </div>
    </div>
  )
}
