import axios from 'axios'

const apiURL = import.meta.env.PROD
  ? (import.meta.env.VITE_API_URL || 'https://appointment-scheduler-api.azurewebsites.net')
  : ''

const baseURL = import.meta.env.PROD
  ? (apiURL.endsWith('/api') ? apiURL : `${apiURL}/api`)
  : '/api'

console.log('API Base URL:', baseURL);

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

export default apiClient
