import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../lib/axios'
import { PLAN_FEATURES, subscriptionError, type SubscriptionPlan } from './subscriptionPlans'
import './SubscriptionPlansPage.css'

interface Assignment {
  planId: number | null
  planName: string | null
  trialEndsAt: string | null
  status: string
  employees: number
  branches: number
  storageBytes: number
}

const statusLabels: Record<string, string> = { Unassigned: 'Da assegnare', Active: 'Attivo', Trial: 'In prova', Expired: 'Prova terminata' }

export default function MerchantSubscriptionTab({ merchantId }: { merchantId: number }) {
  return <MerchantSubscriptionEditor key={merchantId} merchantId={merchantId} />
}

function MerchantSubscriptionEditor({ merchantId }: { merchantId: number }) {
  const [plans, setPlans] = useState<SubscriptionPlan[]>([])
  const [assignment, setAssignment] = useState<Assignment | null>(null)
  const [planId, setPlanId] = useState('')
  const [trial, setTrial] = useState(false)
  const [days, setDays] = useState(14)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const base = `/admin/subscription-plans/merchants/${merchantId}`
  const load = async () => {
    try {
      const [catalog, current] = await Promise.all([apiClient.get<SubscriptionPlan[]>('/admin/subscription-plans'), apiClient.get<Assignment>(base)])
      setPlans(catalog.data); setAssignment(current.data); setPlanId(current.data.planId?.toString() ?? '')
      setTrial(current.data.status === 'Trial' || current.data.status === 'Expired')
    } catch (e) { setError(subscriptionError(e)) }
  }
  useEffect(() => { void load() }, [merchantId])
  const perform = async (action: 'assign' | 'extend' | 'activate') => {
    setSaving(true); setError(''); setSuccess('')
    try {
      if (action === 'extend') await apiClient.post(`${base}/extend-trial`, { days })
      else await apiClient.put(base, { planId: action === 'activate' ? assignment?.planId : Number(planId), trialDays: action === 'assign' && trial ? days : null })
      setSuccess('Assegnazione aggiornata.'); await load()
    } catch (e) { setError(subscriptionError(e)) }
    finally { setSaving(false) }
  }
  const selected = plans.find(p => p.id === Number(planId))
  const current = plans.find(p => p.id === assignment?.planId)
  return <section className="plan-editor">
    <div className="plan-card-heading"><h2>Pacchetto aziendale</h2><Link to="/packages">Gestisci catalogo</Link></div>
    {error && <p className="error-banner" role="alert">{error} <button onClick={() => { setError(''); void load() }}>Riprova</button></p>}
    {success && <p className="success-banner" role="status">{success}</p>}
    {assignment ? <>
      <p><strong>{assignment.planName ?? 'Nessun pacchetto'}</strong> · {statusLabels[assignment.status] ?? assignment.status}</p>
      {assignment.trialEndsAt && <p>Scadenza prova: {new Date(assignment.trialEndsAt).toLocaleString('it-IT')}</p>}
      <dl className="plan-limits"><div><dt>Dipendenti attivi</dt><dd>{assignment.employees} / {current?.maxEmployees ?? '∞'}</dd></div><div><dt>Filiali attive</dt><dd>{assignment.branches} / {current?.maxBranches ?? '∞'}</dd></div><div><dt>Spazio documenti (MiB)</dt><dd>{(assignment.storageBytes / 1048576).toFixed(1)} / {current?.maxStorageBytes == null ? '∞' : current.maxStorageBytes / 1048576}</dd></div></dl>
      <form onSubmit={e => { e.preventDefault(); void perform('assign') }}><fieldset disabled={saving}>
        <div className="plan-fields"><label>Pacchetto<select required value={planId} onChange={e => setPlanId(e.target.value)}><option value="">Seleziona un pacchetto</option>{plans.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}</select></label>
          <label>Tipo di assegnazione<select value={trial ? 'trial' : 'active'} onChange={e => setTrial(e.target.value === 'trial')}><option value="active">Definitiva, senza scadenza</option><option value="trial">Prova gratuita</option></select></label></div>
        {selected && <div className="plan-tags">{selected.features.map(f => <span key={f}>{PLAN_FEATURES[f - 1]}</span>)}</div>}
        {selected && ((selected.maxEmployees != null && assignment.employees > selected.maxEmployees)
          || (selected.maxBranches != null && assignment.branches > selected.maxBranches)
          || (selected.maxStorageBytes != null && assignment.storageBytes > selected.maxStorageBytes)) && <p className="plan-impact">
          L’utilizzo attuale supera almeno un limite del pacchetto selezionato. I dati saranno conservati; le nuove aggiunte nelle categorie oltre soglia saranno bloccate.
        </p>}
        {(trial || assignment.trialEndsAt) && <label className="plan-days">Giorni di prova / proroga<input type="number" min={1} max={3650} step={1} required value={days} onChange={e => setDays(Number(e.target.value))} /></label>}
        <p className="plan-note">Assegnare una prova ne avvia la durata da adesso. Prorogare aggiunge giorni alla scadenza futura, oppure da oggi se già scaduta. Il cambio pacchetto conserva i dati.</p>
        <div className="plan-actions"><button className="btn-primary" disabled={!selected} type="submit">{saving ? 'Salvataggio…' : 'Assegna pacchetto'}</button>
          {assignment.trialEndsAt && <><button className="btn-secondary" type="button" disabled={!Number.isInteger(days) || days < 1 || days > 3650} onClick={() => void perform('extend')}>Proroga prova</button><button className="btn-secondary" type="button" onClick={() => void perform('activate')}>Attiva definitivamente</button></>}
        </div>
      </fieldset></form>
    </> : !error && <p>Caricamento…</p>}
  </section>
}
