import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import type { AuthContextValue, BaseUser } from './types'

const AuthContext = createContext<AuthContextValue<any> | null>(null)

interface AuthProviderProps<TUser extends BaseUser> {
  children: ReactNode
  /** Chiave localStorage per il token (default 'token'). */
  tokenKey?: string
  /** Chiave localStorage per l'utente serializzato (default 'user'). */
  userKey?: string
  /** Hook per personalizzare cosa significa "autenticato" (es. employee con merchantId). */
  isAuthenticatedPredicate?: (user: TUser) => boolean
  /** Callback opzionale invocato dopo login/logout/update — utile per side-effect (es. analytics). */
  onChange?: (user: TUser | null) => void
  /** Render mentre l'auth è in fase di hydration dal localStorage. */
  loadingFallback?: ReactNode
}

export function AuthProvider<TUser extends BaseUser>({
  children,
  tokenKey = 'token',
  userKey = 'user',
  isAuthenticatedPredicate,
  onChange,
  loadingFallback = null
}: AuthProviderProps<TUser>) {
  const [user, setUser] = useState<TUser | null>(null)
  const [hydrated, setHydrated] = useState(false)

  useEffect(() => {
    const token = localStorage.getItem(tokenKey)
    const raw = localStorage.getItem(userKey)
    if (token && raw) {
      try {
        const parsed = JSON.parse(raw) as TUser
        setUser(parsed)
      } catch {
        localStorage.removeItem(tokenKey)
        localStorage.removeItem(userKey)
      }
    }
    setHydrated(true)
  }, [tokenKey, userKey])

  const login = useCallback(
    (next: TUser, token: string) => {
      localStorage.setItem(tokenKey, token)
      localStorage.setItem(userKey, JSON.stringify(next))
      setUser(next)
      onChange?.(next)
    },
    [tokenKey, userKey, onChange]
  )

  const logout = useCallback(() => {
    localStorage.removeItem(tokenKey)
    localStorage.removeItem(userKey)
    setUser(null)
    onChange?.(null)
  }, [tokenKey, userKey, onChange])

  const updateUser = useCallback(
    (next: TUser, token?: string) => {
      if (token) localStorage.setItem(tokenKey, token)
      localStorage.setItem(userKey, JSON.stringify(next))
      setUser(next)
      onChange?.(next)
    },
    [tokenKey, userKey, onChange]
  )

  const value = useMemo<AuthContextValue<TUser>>(() => {
    const isAuth = !!user && (isAuthenticatedPredicate ? isAuthenticatedPredicate(user) : true)
    return { user, isAuthenticated: isAuth, login, logout, updateUser }
  }, [user, isAuthenticatedPredicate, login, logout, updateUser])

  if (!hydrated) return <>{loadingFallback}</>

  return <AuthContext.Provider value={value as AuthContextValue<any>}>{children}</AuthContext.Provider>
}

export function useAuth<TUser extends BaseUser = BaseUser>(): AuthContextValue<TUser> {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth() deve essere usato dentro <AuthProvider>')
  return ctx as AuthContextValue<TUser>
}

export function useOptionalAuth<TUser extends BaseUser = BaseUser>(): AuthContextValue<TUser> | null {
  return useContext(AuthContext) as AuthContextValue<TUser> | null
}
