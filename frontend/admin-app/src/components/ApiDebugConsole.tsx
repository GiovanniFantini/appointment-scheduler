import { useState, useEffect } from 'react'
import axios from 'axios'

interface ApiDebugInfo {
  timestamp: string
  environment: {
    nodeEnv: string
    apiUrl: string
    appUrl: string
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
    environment: {
      nodeEnv: 'unknown',
      apiUrl: 'unknown',
      appUrl: window.location.origin
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

    try {
      // Test 1: Check proxy health endpoint
      const healthResponse = await axios.get('/api-debug/config', {
        timeout: 5000
      })

      newDebugInfo.environment = {
        ...healthResponse.data,
        appUrl: window.location.origin
      }
    } catch (error: any) {
      newDebugInfo.environment.apiUrl = 'Errore: proxy non configurato correttamente'
      newDebugInfo.environment.nodeEnv = 'Errore: impossibile recuperare'
    }

    try {
      // Test 2: Check backend API health
      const apiHealthResponse = await axios.get('/api/health', {
        timeout: 5000
      })

      newDebugInfo.apiHealth = {
        status: 'success',
        message: `API raggiungibile: ${JSON.stringify(apiHealthResponse.data)}`,
        checked: true
      }
    } catch (error: any) {
      newDebugInfo.apiHealth = {
        status: 'error',
        message: `Errore: ${error.response?.status || 'Network error'} - ${error.message}`,
        checked: true
      }
    }

    try {
      // Test 3: Test auth endpoint availability (without credentials)
      await axios.post('/api/auth/login', {
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
        title="API Debug Console"
      >
        {isOpen ? '✕' : '🔧'}
      </button>

      {/* Debug Console */}
      {isOpen && (
        <div className="fixed bottom-20 right-4 bg-white border-2 border-gray-300 rounded-lg shadow-2xl p-4 w-96 max-h-96 overflow-y-auto z-50">
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-lg font-bold text-gray-800">API Debug Console</h3>
            <button
              onClick={checkApiConfiguration}
              disabled={loading}
              className="bg-blue-500 text-white px-3 py-1 rounded text-sm hover:bg-blue-600 disabled:opacity-50"
            >
              {loading ? '...' : '↻ Ricontrolla'}
            </button>
          </div>

          {/* Environment Info */}
          <div className="mb-4 p-3 bg-gray-100 rounded">
            <h4 className="font-semibold text-sm mb-2 text-gray-700">Configurazione Ambiente</h4>
            <div className="space-y-1 text-xs font-mono">
              <div>
                <span className="text-gray-600">App URL:</span>
                <span className="ml-2 text-blue-600">{debugInfo.environment.appUrl}</span>
              </div>
              <div>
                <span className="text-gray-600">API URL:</span>
                <span className="ml-2 text-blue-600">{debugInfo.environment.apiUrl}</span>
              </div>
              <div>
                <span className="text-gray-600">NODE_ENV:</span>
                <span className="ml-2 text-blue-600">{debugInfo.environment.nodeEnv}</span>
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
                  <div className="text-xs font-semibold text-gray-700">Backend Health Check</div>
                  <div className="text-xs text-gray-600 break-words">{debugInfo.apiHealth.message}</div>
                </div>
              </div>

              <div className="flex items-start">
                <span className={`mr-2 font-bold ${getStatusColor(debugInfo.proxyTest.status)}`}>
                  {getStatusIcon(debugInfo.proxyTest.status)}
                </span>
                <div className="flex-1">
                  <div className="text-xs font-semibold text-gray-700">Login Endpoint Test</div>
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
