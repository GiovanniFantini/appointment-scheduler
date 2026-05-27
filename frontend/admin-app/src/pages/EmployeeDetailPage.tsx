import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Breadcrumb, Skeleton } from '@scheduler/ui'
import apiClient from '../lib/axios'
import { formatBrowserDate } from '../lib/dateUtils'
import './EmployeeDetailPage.css'

// EmployeeKind: Internal=0, External=1
const KIND_LABEL: Record<number, string> = { 0: 'Interno', 1: 'Esterno' }

interface MembershipDto {
  id: number
  merchantId: number
  merchantName: string
  merchantApproved: boolean
  roleId: number
  roleName: string
  homeBranchId: number
  homeBranchName: string
  isActive: boolean
  joinedAt: string
}

interface EmployeeDetail {
  id: number
  email: string
  firstName: string
  lastName: string
  phoneNumber?: string | null
  kind: number
  contractType?: number | null
  agencyName?: string | null
  hourlyRate?: number | null
  externalNotes?: string | null
  isActive: boolean
  createdAt: string
  updatedAt?: string | null
  userId?: number | null
  hasAccount: boolean
  memberships: MembershipDto[]
}

export default function EmployeeDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [employee, setEmployee] = useState<EmployeeDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false
    const fetchEmployee = async () => {
      setLoading(true)
      setError('')
      try {
        const res = await apiClient.get<EmployeeDetail>(`/admin/employees/${id}`)
        if (!cancelled) setEmployee(res.data)
      } catch {
        if (!cancelled) setError('Impossibile caricare i dettagli employee.')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    fetchEmployee()
    return () => { cancelled = true }
  }, [id])

  if (loading) return (
    <div className="user-detail-page" style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
      <Skeleton variant="text" width="40%" />
      <Skeleton variant="box" height={140} />
      <Skeleton variant="box" height={140} />
    </div>
  )
  if (error) return <div className="detail-error">{error}</div>
  if (!employee) return <div className="detail-error">Employee non trovato.</div>

  return (
    <div className="user-detail-page">
      <div className="detail-header">
        <div className="detail-header-left">
          <Breadcrumb
            items={[
              { label: 'Employees', to: '/employees' },
              { label: employee.email },
            ]}
          />
          <Link to="/employees" className="back-link">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
              <polyline points="15 18 9 12 15 6" />
            </svg>
            Back to Employees
          </Link>
          <h1 className="page-title">{employee.firstName} {employee.lastName}</h1>
          <div className="user-detail-chips">
            <span className={`kind-chip kind-chip-${employee.kind === 1 ? 'external' : 'internal'}`}>
              {KIND_LABEL[employee.kind] ?? '?'}
            </span>
            <span className={`account-chip account-chip-${employee.hasAccount ? 'yes' : 'no'}`}>
              {employee.hasAccount ? 'Account attivo' : 'Pre-caricato'}
            </span>
            <span className={`user-status user-status-${employee.isActive ? 'active' : 'inactive'}`}>
              {employee.isActive ? 'Active' : 'Inactive'}
            </span>
          </div>
        </div>

        {employee.hasAccount && employee.userId && (
          <div className="detail-actions">
            <Link className="btn-secondary" to={`/users/${employee.userId}`}>Apri user collegato →</Link>
          </div>
        )}
      </div>

      {/* Anagrafica */}
      <div className="info-card">
        <div className="info-card-header">
          <span className="info-card-title">Anagrafica</span>
        </div>
        <div className="info-grid">
          <div className="info-field">
            <div className="info-field-label">Email</div>
            <div className="info-field-value">{employee.email}</div>
          </div>
          <div className="info-field">
            <div className="info-field-label">Telefono</div>
            <div className={`info-field-value${employee.phoneNumber ? '' : ' secondary'}`}>
              {employee.phoneNumber ?? '—'}
            </div>
          </div>
          <div className="info-field">
            <div className="info-field-label">Creato</div>
            <div className="info-field-value">{formatBrowserDate(new Date(employee.createdAt))}</div>
          </div>
          <div className="info-field">
            <div className="info-field-label">Ultima modifica</div>
            <div className={`info-field-value${employee.updatedAt ? '' : ' secondary'}`}>
              {employee.updatedAt ? formatBrowserDate(new Date(employee.updatedAt)) : '—'}
            </div>
          </div>
        </div>
      </div>

      {/* External-only */}
      {employee.kind === 1 && (
        <div className="info-card">
          <div className="info-card-header">
            <span className="info-card-title">Risorsa esterna</span>
          </div>
          <div className="info-grid">
            <div className="info-field">
              <div className="info-field-label">Agenzia</div>
              <div className={`info-field-value${employee.agencyName ? '' : ' secondary'}`}>
                {employee.agencyName ?? '—'}
              </div>
            </div>
            <div className="info-field">
              <div className="info-field-label">Tariffa oraria</div>
              <div className={`info-field-value${employee.hourlyRate != null ? '' : ' secondary'}`}>
                {employee.hourlyRate != null ? `€ ${employee.hourlyRate.toFixed(2)}` : '—'}
              </div>
            </div>
            <div className="info-field" style={{ gridColumn: '1 / -1' }}>
              <div className="info-field-label">Note</div>
              <div className={`info-field-value${employee.externalNotes ? '' : ' secondary'}`}>
                {employee.externalNotes ?? '—'}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Memberships */}
      <div className="info-card">
        <div className="info-card-header">
          <span className="info-card-title">Membership ({employee.memberships.length})</span>
        </div>
        {employee.memberships.length === 0 ? (
          <div className="employees-empty">Nessuna membership attiva. L'employee è pre-caricato ma non ancora collegato a un merchant.</div>
        ) : (
          <div className="memberships-list">
            {employee.memberships.map((m) => (
              <div key={m.id} className="membership-row">
                <div className="membership-main">
                  <Link to={`/merchants/${m.merchantId}`} className="membership-merchant">
                    {m.merchantName}
                  </Link>
                  <div className="membership-meta">
                    {m.roleName} · {m.homeBranchName} · dal {formatBrowserDate(new Date(m.joinedAt))}
                  </div>
                </div>
                <span className={`user-status user-status-${m.isActive ? 'active' : 'inactive'}`}>
                  {m.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
