import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Breadcrumb, Skeleton, useAuth, useConfirm, useToast } from '@scheduler/ui'
import apiClient from '../lib/axios'
import { formatBrowserDate } from '../lib/dateUtils'
import type { AdminUser } from '../App'
import './UserDetailPage.css'

const ACCOUNT_TYPE: Record<number, string> = { 1: 'Admin', 2: 'Merchant', 3: 'Employee' }

interface MerchantLink {
  id: number
  companyName: string
  isApproved: boolean
  isActive: boolean
}

interface EmployeeLink {
  id: number
  email: string
  kind: number
  isActive: boolean
}

interface UserDetail {
  id: number
  email: string
  firstName: string
  lastName: string
  phoneNumber?: string
  accountType: number
  isActive: boolean
  createdAt: string
  updatedAt?: string
  merchant?: MerchantLink | null
  employee?: EmployeeLink | null
}

function accountChipClass(t: number) {
  switch (t) {
    case 1: return 'acct-chip acct-chip-admin'
    case 2: return 'acct-chip acct-chip-merchant'
    default: return 'acct-chip acct-chip-employee'
  }
}

export default function UserDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { user: currentUser } = useAuth<AdminUser>()
  const toast = useToast()
  const confirm = useConfirm()

  const [user, setUser] = useState<UserDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [fetchError, setFetchError] = useState('')
  const [actionLoading, setActionLoading] = useState(false)

  const fetchUser = async () => {
    setLoading(true)
    setFetchError('')
    try {
      const res = await apiClient.get<UserDetail>(`/admin/users/${id}`)
      setUser(res.data)
    } catch {
      setFetchError('Impossibile caricare i dettagli utente.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchUser()
  }, [id])

  const isSelf = user != null && currentUser != null && user.id === currentUser.userId

  const handleToggleActive = async () => {
    if (!user) return
    const action = user.isActive ? 'deactivate' : 'activate'
    const ok = await confirm({
      title: user.isActive ? 'Disattivare account?' : 'Riattivare account?',
      message: user.isActive
        ? `L'utente ${user.email} non potrà più accedere finché non verrà riattivato.`
        : `L'utente ${user.email} potrà nuovamente accedere.`,
      variant: user.isActive ? 'danger' : 'warning',
      confirmLabel: user.isActive ? 'Disattiva' : 'Riattiva',
    })
    if (!ok) return

    setActionLoading(true)
    try {
      await apiClient.patch(`/admin/users/${user.id}/${action}`)
      toast.success(user.isActive ? 'Account disattivato' : 'Account riattivato')
      await fetchUser()
    } catch (err: unknown) {
      const e = err as { response?: { status?: number; data?: { message?: string } } }
      if (e?.response?.status === 409) {
        toast.error(e.response.data?.message ?? 'Operazione non consentita')
      } else {
        toast.error('Errore durante l\'operazione')
      }
    } finally {
      setActionLoading(false)
    }
  }

  const handleSendReset = async () => {
    if (!user) return
    const ok = await confirm({
      title: 'Inviare email di reset password?',
      message: `Verrà inviata un'email a ${user.email} con il link per impostare una nuova password.`,
      variant: 'warning',
      confirmLabel: 'Invia',
    })
    if (!ok) return

    setActionLoading(true)
    try {
      await apiClient.post(`/admin/users/${user.id}/send-password-reset`)
      toast.success('Email di reset inviata')
    } catch {
      toast.error('Impossibile inviare l\'email di reset')
    } finally {
      setActionLoading(false)
    }
  }

  if (loading) return (
    <div className="user-detail-page" style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
      <Skeleton variant="text" width="40%" />
      <Skeleton variant="box" height={140} />
      <Skeleton variant="box" height={140} />
    </div>
  )
  if (fetchError) return <div className="detail-error">{fetchError}</div>
  if (!user) return <div className="detail-error">Utente non trovato.</div>

  return (
    <div className="user-detail-page">
      <div className="detail-header">
        <div className="detail-header-left">
          <Breadcrumb
            items={[
              { label: 'Users', to: '/users' },
              { label: user.email },
            ]}
          />
          <Link to="/users" className="back-link">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
              <polyline points="15 18 9 12 15 6" />
            </svg>
            Back to Users
          </Link>
          <h1 className="page-title">{user.firstName} {user.lastName}</h1>
          <div className="user-detail-chips">
            <span className={accountChipClass(user.accountType)}>
              {ACCOUNT_TYPE[user.accountType] ?? '?'}
            </span>
            <span className={`user-status user-status-${user.isActive ? 'active' : 'inactive'}`}>
              {user.isActive ? 'Active' : 'Inactive'}
            </span>
            {isSelf && <span className="self-chip">Sei tu</span>}
          </div>
        </div>

        <div className="detail-actions">
          <button
            className="btn-secondary"
            onClick={handleSendReset}
            disabled={actionLoading}
          >
            Invia reset password
          </button>
          <button
            className={user.isActive ? 'btn-danger' : 'btn-success'}
            onClick={handleToggleActive}
            disabled={actionLoading || isSelf}
            title={isSelf ? 'Non puoi modificare il tuo stesso account' : undefined}
          >
            {user.isActive ? 'Disattiva' : 'Riattiva'}
          </button>
        </div>
      </div>

      {/* Account */}
      <div className="info-card">
        <div className="info-card-header">
          <span className="info-card-title">Account</span>
        </div>
        <div className="info-grid">
          <div className="info-field">
            <div className="info-field-label">Email</div>
            <div className="info-field-value">{user.email}</div>
          </div>
          <div className="info-field">
            <div className="info-field-label">Telefono</div>
            <div className={`info-field-value${user.phoneNumber ? '' : ' secondary'}`}>
              {user.phoneNumber ?? '—'}
            </div>
          </div>
          <div className="info-field">
            <div className="info-field-label">Nome</div>
            <div className="info-field-value">{user.firstName}</div>
          </div>
          <div className="info-field">
            <div className="info-field-label">Cognome</div>
            <div className="info-field-value">{user.lastName}</div>
          </div>
          <div className="info-field">
            <div className="info-field-label">Registrato</div>
            <div className="info-field-value">{formatBrowserDate(new Date(user.createdAt))}</div>
          </div>
          <div className="info-field">
            <div className="info-field-label">Ultima modifica</div>
            <div className={`info-field-value${user.updatedAt ? '' : ' secondary'}`}>
              {user.updatedAt ? formatBrowserDate(new Date(user.updatedAt)) : '—'}
            </div>
          </div>
        </div>
      </div>

      {/* Merchant link */}
      {user.merchant && (
        <div className="info-card">
          <div className="info-card-header">
            <span className="info-card-title">Merchant associato</span>
            <Link className="info-card-link" to={`/merchants/${user.merchant.id}`}>Apri scheda →</Link>
          </div>
          <div className="info-grid">
            <div className="info-field">
              <div className="info-field-label">Ragione sociale</div>
              <div className="info-field-value">{user.merchant.companyName}</div>
            </div>
            <div className="info-field">
              <div className="info-field-label">Stato</div>
              <div className="info-field-value">
                {user.merchant.isActive
                  ? (user.merchant.isApproved ? 'Approvato' : 'In attesa')
                  : 'Inattivo'}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Employee link */}
      {user.employee && (
        <div className="info-card">
          <div className="info-card-header">
            <span className="info-card-title">Profilo employee</span>
          </div>
          <div className="info-grid">
            <div className="info-field">
              <div className="info-field-label">Email employee</div>
              <div className="info-field-value">{user.employee.email}</div>
            </div>
            <div className="info-field">
              <div className="info-field-label">Tipo</div>
              <div className="info-field-value">
                {user.employee.kind === 1 ? 'Esterno' : 'Interno'}
              </div>
            </div>
            <div className="info-field">
              <div className="info-field-label">Attivo</div>
              <div className="info-field-value">{user.employee.isActive ? 'Sì' : 'No'}</div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
