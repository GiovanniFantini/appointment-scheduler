import axios from 'axios'
import { API_CONFIG, STORAGE_KEYS, HTTP_HEADERS } from '../constants'

// In produzione, usa l'URL dell'API backend dalla configurazione
// In sviluppo, usa il percorso relativo che passa attraverso il proxy di Vite
const apiURL = import.meta.env.PROD
  ? API_CONFIG.BASE_URL
  : ''

// Assicura che il baseURL in produzione termini sempre con il base path
const baseURL = import.meta.env.PROD
  ? (apiURL.endsWith(API_CONFIG.BASE_PATH) ? apiURL : `${apiURL}${API_CONFIG.BASE_PATH}`)
  : API_CONFIG.BASE_PATH

const apiClient = axios.create({
  baseURL,
  headers: {
    [HTTP_HEADERS.CONTENT_TYPE]: 'application/json'
  },
  withCredentials: API_CONFIG.WITH_CREDENTIALS,
  timeout: API_CONFIG.TIMEOUT
})

// Interceptor per aggiungere il token di autenticazione se presente
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem(STORAGE_KEYS.TOKEN)
    if (token) {
      config.headers[HTTP_HEADERS.AUTHORIZATION] = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

export default apiClient
