import './UsersPage.css'

export default function UsersPage() {
  return (
    <div className="users-page">
      <div className="page-header">
        <h1 className="page-title">Users</h1>
        <p className="page-subtitle">Platform user management</p>
      </div>

      <div className="users-placeholder">
        <div className="users-placeholder-icon">
          <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8}>
            <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
            <circle cx="9" cy="7" r="4" />
            <path d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" />
          </svg>
        </div>
        <div className="users-placeholder-title">User management coming soon</div>
        <p className="users-placeholder-text">
          Full consumer and employee user management will be available once the
          dedicated user endpoints are implemented in the API.
        </p>
        <span className="users-placeholder-badge">In development</span>
      </div>
    </div>
  )
}
