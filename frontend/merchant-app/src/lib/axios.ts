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

// Interceptor per gestire gli errori di risposta
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    // Log dell'errore per debug
    console.error('API Error:', {
      status: error.response?.status,
      data: error.response?.data,
      url: error.config?.url
    })

    // Se ricevi 401 Unauthorized, probabilmente il token è scaduto o non valido
    if (error.response?.status === 401) {
      // Rimuovi il token scaduto
      const currentPath = window.location.pathname

      // Solo se non siamo già sulla pagina di login, mostra un alert e redirect
      if (!currentPath.includes('/login')) {
        console.warn('Token scaduto o non valido, redirect al login')

        // Mostra un messaggio più dettagliato se disponibile
        const errorMessage = error.response?.data?.message || error.response?.data || 'Sessione scaduta'

        // Non fare alert qui, lascia gestire al componente
        // ma logga per debug
        console.error('Errore 401:', errorMessage)
      }
    }

    return Promise.reject(error)
  }
)

export default apiClient
