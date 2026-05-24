import { useEffect, useState, FormEvent } from 'react'
import { skillsApi, Skill } from '../../lib/api/skills'
import './MansioniPage.css'

const PRESET_COLORS = [
  '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6',
  '#ec4899', '#06b6d4', '#84cc16', '#f97316', '#6366f1'
]

interface FormState {
  name: string
  color: string
  isActive: boolean
}

const emptyForm: FormState = { name: '', color: PRESET_COLORS[0], isActive: true }

export default function MansioniPage() {
  const [skills, setSkills] = useState<Skill[]>([])
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [editing, setEditing] = useState<Skill | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [drawerSkill, setDrawerSkill] = useState<Skill | null>(null)
  const [drawerEmployees, setDrawerEmployees] = useState<Array<{ id: number; firstName: string; lastName: string; email: string }>>([])
  const [drawerLoading, setDrawerLoading] = useState(false)

  const fetchSkills = async () => {
    setLoading(true)
    try {
      const data = await skillsApi.list()
      setSkills(data)
    } catch {
      setSkills([])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchSkills()
  }, [])

  const openAdd = () => {
    setEditing(null)
    setForm(emptyForm)
    setError('')
    setShowModal(true)
  }

  const openEdit = (s: Skill) => {
    setEditing(s)
    setForm({ name: s.name, color: s.color, isActive: s.isActive })
    setError('')
    setShowModal(true)
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!form.name.trim()) {
      setError('Il nome è obbligatorio')
      return
    }
    setSaving(true)
    setError('')
    try {
      if (editing) {
        await skillsApi.update(editing.id, form)
      } else {
        await skillsApi.create(form)
      }
      setShowModal(false)
      await fetchSkills()
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setError(e.response?.data?.message ?? 'Errore durante il salvataggio')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (s: Skill) => {
    if (!confirm(`Eliminare la mansione "${s.name}"?`)) return
    try {
      await skillsApi.remove(s.id)
      await fetchSkills()
      if (drawerSkill?.id === s.id) setDrawerSkill(null)
    } catch {
      alert('Errore durante l\'eliminazione')
    }
  }

  const openDrawer = async (s: Skill) => {
    setDrawerSkill(s)
    setDrawerLoading(true)
    try {
      const list = await skillsApi.getEmployees(s.id)
      setDrawerEmployees(list)
    } catch {
      setDrawerEmployees([])
    } finally {
      setDrawerLoading(false)
    }
  }

  return (
    <div className="mansioni-page">
      <div className="page-header-row">
        <div>
          <h1 className="page-title">Mansioni</h1>
          <p className="page-subtitle">
            Definisci le qualifiche operative dei tuoi dipendenti (es. Cassiere, Repartista).
            Sono opzionali: usale solo se ti servono.
          </p>
        </div>
        <button className="btn-primary" onClick={openAdd}>+ Nuova mansione</button>
      </div>

      {loading ? (
        <div className="empty-state">Caricamento...</div>
      ) : skills.length === 0 ? (
        <div className="empty-state">
          <div className="empty-icon">🏷</div>
          <div className="empty-title">Nessuna mansione configurata</div>
          <div className="empty-text">
            Crea le mansioni della tua azienda per assegnarle ai dipendenti e definire il fabbisogno dei turni.
          </div>
          <button className="btn-primary" onClick={openAdd}>Crea la prima mansione</button>
        </div>
      ) : (
        <div className="skills-grid">
          {skills.map(s => (
            <div key={s.id} className={`skill-card ${!s.isActive ? 'inactive' : ''}`}>
              <div className="skill-card-top" onClick={() => openDrawer(s)}>
                <div className="skill-badge" style={{ background: s.color }}>{s.name.charAt(0).toUpperCase()}</div>
                <div className="skill-meta">
                  <div className="skill-name">{s.name}</div>
                  <div className="skill-count">{s.employeeCount} {s.employeeCount === 1 ? 'dipendente' : 'dipendenti'}</div>
                </div>
                {!s.isActive && <span className="skill-inactive-pill">disattivata</span>}
              </div>
              <div className="skill-card-actions">
                <button className="btn-edit" onClick={() => openEdit(s)}>Modifica</button>
                <button className="btn-remove" onClick={() => handleDelete(s)}>Elimina</button>
              </div>
            </div>
          ))}
        </div>
      )}

      {showModal && (
        <div className="modal-overlay" onClick={e => { if (e.target === e.currentTarget) setShowModal(false) }}>
          <div className="modal-box">
            <div className="modal-header">
              <h2 className="modal-title">{editing ? 'Modifica mansione' : 'Nuova mansione'}</h2>
              <button className="modal-close" onClick={() => setShowModal(false)}>✕</button>
            </div>
            <form onSubmit={handleSubmit}>
              <div className="modal-body">
                {error && <div className="modal-error">{error}</div>}
                <div className="form-group">
                  <label className="form-label">Nome *</label>
                  <input
                    type="text"
                    className="form-input"
                    placeholder="es. Cassiere"
                    value={form.name}
                    onChange={e => setForm(p => ({ ...p, name: e.target.value }))}
                    autoFocus
                    required
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">Colore</label>
                  <div className="color-palette">
                    {PRESET_COLORS.map(c => (
                      <button
                        key={c}
                        type="button"
                        className={`color-swatch ${form.color === c ? 'selected' : ''}`}
                        style={{ background: c }}
                        onClick={() => setForm(p => ({ ...p, color: c }))}
                        aria-label={`Colore ${c}`}
                      />
                    ))}
                  </div>
                </div>
                <div className="form-group">
                  <label className="form-checkbox">
                    <input
                      type="checkbox"
                      checked={form.isActive}
                      onChange={e => setForm(p => ({ ...p, isActive: e.target.checked }))}
                    />
                    Attiva (visibile nelle assegnazioni)
                  </label>
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

      {drawerSkill && (
        <div className="drawer-overlay" onClick={e => { if (e.target === e.currentTarget) setDrawerSkill(null) }}>
          <aside className="drawer">
            <div className="drawer-header">
              <div className="drawer-title">
                <span className="skill-badge sm" style={{ background: drawerSkill.color }}>
                  {drawerSkill.name.charAt(0).toUpperCase()}
                </span>
                <h2>{drawerSkill.name}</h2>
              </div>
              <button className="modal-close" onClick={() => setDrawerSkill(null)}>✕</button>
            </div>
            <div className="drawer-body">
              <h3>Dipendenti con questa mansione</h3>
              {drawerLoading ? (
                <div className="empty-state small">Caricamento...</div>
              ) : drawerEmployees.length === 0 ? (
                <div className="empty-state small">
                  Nessun dipendente ha ancora questa mansione.
                  <br />
                  Vai su <strong>Risorse</strong> per assegnarla.
                </div>
              ) : (
                <ul className="drawer-list">
                  {drawerEmployees.map(e => (
                    <li key={e.id}>
                      <div className="drawer-avatar">
                        {`${e.firstName?.[0] ?? ''}${e.lastName?.[0] ?? ''}`.toUpperCase()}
                      </div>
                      <div>
                        <div className="drawer-name">{e.firstName} {e.lastName}</div>
                        <div className="drawer-sub">{e.email}</div>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </aside>
        </div>
      )}
    </div>
  )
}
