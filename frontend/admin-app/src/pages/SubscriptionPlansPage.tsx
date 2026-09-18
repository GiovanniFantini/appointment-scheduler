import { useEffect, useState, type FormEvent } from 'react'
import { useConfirm } from '@scheduler/ui'
import apiClient from '../lib/axios'
import { PLAN_FEATURES, subscriptionError, type SubscriptionPlan } from './subscriptionPlans'
import './SubscriptionPlansPage.css'

const emptyPlan = (): SubscriptionPlan => ({ id: 0, name: '', description: '', features: [], maxEmployees: null, maxBranches: null, maxStorageBytes: null, assignedMerchants: 0 })

export default function SubscriptionPlansPage() {
  const [plans, setPlans] = useState<SubscriptionPlan[]>([])
  const [draft, setDraft] = useState<SubscriptionPlan | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [success, setSuccess] = useState('')
  const confirm = useConfirm()
  const load = async () => {
    try { setPlans((await apiClient.get<SubscriptionPlan[]>('/admin/subscription-plans')).data) }
    catch (e) { setError(subscriptionError(e)) }
    finally { setLoading(false) }
  }
  useEffect(() => { void load() }, [])

  const save = async (event: FormEvent) => {
    event.preventDefault()
    if (!draft || saving) return
    if (draft.assignedMerchants > 0 && !await confirm({ title: 'Aggiorna il pacchetto', message: `La modifica sarà applicata a tutte le ${draft.assignedMerchants} aziende assegnate, comprese quelle in prova. Le funzioni escluse non saranno più accessibili. I dati saranno conservati.`, confirmLabel: 'Applica a tutte' })) return
    setSaving(true); setError(''); setSuccess('')
    try {
      const { id, assignedMerchants: _assigned, ...payload } = draft
      if (id) await apiClient.put(`/admin/subscription-plans/${id}`, payload)
      else await apiClient.post('/admin/subscription-plans', payload)
      setDraft(null)
      setSuccess('Pacchetto salvato. Le modifiche sono attive per tutte le aziende assegnate.')
      await load()
    } catch (e) { setError(subscriptionError(e)) }
    finally { setSaving(false) }
  }

  return <div className="plans-page">
    <header className="plans-heading"><div><p className="plans-eyebrow">CATALOGO COMMERCIALE</p><h1>Pacchetti</h1><p>Funzioni e capacità per ogni azienda. Crea un pacchetto dedicato per le configurazioni personalizzate.</p></div>
      <button data-activity="admin.pages.SubscriptionPlansPage.1" className="btn-primary" disabled={saving} onClick={() => { setDraft(emptyPlan()); setError(''); setSuccess('') }}>Nuovo pacchetto</button></header>
    {error && <div className="error-banner" role="alert">{error} <button data-activity="admin.pages.SubscriptionPlansPage.2" onClick={() => { setError(''); void load() }}>Riprova</button></div>}
    {success && <div className="success-banner" role="status">{success}</div>}
    {loading ? <p>Caricamento pacchetti…</p> : <div className="plans-catalog">
      {plans.map(plan => <article className="plan-card" key={plan.id}>
        <div className="plan-card-heading"><h2>{plan.name}</h2><span>{plan.assignedMerchants} aziende</span></div>
        {plan.description && <p>{plan.description}</p>}
        <div className="plan-tags">{plan.features.map(feature => <span key={feature}>{PLAN_FEATURES[feature - 1]}</span>)}{plan.features.length === 0 && <span>Nessun modulo</span>}</div>
        <dl className="plan-limits"><div><dt>Dipendenti attivi</dt><dd>{plan.maxEmployees ?? 'Illimitati'}</dd></div><div><dt>Filiali attive</dt><dd>{plan.maxBranches ?? 'Illimitate'}</dd></div><div><dt>Documenti</dt><dd>{plan.maxStorageBytes == null ? 'Spazio illimitato' : `${plan.maxStorageBytes / 1048576} MiB`}</dd></div></dl>
        <button data-activity="admin.pages.SubscriptionPlansPage.3" className="btn-secondary" disabled={saving} onClick={() => { setDraft({ ...plan, features: [...plan.features] }); setError(''); setSuccess('') }}>Modifica pacchetto</button>
      </article>)}
      {plans.length === 0 && <p>Nessun pacchetto. Crea il primo per assegnarlo alle aziende.</p>}
    </div>}
    {draft && <form data-activity="admin.pages.SubscriptionPlansPage.4" className="plan-editor" onSubmit={save}>
      <h2>{draft.id ? `Modifica: ${draft.name}` : 'Nuovo pacchetto'}</h2>
      <fieldset disabled={saving}>
        <div className="plan-fields"><label>Nome<input data-activity="admin.pages.SubscriptionPlansPage.5" required maxLength={100} value={draft.name} onChange={e => setDraft({ ...draft, name: e.target.value })} /></label>
          <label>Descrizione<input data-activity="admin.pages.SubscriptionPlansPage.6" maxLength={1000} value={draft.description ?? ''} onChange={e => setDraft({ ...draft, description: e.target.value })} /></label></div>
        <h3>Funzioni incluse</h3><div className="plan-feature-grid">{PLAN_FEATURES.map((feature, i) => <label key={feature}>
          <input data-activity="admin.pages.SubscriptionPlansPage.7" type="checkbox" checked={draft.features.includes(i + 1)} onChange={e => setDraft({ ...draft, features: e.target.checked ? [...draft.features, i + 1] : draft.features.filter(f => f !== i + 1) })} />{feature}</label>)}</div>
        <p className="plan-note">Il calendario conserva l’accesso ai dati necessari per assegnare i turni. I ruoli stabiliscono i permessi delle persone entro le funzioni incluse.</p>
        <h3>Limiti quantitativi</h3><p className="plan-note">Lascia il campo vuoto per un limite illimitato. I dipendenti comprendono interni ed esterni attivi; lo spazio include tutte le versioni dei documenti non eliminati.</p>
        <div className="plan-fields">
          <label>Dipendenti attivi<input data-activity="admin.pages.SubscriptionPlansPage.8" type="number" min={0} max={2147483647} step={1} placeholder="Illimitati" value={draft.maxEmployees ?? ''} onChange={e => setDraft({ ...draft, maxEmployees: e.target.value === '' ? null : Number(e.target.value) })} /></label>
          <label>Filiali attive<input data-activity="admin.pages.SubscriptionPlansPage.9" type="number" min={1} max={2147483647} step={1} placeholder="Illimitate" value={draft.maxBranches ?? ''} onChange={e => setDraft({ ...draft, maxBranches: e.target.value === '' ? null : Number(e.target.value) })} /></label>
          <label>Spazio documenti (MiB)<input data-activity="admin.pages.SubscriptionPlansPage.10" type="number" min={0} max={8589934591} step={1} placeholder="Illimitato" value={draft.maxStorageBytes == null ? '' : draft.maxStorageBytes / 1048576} onChange={e => setDraft({ ...draft, maxStorageBytes: e.target.value === '' ? null : Number(e.target.value) * 1048576 })} /></label>
        </div>
        {draft.assignedMerchants > 0 && <p className="plan-impact">Questa modifica coinvolge {draft.assignedMerchants} aziende. Se un nuovo limite è inferiore all’utilizzo, saranno bloccate solo le nuove aggiunte.</p>}
        <div className="plan-actions"><button data-activity="admin.pages.SubscriptionPlansPage.11" className="btn-primary" type="submit">{saving ? 'Salvataggio…' : 'Salva pacchetto'}</button><button data-activity="admin.pages.SubscriptionPlansPage.12" className="btn-secondary" type="button" onClick={() => setDraft(null)}>Annulla</button></div>
      </fieldset>
    </form>}
  </div>
}
