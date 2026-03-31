import { useState } from 'react'
import { EmployeeUser } from '../../App'
import apiClient from '../../lib/axios'
import './SelectCompanyPage.css'

interface AuthResponse {
  token: string
  userId: number
  email: string
  firstName: string
  lastName: string
  accountType: number
  employeeId?: number
  merchantId?: number
  companyName?: string
  companies: Array<{ merchantId: number; companyName: string; city?: string; roleId: number; roleName: string }>
  activeFeatures: string[]
}

interface Props {
  user: EmployeeUser
  onCompanySelected: (user: EmployeeUser, token: string) => void
  onLogout: () => void
}

export default function SelectCompanyPage({ user, onCompanySelected, onLogout }: Props) {
  const [loading, setLoading] = useState<number | null>(null)
  const [error, setError] = useState('')

  const handleSelect = async (merchantId: number) => {
    setError('')
    setLoading(merchantId)
    try {
      const { data } = await apiClient.post<AuthResponse>(`/auth/employee/select-company/${merchantId}`)

      const updatedUser: EmployeeUser = {
        userId: data.userId,
        email: data.email,
        firstName: data.firstName,
        lastName: data.lastName,
        accountType: data.accountType,
        employeeId: data.employeeId,
        merchantId: data.merchantId,
        companyName: data.companyName,
        activeFeatures: data.activeFeatures ?? [],
        companies: data.companies ?? user.companies,
      }

      onCompanySelected(updatedUser, data.token)
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } }
      setError(axiosErr.response?.data?.message ?? 'Errore nella selezione azienda')
    } finally {
      setLoading(null)
    }
  }

  return (
    <div className="select-company-page">
      <div className="select-company-container">
        <div className="select-company-header">
          <div className="select-company-logo">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 7V3H2v18h20V7H12zM6 19H4v-2h2v2zm0-4H4v-2h2v2zm0-4H4V9h2v2zm0-4H4V5h2v2zm4 12H8v-2h2v2zm0-4H8v-2h2v2zm0-4H8V9h2v2zm0-4H8V5h2v2zm10 12h-8v-2h2v-2h-2v-2h2v-2h-2V9h8v10zm-2-8h-2v2h2v-2zm0 4h-2v2h2v-2z" fill="currentColor" />
            </svg>
          </div>
          <h1 className="select-company-title">Seleziona Azienda</h1>
          <p className="select-company-subtitle">
            Ciao <strong>{user.firstName}</strong>! Seleziona l'azienda per cui vuoi accedere.
          </p>
        </div>

        {error && <div className="select-company-error">{error}</div>}

        <div className="company-list">
          {(user.companies ?? []).map(company => (
            <button
              key={company.merchantId}
              className="company-card"
              onClick={() => handleSelect(company.merchantId)}
              disabled={loading !== null}
            >
              <div className="company-card-icon">
                {company.companyName.charAt(0).toUpperCase()}
              </div>
              <div className="company-card-info">
                <span className="company-card-name">{company.companyName}</span>
                {company.city && <span className="company-card-city">{company.city}</span>}
                <span className="company-card-role">{company.roleName}</span>
              </div>
              <div className="company-card-arrow">
                {loading === company.merchantId
                  ? <span className="btn-spinner-dark" />
                  : (
                    <svg viewBox="0 0 24 24" fill="none">
                      <path d="M9 18l6-6-6-6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                    </svg>
                  )
                }
              </div>
            </button>
          ))}
        </div>

        <button className="logout-link" onClick={onLogout}>
          Esci dall'account
        </button>
      </div>
    </div>
  )
}
