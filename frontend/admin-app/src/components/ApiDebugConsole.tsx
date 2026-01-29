import { useState, useEffect } from 'react'
import apiClient from '../lib/axios'

interface ApiDebugInfo {
  timestamp: string
  viteEnv: {
    mode: string
    prod: boolean
    dev: boolean
    viteApiUrl: string
    baseUrl: string
  }
  serverConfig: {
    nodeEnv: string
    apiUrl: string
    port: string | number
  }
  runtime: {
    appUrl: string
    axiosBaseURL: string
  }
  apiHealth: {
    status: string
    message: string
    checked: boolean
  }
  proxyTest: {
    status: string
    message: string
    checked: boolean
  }
}

function ApiDebugConsole() {
  const [isOpen, setIsOpen] = useState(false)
  const [debugInfo, setDebugInfo] = useState<ApiDebugInfo>({
    timestamp: new Date().toISOString(),
    viteEnv: {
      mode: import.meta.env.MODE || 'unknown',
      prod: import.meta.env.PROD || false,
      dev: import.meta.env.DEV || false,
      viteApiUrl: import.meta.env.VITE_API_URL || '(not set)',
      baseUrl: import.meta.env.BASE_URL || '/'
    },
    serverConfig: {
      nodeEnv: 'unknown',
      apiUrl: 'unknown',
      port: 'unknown'
    },
    runtime: {
      appUrl: window.location.origin,
      axiosBaseURL: (apiClient.defaults.baseURL || 'unknown')
    },
    apiHealth: {
      status: 'pending',
      message: 'Non ancora verificato',
      checked: false
    },
    proxyTest: {
      status: 'pending',
      message: 'Non ancora verificato',
      checked: false
    }
  })
  const [loading, setLoading] = useState(false)

  const checkApiConfiguration = async () => {
    setLoading(true)
    const newDebugInfo = { ...debugInfo }
    newDebugInfo.timestamp = new Date().toISOString()

    // Update runtime info
    newDebugInfo.viteEnv = {
      mode: import.meta.env.MODE || 'unknown',
      prod: import.meta.env.PROD || false,
      dev: import.meta.env.DEV || false,
      viteApiUrl: import.meta.env.VITE_API_URL || '(not set)',
      baseUrl: import.meta.env.BASE_URL || '/'
    }
    newDebugInfo.runtime = {
      appUrl: window.location.origin,
      axiosBaseURL: (apiClient.defaults.baseURL || 'unknown')
    }

    try {
      // Test 1: Check server config endpoint (non ha prefisso /api, usa fetch)
      const controller1 = new AbortController()
      const timeout1 = setTimeout(() => controller1.abort(), 5000)
      const configResponse = await fetch('/api-debug/config', {
        signal: controller1.signal
      })
      clearTimeout(timeout1)

      if (!configResponse.ok) throw new Error(`HTTP ${configResponse.status}`)
      const configData = await configResponse.json()

      newDebugInfo.serverConfig = {
        nodeEnv: configData.nodeEnv || 'unknown',
        apiUrl: configData.apiUrl || 'unknown',
        port: configData.port || 'unknown'
      }
    } catch (error: any) {
      newDebugInfo.serverConfig = {
        nodeEnv: 'Errore: impossibile recuperare',
        apiUrl: 'Errore: proxy non configurato',
        port: 'unknown'
      }
    }

    try {
      // Test 2: Check backend API health
      // NOTA: /health è l'unico endpoint del backend che NON ha il prefisso /api, usa fetch
      const baseURL = apiClient.defaults.baseURL || ''
      const healthURL = import.meta.env.PROD
        ? baseURL.replace(/\/api$/, '/health')  // Rimuove /api e aggiunge /health
        : '/health'  // In dev, il proxy Vite gestisce correttamente /health

      const controller2 = new AbortController()
      const timeout2 = setTimeout(() => controller2.abort(), 5000)
      const apiHealthResponse = await fetch(healthURL, {
        signal: controller2.signal
      })
      clearTimeout(timeout2)

      if (!apiHealthResponse.ok) throw new Error(`HTTP ${apiHealthResponse.status}`)
      const healthData = await apiHealthResponse.json()

      newDebugInfo.apiHealth = {
        status: 'success',
        message: `API raggiungibile: ${JSON.stringify(healthData)}`,
        checked: true
      }
    } catch (error: any) {
      newDebugInfo.apiHealth = {
        status: 'error',
        message: `Errore: ${error.message || 'Network error'}`,
        checked: true
      }
    }

    try {
      // Test 3: Test auth endpoint availability (without credentials)
      await apiClient.post('/auth/login', {
        email: 'test@test.com',
        password: 'test'
      }, { timeout: 5000 })
    } catch (error: any) {
      if (error.response?.status === 401 || error.response?.status === 400) {
        // Endpoint exists but credentials are wrong - this is good!
        newDebugInfo.proxyTest = {
          status: 'success',
          message: `Endpoint raggiungibile (risposta: ${error.response.status})`,
          checked: true
        }
      } else {
        newDebugInfo.proxyTest = {
          status: 'error',
          message: `Errore: ${error.response?.status || 'Network error'} - ${error.message}`,
          checked: true
        }
      }
    }

    setDebugInfo(newDebugInfo)
    setLoading(false)
  }

  useEffect(() => {
    if (isOpen && !debugInfo.apiHealth.checked) {
      checkApiConfiguration()
    }
  }, [isOpen])

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'success':
        return 'text-green-600'
      case 'error':
        return 'text-red-600'
      case 'pending':
        return 'text-gray-400'
      default:
        return 'text-gray-600'
    }
  }

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'success':
        return '✓'
      case 'error':
        return '✗'
      case 'pending':
        return '○'
      default:
        return '?'
    }
  }

  return (
    <>
      {/* Toggle Button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="fixed bottom-4 right-4 bg-blue-600 text-white p-3 rounded-full shadow-lg hover:bg-blue-700 transition-all z-50"
        title="Console Debug API"
      >
        {isOpen ? '✕' : '🔧'}
      </button>

      {/* Debug Console */}
      {isOpen && (
        <div className="fixed bottom-20 right-4 bg-white border-2 border-gray-300 rounded-lg shadow-2xl p-4 w-[500px] max-h-[600px] overflow-y-auto z-50">
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-lg font-bold text-gray-800">Console Debug API</h3>
            <button
              onClick={checkApiConfiguration}
              disabled={loading}
              className="bg-blue-500 text-white px-3 py-1 rounded text-sm hover:bg-blue-600 disabled:opacity-50"
            >
              {loading ? '...' : '↻ Ricontrolla'}
            </button>
          </div>

          {/* Vite Environment Variables (Build Time) */}
          <div className="mb-3 p-3 bg-purple-50 border border-purple-200 rounded">
            <h4 className="font-semibold text-sm mb-2 text-purple-900">🏗️ Vite Build Config (Compile Time)</h4>
            <div className="space-y-1 text-xs font-mono">
              <div>
                <span className="text-gray-700 font-semibold">MODE:</span>
                <span className="ml-2 text-purple-700">{String(debugInfo.viteEnv.mode)}</span>
              </div>
              <div>
                <span className="text-gray-700 font-semibold">PROD:</span>
                <span className="ml-2 text-purple-700">{String(debugInfo.viteEnv.prod)}</span>
              </div>
              <div>
                <span className="text-gray-700 font-semibold">DEV:</span>
                <span className="ml-2 text-purple-700">{String(debugInfo.viteEnv.dev)}</span>
              </div>
              <div>
                <span className="text-gray-700 font-semibold">VITE_API_URL:</span>
                <span className="ml-2 text-purple-700 break-all">{debugInfo.viteEnv.viteApiUrl}</span>
              </div>
              <div>
                <span className="text-gray-700 font-semibold">BASE_URL:</span>
                <span className="ml-2 text-purple-700">{debugInfo.viteEnv.baseUrl}</span>
              </div>
            </div>
          </div>

          {/* Server Config (Runtime) */}
          <div className="mb-3 p-3 bg-blue-50 border border-blue-200 rounded">
            <h4 className="font-semibold text-sm mb-2 text-blue-900">⚙️ Server Config (Runtime - Azure)</h4>
            <div className="space-y-1 text-xs font-mono">
              <div>
                <span className="text-gray-700 font-semibold">NODE_ENV:</span>
                <span className="ml-2 text-blue-700">{debugInfo.serverConfig.nodeEnv}</span>
              </div>
              <div>
                <span className="text-gray-700 font-semibold">API_URL:</span>
                <span className="ml-2 text-blue-700 break-all">{debugInfo.serverConfig.apiUrl}</span>
              </div>
              <div>
                <span className="text-gray-700 font-semibold">PORT:</span>
                <span className="ml-2 text-blue-700">{debugInfo.serverConfig.port}</span>
              </div>
            </div>
          </div>

          {/* Runtime Info */}
          <div className="mb-3 p-3 bg-green-50 border border-green-200 rounded">
            <h4 className="font-semibold text-sm mb-2 text-green-900">🚀 Runtime Info</h4>
            <div className="space-y-1 text-xs font-mono">
              <div>
                <span className="text-gray-700 font-semibold">App URL:</span>
                <span className="ml-2 text-green-700 break-all">{debugInfo.runtime.appUrl}</span>
              </div>
              <div>
                <span className="text-gray-700 font-semibold">Axios baseURL:</span>
                <span className="ml-2 text-green-700 break-all">{debugInfo.runtime.axiosBaseURL}</span>
              </div>
            </div>
          </div>

          {/* API Health Check */}
          <div className="mb-4 p-3 bg-gray-100 rounded">
            <h4 className="font-semibold text-sm mb-2 text-gray-700">Test Connettività API</h4>

            <div className="space-y-2">
              <div className="flex items-start">
                <span className={`mr-2 font-bold ${getStatusColor(debugInfo.apiHealth.status)}`}>
                  {getStatusIcon(debugInfo.apiHealth.status)}
                </span>
                <div className="flex-1">
                  <div className="text-xs font-semibold text-gray-700">Controllo Salute Backend</div>
                  <div className="text-xs text-gray-600 break-words">{debugInfo.apiHealth.message}</div>
                </div>
              </div>

              <div className="flex items-start">
                <span className={`mr-2 font-bold ${getStatusColor(debugInfo.proxyTest.status)}`}>
                  {getStatusIcon(debugInfo.proxyTest.status)}
                </span>
                <div className="flex-1">
                  <div className="text-xs font-semibold text-gray-700">Test Endpoint Login</div>
                  <div className="text-xs text-gray-600 break-words">{debugInfo.proxyTest.message}</div>
                </div>
              </div>
            </div>
          </div>

          {/* Timestamp */}
          <div className="text-xs text-gray-500 text-right">
            Ultimo controllo: {new Date(debugInfo.timestamp).toLocaleString('it-IT')}
          </div>
        </div>
      )}
    </>
  )
}

export default ApiDebugConsole
