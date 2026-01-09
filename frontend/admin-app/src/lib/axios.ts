import axios from 'axios'

// In produzione, usa l'URL dell'API backend dalla variabile d'ambiente o fallback
// In sviluppo, usa il percorso relativo che passa attraverso il proxy di Vite
const apiURL = import.meta.env.PROD
  ? (import.meta.env.VITE_API_URL || 'https://appointment-scheduler-api.azurewebsites.net')
  : ''

// Assicura che il baseURL in produzione termini sempre con /api
const baseURL = import.meta.env.PROD
  ? (apiURL.endsWith('/api') ? apiURL : `${apiURL}/api`)
  : '/api'

console.log('API Base URL:', baseURL);

const apiClient = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json'
  },
  withCredentials: false
})

// Interceptor per aggiungere il token di autenticazione se presente
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

export default apiClient
