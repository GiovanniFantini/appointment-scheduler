import { useState, useEffect } from 'react'
import apiClient from '../lib/axios'

interface HRDocument {
  id: number
  documentType: string
  documentTypeText: string
  title: string
  description: string | null
  year: number | null
  month: number | null
  currentVersion: number
  createdAt: string
}

interface MyDocumentsProps {
  onLogout: () => void
}

const DOCUMENT_TYPES = [
  { value: '', label: 'Tutti' },
  { value: 'Payslip', label: 'Busta Paga' },
  { value: 'Contract', label: 'Contratto' },
  { value: 'Bonus', label: 'Bonus' },
  { value: 'Communication', label: 'Comunicazione' },
  { value: 'LevelChange', label: 'Cambio Livello' },
  { value: 'Other', label: 'Altro' }
]

const MONTHS = [
  { value: '', label: 'Tutti' },
  { value: '1', label: 'Gennaio' },
  { value: '2', label: 'Febbraio' },
  { value: '3', label: 'Marzo' },
  { value: '4', label: 'Aprile' },
  { value: '5', label: 'Maggio' },
  { value: '6', label: 'Giugno' },
  { value: '7', label: 'Luglio' },
  { value: '8', label: 'Agosto' },
  { value: '9', label: 'Settembre' },
  { value: '10', label: 'Ottobre' },
  { value: '11', label: 'Novembre' },
  { value: '12', label: 'Dicembre' }
]

function MyDocuments({ onLogout }: MyDocumentsProps) {
  const [documents, setDocuments] = useState<HRDocument[]>([])
  const [loading, setLoading] = useState(true)
  const [filters, setFilters] = useState({
    documentType: '',
    year: '',
    month: ''
  })

  useEffect(() => {
    fetchDocuments()
  }, [filters])

  const fetchDocuments = async () => {
    try {
      setLoading(true)
      const params = new URLSearchParams()
      if (filters.documentType) params.append('documentType', filters.documentType)
      if (filters.year) params.append('year', filters.year)
      if (filters.month) params.append('month', filters.month)

      const response = await apiClient.get(`/employee/documents?${params.toString()}`)
      setDocuments(response.data)
    } catch (err: any) {
      console.error('Errore caricamento documenti:', err)
      if (err.response?.status === 400) {
        alert('Errore: Assicurati di essere registrato come dipendente.')
      } else {
        alert('Errore nel caricamento dei documenti')
      }
    } finally {
      setLoading(false)
    }
  }

  const handleDownload = async (documentId: number) => {
    try {
      const response = await apiClient.get(`/employee/documents/${documentId}/download`)
      const { downloadUrl, fileName } = response.data

      // Trigger download
      const link = document.createElement('a')
      link.href = downloadUrl
      link.download = fileName
      link.click()
    } catch (err: any) {
      console.error('Errore download:', err)
      alert(err.response?.data?.message || 'Errore durante il download')
    }
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('it-IT')
  }

  const getMonthName = (month: number) => {
    const monthObj = MONTHS.find(m => m.value === month.toString())
    return monthObj?.label || month
  }

  return (
    <div className="min-h-screen bg-gray-100">
      {/* Header */}
      <div className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
          <div className="flex justify-between items-center">
            <h1 className="text-2xl font-bold text-gray-900">I Miei Documenti</h1>
            <button
              onClick={onLogout}
              className="bg-gray-600 text-white px-4 py-2 rounded-lg hover:bg-gray-700"
            >
              Logout
            </button>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Filtri */}
        <div className="bg-white shadow rounded-lg p-6 mb-8">
          <h2 className="text-lg font-semibold mb-4">Filtra Documenti</h2>
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Tipo Documento
              </label>
              <select
                value={filters.documentType}
                onChange={(e) => setFilters({ ...filters, documentType: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
              >
                {DOCUMENT_TYPES.map(type => (
                  <option key={type.value} value={type.value}>{type.label}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Anno
              </label>
              <input
                type="number"
                value={filters.year}
                onChange={(e) => setFilters({ ...filters, year: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
                placeholder="es. 2026"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Mese
              </label>
              <select
                value={filters.month}
                onChange={(e) => setFilters({ ...filters, month: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
              >
                {MONTHS.map(month => (
                  <option key={month.value} value={month.value}>{month.label}</option>
                ))}
              </select>
            </div>
          </div>
        </div>

        {/* Lista Documenti */}
        <div className="bg-white shadow rounded-lg overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
            <h2 className="text-lg font-semibold">Documenti Disponibili ({documents.length})</h2>
            <div className="text-sm text-gray-500">
              Ultimi documenti pubblicati
            </div>
          </div>

          {loading ? (
            <div className="p-8 text-center text-gray-500">Caricamento documenti...</div>
          ) : documents.length === 0 ? (
            <div className="p-8 text-center">
              <svg className="mx-auto h-12 w-12 text-gray-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              <p className="text-gray-500">
                Nessun documento disponibile al momento.
              </p>
              <p className="text-sm text-gray-400 mt-2">
                I documenti pubblicati dal tuo datore di lavoro appariranno qui.
              </p>
            </div>
          ) : (
            <div className="divide-y divide-gray-200">
              {documents.map(doc => (
                <div key={doc.id} className="p-6 hover:bg-gray-50 transition">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-3 mb-2">
                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                          {doc.documentTypeText}
                        </span>
                        <span className="text-xs text-gray-500">
                          Versione {doc.currentVersion}
                        </span>
                      </div>

                      <h3 className="text-lg font-semibold text-gray-900 mb-1">
                        {doc.title}
                      </h3>

                      {doc.description && (
                        <p className="text-sm text-gray-600 mb-2">
                          {doc.description}
                        </p>
                      )}

                      <div className="flex items-center gap-4 text-sm text-gray-500">
                        {doc.month && doc.year && (
                          <span className="flex items-center">
                            <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                            </svg>
                            {getMonthName(doc.month)} {doc.year}
                          </span>
                        )}
                        {!doc.month && doc.year && (
                          <span className="flex items-center">
                            <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                            </svg>
                            Anno {doc.year}
                          </span>
                        )}
                        <span className="flex items-center">
                          <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                          </svg>
                          Pubblicato il {formatDate(doc.createdAt)}
                        </span>
                      </div>
                    </div>

                    <button
                      onClick={() => handleDownload(doc.id)}
                      className="ml-4 inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-lg shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                    >
                      <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                      </svg>
                      Scarica
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Info Box */}
        <div className="mt-8 bg-blue-50 border border-blue-200 rounded-lg p-6">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-blue-400" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="ml-3">
              <h3 className="text-sm font-medium text-blue-800">Informazioni sui documenti</h3>
              <div className="mt-2 text-sm text-blue-700">
                <ul className="list-disc pl-5 space-y-1">
                  <li>I documenti vengono pubblicati dal tuo datore di lavoro</li>
                  <li>Puoi scaricare i documenti in qualsiasi momento</li>
                  <li>I link di download sono validi per 5 minuti</li>
                  <li>Per assistenza, contatta il reparto HR della tua azienda</li>
                </ul>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default MyDocuments
