import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import apiClient from '../lib/axios'
import { formatBrowserDate } from '../lib/dateUtils'
import './MerchantDetailPage.css'

interface MerchantDetail {
  id: number
  userId: number
  companyName: string
  vatNumber?: string
  address?: string
  city?: string
  postalCode?: string
  country?: string
  phone?: string
  businessEmail?: string
  isApproved: boolean
  isActive: boolean
  createdAt: string
  approvedAt?: string
  owner?: {
    id: number
    email: string
    firstName: string
    lastName: string
    phoneNumber?: string
  }
  employeeCount: number
}

function getMerchantStatus(m: MerchantDetail): string {
  if (!m.isActive) return 'inactive'
  if (!m.isApproved) return 'pending'
  return 'active'
}

function chipClass(status: string) {
  switch (status) {
    case 'active': return 'status-chip chip-active'
    case 'pending': return 'status-chip chip-pending'
    default: return 'status-chip chip-inactive'
  }
}

interface EditData {
  companyName: string
  vatNumber: string
  city: string
  address: string
  phone: string
  businessEmail: string
}

export default function MerchantDetailPage() {
  const { id } = useParams<{ id: string }>()

  const [merchant, setMerchant] = useState<MerchantDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [fetchError, setFetchError] = useState('')

  const [editMode, setEditMode] = useState(false)
  const [editData, setEditData] = useState<EditData>({ companyName: '', vatNumber: '', city: '', address: '', phone: '', businessEmail: '' })
  const [saving, setSaving] = useState(false)
  const [saveSuccess, setSaveSuccess] = useState(false)
  const [saveError, setSaveError] = useState('')

  const [actionLoading, setActionLoading] = useState(false)

  const fetchMerchant = async () => {
    setLoading(true)
    setFetchError('')
    try {
      const res = await apiClient.get(`/merchants/${id}`)
      setMerchant(res.data?.data ?? res.data)
    } catch {
      setFetchError('Failed to load merchant details.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchMerchant()
  }, [id])

  const startEdit = () => {
    if (!merchant) return
    setEditData({
      companyName: merchant.companyName,
      vatNumber: merchant.vatNumber ?? '',
      city: merchant.city ?? '',
      address: merchant.address ?? '',
      phone: merchant.phone ?? '',
      businessEmail: merchant.businessEmail ?? '',
    })
    setEditMode(true)
    setSaveSuccess(false)
    setSaveError('')
  }

  const cancelEdit = () => {
    setEditMode(false)
    setSaveError('')
  }

  const saveEdit = async () => {
    setSaving(true)
    setSaveError('')
    try {
      await apiClient.put(`/merchants/${id}`, editData)
      setSaveSuccess(true)
      setEditMode(false)
      await fetchMerchant()
      setTimeout(() => setSaveSuccess(false), 3000)
    } catch {
      setSaveError('Failed to save changes. Please try again.')
    } finally {
      setSaving(false)
    }
  }

  const handleApprove = async () => {
    setActionLoading(true)
    try {
      await apiClient.patch(`/merchants/${id}/approve`)
      await fetchMerchant()
    } finally {
      setActionLoading(false)
    }
  }

  const handleReject = async () => {
    setActionLoading(true)
    try {
      await apiClient.patch(`/merchants/${id}/reject`)
      await fetchMerchant()
    } finally {
      setActionLoading(false)
    }
  }

  if (loading) return <div className="detail-loading">Loading merchant details…</div>
  if (fetchError) return <div className="detail-error">{fetchError}</div>
  if (!merchant) return <div className="detail-error">Merchant not found.</div>

  const status = getMerchantStatus(merchant)

  return (
    <div className="merchant-detail-page">
      {/* Header */}
      <div className="detail-header">
        <div className="detail-header-left">
          <Link to="/merchants" className="back-link">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
              <polyline points="15 18 9 12 15 6" />
            </svg>
            Back to Merchants
          </Link>
          <h1 className="page-title">{merchant.companyName}</h1>
          <span className={chipClass(status)}>
            {status.charAt(0).toUpperCase() + status.slice(1)}
          </span>
        </div>

        <div className="detail-actions">
          {status !== 'active' && (
            <button className="btn-success" onClick={handleApprove} disabled={actionLoading}>
              Approve
            </button>
          )}
          {status !== 'inactive' && (
            <button className="btn-danger" onClick={handleReject} disabled={actionLoading}>
              Deactivate
            </button>
          )}
          {!editMode && (
            <button className="btn-primary" onClick={startEdit}>
              Edit
            </button>
          )}
        </div>
      </div>

      {saveSuccess && <div className="success-banner">Changes saved successfully.</div>}
      {saveError && <div className="error-banner">{saveError}</div>}

      {/* Company info */}
      <div className="info-card">
        <div className="info-card-header">
          <span className="info-card-title">Company Information</span>
        </div>

        {editMode ? (
          <div className="edit-form">
            <div className="edit-grid">
              {(
                [
                  { key: 'companyName', label: 'Company Name' },
                  { key: 'vatNumber', label: 'VAT Number' },
                  { key: 'city', label: 'City' },
                  { key: 'address', label: 'Address' },
                  { key: 'phone', label: 'Phone' },
                  { key: 'businessEmail', label: 'Business Email' },
                ] as { key: keyof EditData; label: string }[]
              ).map(({ key, label }) => (
                <div key={key} className="edit-field">
                  <label className="edit-field-label">{label}</label>
                  <input
                    className="edit-input"
                    value={editData[key]}
                    onChange={(e) => setEditData((prev) => ({ ...prev, [key]: e.target.value }))}
                  />
                </div>
              ))}
            </div>
            <div className="edit-form-actions">
              <button className="btn-primary" onClick={saveEdit} disabled={saving}>
                {saving ? 'Saving…' : 'Save changes'}
              </button>
              <button className="btn-secondary" onClick={cancelEdit} disabled={saving}>
                Cancel
              </button>
            </div>
          </div>
        ) : (
          <div className="info-grid">
            <div className="info-field">
              <div className="info-field-label">Company Name</div>
              <div className="info-field-value">{merchant.companyName}</div>
            </div>
            <div className="info-field">
              <div className="info-field-label">VAT Number</div>
              <div className={`info-field-value${merchant.vatNumber ? '' : ' secondary'}`}>
                {merchant.vatNumber ?? '—'}
              </div>
            </div>
            <div className="info-field">
              <div className="info-field-label">City</div>
              <div className={`info-field-value${merchant.city ? '' : ' secondary'}`}>
                {merchant.city ?? '—'}
              </div>
            </div>
            <div className="info-field">
              <div className="info-field-label">Address</div>
              <div className={`info-field-value${merchant.address ? '' : ' secondary'}`}>
                {merchant.address ?? '—'}
              </div>
            </div>
            <div className="info-field">
              <div className="info-field-label">Phone</div>
              <div className={`info-field-value${merchant.phone ? '' : ' secondary'}`}>
                {merchant.phone ?? '—'}
              </div>
            </div>
            <div className="info-field">
              <div className="info-field-label">Business Email</div>
              <div className={`info-field-value${merchant.businessEmail ? '' : ' secondary'}`}>
                {merchant.businessEmail ?? '—'}
              </div>
            </div>
            <div className="info-field">
              <div className="info-field-label">Registered</div>
              <div className="info-field-value">
                {formatBrowserDate(new Date(merchant.createdAt))}
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Owner info */}
      <div className="info-card">
        <div className="info-card-header">
          <span className="info-card-title">Owner Information</span>
        </div>
        <div className="info-grid">
          <div className="info-field">
            <div className="info-field-label">First Name</div>
            <div className={`info-field-value${merchant.owner?.firstName ? '' : ' secondary'}`}>
              {merchant.owner?.firstName ?? '—'}
            </div>
          </div>
          <div className="info-field">
            <div className="info-field-label">Last Name</div>
            <div className={`info-field-value${merchant.owner?.lastName ? '' : ' secondary'}`}>
              {merchant.owner?.lastName ?? '—'}
            </div>
          </div>
          <div className="info-field">
            <div className="info-field-label">Email</div>
            <div className={`info-field-value${merchant.owner?.email ? '' : ' secondary'}`}>
              {merchant.owner?.email ?? '—'}
            </div>
          </div>
          <div className="info-field">
            <div className="info-field-label">Employees</div>
            <div className="info-field-value">{merchant.employeeCount}</div>
          </div>
        </div>
      </div>
    </div>
  )
}
