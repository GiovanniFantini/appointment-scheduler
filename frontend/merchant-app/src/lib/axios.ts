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
    console.error('API Error:', {
      status: error.response?.status,
      data: error.response?.data,
      url: error.config?.url
    })

    if (error.response?.status === 401) {
      const currentPath = window.location.pathname
      if (!currentPath.includes('/login')) {
        console.warn('Token scaduto, redirect al login')
        const errorMessage = error.response?.data?.message || error.response?.data || 'Sessione scaduta'
        console.error('Errore 401:', errorMessage)
      }
    }

    return Promise.reject(error)
  }
)

export default apiClient
