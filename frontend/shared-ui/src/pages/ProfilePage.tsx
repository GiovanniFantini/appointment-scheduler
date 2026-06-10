import { useEffect, useState } from 'react'
import { PageHeader } from '../shell/PageHeader'
import { FormField } from '../form/FormField'
import { Input } from '../form/Input'
import { PasswordRequirements, passwordRulesMet } from '../form/PasswordRequirements'
import { Button } from '../ui/Button'
import { useToast } from '../ui/Toast/Toaster'
import { extractApiError } from '../lib/apiError'
import './profile.css'

interface AccountProfile {
  userId: number
  email: string
  firstName: string
  lastName: string
  phoneNumber?: string | null
  accountType: number
}

interface AxiosLike {
  get: <T = unknown>(url: string) => Promise<{ data: T }>
  put: <T = unknown>(url: string, body: unknown) => Promise<{ data: T }>
  post: <T = unknown>(url: string, body: unknown) => Promise<{ data: T }>
}

interface ProfilePageProps {
  /**
   * Axios client dell'app chiamante (ognuna ha il suo, con interceptor token).
   * Iniettato per evitare duplicare la config nel pacchetto condiviso.
   */
  apiClient: AxiosLike
  /**
   * Callback opzionale invocato quando l'utente aggiorna nome/cognome:
   * permette all'app di rinfrescare lo stato auth e quindi l'avatar nell'header.
   * Non viene chiamato quando readOnlyProfile=true.
   */
  onProfileUpdated?: (profile: AccountProfile) => void
  /**
   * Se true, l'anagrafica è solo in lettura (no input, no bottone "Salva").
   * Cambio password resta sempre disponibile.
   * Usato in merchant + employee dove i dati vengono dal gestionale aziendale.
   */
  readOnlyProfile?: boolean
}

export function ProfilePage({ apiClient, onProfileUpdated, readOnlyProfile }: ProfilePageProps) {
  const toast = useToast()
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [profile, setProfile] = useState<AccountProfile | null>(null)
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')

  // Password
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [changingPwd, setChangingPwd] = useState(false)
  const [pwdError, setPwdError] = useState<string | undefined>(undefined)

  useEffect(() => {
    let cancelled = false
    async function load() {
      try {
        const { data } = await apiClient.get<AccountProfile>('/account/me')
        if (cancelled) return
        setProfile(data)
        setFirstName(data.firstName)
        setLastName(data.lastName)
        setPhoneNumber(data.phoneNumber ?? '')
      } catch (err) {
        if (!cancelled) toast.error(extractApiError(err, 'Impossibile caricare il profilo'))
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    load()
    return () => {
      cancelled = true
    }
  }, [apiClient, toast])

  const handleSaveProfile = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!firstName.trim() || !lastName.trim()) {
      toast.error('Nome e cognome obbligatori')
      return
    }
    setSaving(true)
    try {
      const { data } = await apiClient.put<AccountProfile>('/account/me', {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        phoneNumber: phoneNumber.trim() || null
      })
      setProfile(data)
      onProfileUpdated?.(data)
      toast.success('Profilo aggiornato')
    } catch (err) {
      toast.error(extractApiError(err, 'Errore durante il salvataggio'))
    } finally {
      setSaving(false)
    }
  }

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault()
    setPwdError(undefined)
    if (!passwordRulesMet(newPassword)) {
      setPwdError('La password non rispetta i requisiti minimi indicati.')
      return
    }
    if (newPassword !== confirmPassword) {
      setPwdError('Le due password non coincidono')
      return
    }
    setChangingPwd(true)
    try {
      await apiClient.post('/account/change-password', { currentPassword, newPassword })
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      toast.success('Password aggiornata')
    } catch (err) {
      setPwdError(extractApiError(err, 'Errore durante il cambio password'))
    } finally {
      setChangingPwd(false)
    }
  }

  return (
    <div className="su-page">
      <PageHeader
        title="Il mio profilo"
        subtitle="Gestisci le tue informazioni e la password di accesso"
      />

      {loading ? (
        <div className="su-page__section" style={{ textAlign: 'center', color: 'var(--su-text-secondary)' }}>
          Caricamento…
        </div>
      ) : profile ? (
        <>
          {/* Sezione anagrafica */}
          {readOnlyProfile ? (
            <div className="su-page__section">
              <h2 className="su-page__section-title">Dati personali</h2>
              <p className="su-page__section-desc">
                Per modificare i dati anagrafici contatta l'amministratore dell'azienda.
              </p>

              <div className="su-form-grid">
                <FormField label="Nome">
                  <Input value={profile.firstName} disabled readOnly />
                </FormField>
                <FormField label="Cognome">
                  <Input value={profile.lastName} disabled readOnly />
                </FormField>
                <FormField label="Email">
                  <Input value={profile.email} disabled readOnly />
                </FormField>
                <FormField label="Telefono">
                  <Input value={profile.phoneNumber ?? '—'} disabled readOnly />
                </FormField>
              </div>
            </div>
          ) : (
            <form className="su-page__section" onSubmit={handleSaveProfile}>
              <h2 className="su-page__section-title">Dati personali</h2>
              <p className="su-page__section-desc">L'email non è modificabile.</p>

              <div className="su-form-grid">
                <FormField label="Nome" required htmlFor="firstName">
                  <Input
                    id="firstName"
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                    required
                  />
                </FormField>
                <FormField label="Cognome" required htmlFor="lastName">
                  <Input
                    id="lastName"
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    required
                  />
                </FormField>
                <FormField label="Email" htmlFor="email">
                  <Input id="email" value={profile.email} disabled readOnly />
                </FormField>
                <FormField label="Telefono" htmlFor="phone">
                  <Input
                    id="phone"
                    type="tel"
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                    placeholder="+39 ..."
                  />
                </FormField>
              </div>

              <div className="su-form-actions">
                <Button type="submit" variant="primary" loading={saving}>
                  Salva modifiche
                </Button>
              </div>
            </form>
          )}

          {/* Sezione password */}
          <form className="su-page__section" onSubmit={handleChangePassword}>
            <h2 className="su-page__section-title">Cambia password</h2>
            <p className="su-page__section-desc">
              Per sicurezza inserisci la password attuale prima di sceglierne una nuova.
            </p>

            <div className="su-form-grid">
              <FormField label="Password attuale" required htmlFor="curPwd" className="su-form-grid__full">
                <Input
                  id="curPwd"
                  type="password"
                  autoComplete="current-password"
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  required
                />
              </FormField>
              <FormField
                label="Nuova password"
                required
                htmlFor="newPwd"
                error={pwdError && !passwordRulesMet(newPassword) ? pwdError : undefined}
              >
                <Input
                  id="newPwd"
                  type="password"
                  autoComplete="new-password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  required
                  error={!!pwdError && !passwordRulesMet(newPassword)}
                />
                {newPassword.length > 0 && <PasswordRequirements password={newPassword} />}
              </FormField>
              <FormField
                label="Conferma nuova password"
                required
                htmlFor="confirmPwd"
                error={pwdError && newPassword === confirmPassword ? undefined : pwdError}
              >
                <Input
                  id="confirmPwd"
                  type="password"
                  autoComplete="new-password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  required
                  error={!!pwdError && newPassword !== confirmPassword}
                />
              </FormField>
            </div>

            <div className="su-form-actions">
              <Button type="submit" variant="primary" loading={changingPwd}>
                Aggiorna password
              </Button>
            </div>
          </form>
        </>
      ) : null}
    </div>
  )
}
