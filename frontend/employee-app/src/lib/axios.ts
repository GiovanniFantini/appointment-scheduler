import axios from 'axios'

const apiURL = import.meta.env.PROD
  ? (import.meta.env.VITE_API_URL || 'https://appointment-scheduler-api.azurewebsites.net')
  : ''

const baseURL = import.meta.env.PROD
  ? (apiURL.endsWith('/api') ? apiURL : `${apiURL}/api`)
  : '/api'

const apiClient = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
  withCredentials: false
})

apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) config.headers.Authorization = `Bearer ${token}`
    return config
  },
  (error) => Promise.reject(error)
)

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error('Errore API:', {
      status: error.response?.status,
      data: error.response?.data,
      url: error.config?.url
    })

    if (error.response?.status === 401) {
      const currentPath = window.location.pathname
      const publicPaths = ['/login', '/register', '/forgot-password', '/reset-password']
      const isPublicPage = publicPaths.some((p) => currentPath.includes(p))
      if (!isPublicPage) {
        console.warn('Sessione scaduta, redirect al login')
        localStorage.removeItem('token')
        localStorage.removeItem('user')
        window.location.href = '/login'
      }
    }

    return Promise.reject(error)
  }
)

export default apiClient
