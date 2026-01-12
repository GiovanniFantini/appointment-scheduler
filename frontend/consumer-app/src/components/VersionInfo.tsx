import { useEffect, useState } from 'react'
import apiClient from '../lib/axios'

interface VersionData {
  frontend: string
  backend: string
  environment: string
}

function VersionInfo() {
  const [version, setVersion] = useState<VersionData>({
    frontend: '0.0.1', // Default from package.json
    backend: 'Loading...',
    environment: import.meta.env.MODE || 'development'
  })

  useEffect(() => {
    // Fetch backend version
    const fetchBackendVersion = async () => {
      try {
        const response = await apiClient.get('/version')
        setVersion(prev => ({
          ...prev,
          backend: response.data.version,
          environment: response.data.environment
        }))
      } catch (error) {
        setVersion(prev => ({
          ...prev,
          backend: 'N/A'
        }))
      }
    }

    fetchBackendVersion()
  }, [])

  return (
    <div className="text-xs text-gray-500 text-center mt-4">
      <div className="flex items-center justify-center gap-3">
        <span className="bg-gray-100 px-2 py-1 rounded">
          App v{version.frontend}
        </span>
        <span className="bg-gray-100 px-2 py-1 rounded">
          API v{version.backend}
        </span>
        {version.environment !== 'Production' && (
          <span className="bg-yellow-100 text-yellow-800 px-2 py-1 rounded font-semibold">
            {version.environment}
          </span>
        )}
      </div>
    </div>
  )
}

export default VersionInfo
