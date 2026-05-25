/**
 * Base utente comune alle 3 app (admin, merchant, employee).
 * Ogni app può estenderlo con campi specifici (es. activeFeatures, featureLevels, companies).
 */
export interface BaseUser {
  userId: number
  email: string
  firstName: string
  lastName: string
  accountType: number
}

export interface AuthContextValue<TUser extends BaseUser = BaseUser> {
  user: TUser | null
  isAuthenticated: boolean
  login: (user: TUser, token: string) => void
  logout: () => void
  updateUser: (user: TUser, token?: string) => void
}
