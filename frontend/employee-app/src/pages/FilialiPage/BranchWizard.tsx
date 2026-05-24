import { useState } from 'react'
import { branchesApi } from '../../lib/api/branches'
import './BranchWizard.css'

interface BranchWizardProps {
  onClose: () => void
  onDone: () => void | Promise<void>
}

const DEPT_PRESETS = ['Produzione', 'Amministrazione', 'Magazzino', 'Vendita', 'Cassa']
const DEPT_COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899']

/**
 * Wizard A — onboarding multi-filiale. Pochi step, ogni passaggio opzionale è
 * skippabile. Per un merchant che dichiara "una sola sede" il wizard si chiude
 * subito senza creare nulla.
 */
export default function BranchWizard({ onClose, onDone }: BranchWizardProps) {
  const [step, setStep] = useState(1)
  const [name, setName] = useState('')
  const [city, setCity] = useState('')
  const [departments, setDepartments] = useState<string[]>([])
  const [customDept, setCustomDept] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const toggleDept = (d: string) => {
    setDepartments(prev => prev.includes(d) ? prev.filter(x => x !== d) : [...prev, d])
  }

  const addCustomDept = () => {
    const v = customDept.trim()
    if (v && !departments.includes(v)) setDepartments(prev => [...prev, v])
    setCustomDept('')
  }

  const finish = async () => {
    setSaving(true)
    setError('')
    let branchCreated = false
    try {
      const branch = await branchesApi.create({ name: name.trim(), city: city.trim() || null })
      branchCreated = true
      for (let i = 0; i < departments.length; i++) {
        await branchesApi.createDepartment(branch.id, {
          name: departments[i],
          color: DEPT_COLORS[i % DEPT_COLORS.length],
        })
      }
      await onDone()
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      // Se la filiale è già stata creata ma un reparto è fallito, l'operazione è
      // riuscita parzialmente: chiudi comunque (la FilialiPage mostrerà la filiale,
      // i reparti mancanti si aggiungono a mano) per non creare filiali duplicate.
      if (branchCreated) {
        await onDone()
        return
      }
      setError(e.response?.data?.message ?? 'Errore durante la creazione')
      setSaving(false)
    }
  }

  return (
    <div className="wizard-overlay" onClick={e => { if (e.target === e.currentTarget) onClose() }}>
      <div className="wizard-box">
        <div className="wizard-header">
          <h2 className="wizard-title">Configura le filiali</h2>
          <button className="modal-close" onClick={onClose}>✕</button>
        </div>

        <div className="wizard-steps">
          {[1, 2, 3].map(s => (
            <div key={s} className={`wizard-step-dot ${step >= s ? 'active' : ''}`}>{s}</div>
          ))}
        </div>

        <div className="wizard-body">
          {error && <div className="modal-error">{error}</div>}

          {step === 1 && (
            <div className="wizard-stage">
              <h3 className="wizard-q">La tua azienda ha più punti vendita?</h3>
              <p className="wizard-hint">
                Se hai una sola sede puoi continuare a usare l'app come prima.
              </p>
              <div className="wizard-choice-row">
                <button className="wizard-choice" onClick={() => setStep(2)}>
                  <span className="wizard-choice-icon">🏬</span>
                  <span className="wizard-choice-title">Sì, ho più sedi</span>
                  <span className="wizard-choice-sub">Aggiungiamo la prima filiale</span>
                </button>
                <button className="wizard-choice" onClick={onClose}>
                  <span className="wizard-choice-icon">🏢</span>
                  <span className="wizard-choice-title">No, una sola sede</span>
                  <span className="wizard-choice-sub">Chiudi la configurazione</span>
                </button>
              </div>
            </div>
          )}

          {step === 2 && (
            <div className="wizard-stage">
              <h3 className="wizard-q">Aggiungi la prima filiale</h3>
              <div className="form-group">
                <label className="form-label">Nome filiale *</label>
                <input
                  type="text" className="form-input" placeholder="es. Milano Centro"
                  value={name} autoFocus
                  onChange={e => setName(e.target.value)}
                />
              </div>
              <div className="form-group">
                <label className="form-label">Città</label>
                <input
                  type="text" className="form-input" placeholder="es. Milano"
                  value={city}
                  onChange={e => setCity(e.target.value)}
                />
              </div>
              <div className="wizard-nav">
                <button className="btn-cancel" onClick={() => setStep(1)}>Indietro</button>
                <button
                  className="btn-primary"
                  disabled={!name.trim()}
                  onClick={() => setStep(3)}
                >
                  Avanti
                </button>
              </div>
            </div>
          )}

          {step === 3 && (
            <div className="wizard-stage">
              <h3 className="wizard-q">Reparti della filiale <span className="wizard-optional">(opzionale)</span></h3>
              <p className="wizard-hint">
                I reparti suddividono i turni per area (es. Produzione su turni, Amministrazione 9–18).
                Puoi saltare questo passaggio.
              </p>
              <div className="wizard-preset-chips">
                {DEPT_PRESETS.map(d => (
                  <button
                    key={d}
                    type="button"
                    className={`wizard-preset-chip ${departments.includes(d) ? 'selected' : ''}`}
                    onClick={() => toggleDept(d)}
                  >
                    {departments.includes(d) ? '✓ ' : '+ '}{d}
                  </button>
                ))}
              </div>
              <div className="wizard-custom-row">
                <input
                  type="text" className="form-input" placeholder="Aggiungi un reparto…"
                  value={customDept}
                  onChange={e => setCustomDept(e.target.value)}
                  onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); addCustomDept() } }}
                />
                <button type="button" className="btn-ghost" onClick={addCustomDept}>Aggiungi</button>
              </div>
              {departments.length > 0 && (
                <div className="wizard-selected-depts">
                  {departments.map(d => (
                    <span key={d} className="wizard-dept-tag">
                      {d}
                      <button onClick={() => toggleDept(d)}>✕</button>
                    </span>
                  ))}
                </div>
              )}
              <div className="wizard-nav">
                <button className="btn-cancel" onClick={() => setStep(2)}>Indietro</button>
                <div className="wizard-nav-right">
                  <button className="btn-ghost" disabled={saving} onClick={finish}>
                    Salta i reparti
                  </button>
                  <button className="btn-primary" disabled={saving} onClick={finish}>
                    {saving ? 'Creazione…' : 'Crea filiale'}
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
