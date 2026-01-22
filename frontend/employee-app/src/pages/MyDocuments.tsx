import { useState, useEffect } from 'react'
import apiClient from '../lib/axios'
import AppLayout from '../components/layout/AppLayout'

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
  user: any
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

function MyDocuments({ user, onLogout }: MyDocumentsProps) {
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
    <AppLayout user={user} onLogout={onLogout} pageTitle="I Miei Documenti">
        {/* Filtri */}
        <div className="glass-card rounded-3xl p-6 mb-8 border border-white/10">
          <div className="flex items-center gap-3 mb-4">
            <div className="p-2 rounded-xl bg-gradient-to-br from-neon-cyan/20 to-neon-blue/20 border border-neon-cyan/30">
              <svg className="w-5 h-5 text-neon-cyan" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" />
              </svg>
            </div>
            <h2 className="text-lg font-semibold gradient-text">Filtra Documenti</h2>
          </div>
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Tipo Documento
              </label>
              <select
                value={filters.documentType}
                onChange={(e) => setFilters({ ...filters, documentType: e.target.value })}
                className="w-full glass-card-dark border border-white/10 rounded-xl px-3 py-2 text-white focus:border-neon-cyan/50 focus:outline-none transition-colors"
              >
                {DOCUMENT_TYPES.map(type => (
                  <option key={type.value} value={type.value}>{type.label}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Anno
              </label>
              <input
                type="number"
                value={filters.year}
                onChange={(e) => setFilters({ ...filters, year: e.target.value })}
                className="w-full glass-card-dark border border-white/10 rounded-xl px-3 py-2 text-white focus:border-neon-cyan/50 focus:outline-none transition-colors"
                placeholder="es. 2026"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Mese
              </label>
              <select
                value={filters.month}
                onChange={(e) => setFilters({ ...filters, month: e.target.value })}
                className="w-full glass-card-dark border border-white/10 rounded-xl px-3 py-2 text-white focus:border-neon-cyan/50 focus:outline-none transition-colors"
              >
                {MONTHS.map(month => (
                  <option key={month.value} value={month.value}>{month.label}</option>
                ))}
              </select>
            </div>
          </div>
        </div>

        {/* Lista Documenti */}
        <div className="glass-card rounded-3xl overflow-hidden border border-white/10 mb-8">
          <div className="px-6 py-4 border-b border-white/10 bg-gradient-to-r from-white/5 to-transparent flex justify-between items-center">
            <h2 className="text-lg font-semibold gradient-text">Documenti Disponibili ({documents.length})</h2>
            <div className="text-sm text-gray-400">
              Ultimi documenti pubblicati
            </div>
          </div>

          {loading ? (
            <div className="p-12 text-center">
              <svg className="animate-spin h-12 w-12 text-neon-purple mx-auto mb-4" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <p className="text-gray-300">Caricamento documenti...</p>
            </div>
          ) : documents.length === 0 ? (
            <div className="p-12 text-center">
              <svg className="w-20 h-20 text-gray-500 mx-auto mb-4 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              <p className="text-gray-400 text-lg mb-2">Nessun documento disponibile</p>
              <p className="text-gray-500 text-sm">
                I documenti pubblicati dal tuo datore di lavoro appariranno qui.
              </p>
            </div>
          ) : (
            <div className="divide-y divide-white/10">
              {documents.map(doc => (
                <div key={doc.id} className="p-6 hover:bg-white/5 transition-colors">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-3 mb-2">
                        <span className="px-2.5 py-1 rounded-lg bg-neon-blue/20 text-neon-blue text-xs font-semibold border border-neon-blue/30">
                          {doc.documentTypeText}
                        </span>
                        <span className="px-2 py-0.5 rounded-lg bg-white/10 text-gray-300 text-xs">
                          v{doc.currentVersion}
                        </span>
                      </div>

                      <h3 className="text-lg font-semibold text-white mb-2">
                        {doc.title}
                      </h3>

                      {doc.description && (
                        <p className="text-sm text-gray-400 mb-3">
                          {doc.description}
                        </p>
                      )}

                      <div className="flex items-center gap-4 text-sm text-gray-400">
                        {doc.month && doc.year && (
                          <span className="flex items-center gap-1">
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                            </svg>
                            {getMonthName(doc.month)} {doc.year}
                          </span>
                        )}
                        {!doc.month && doc.year && (
                          <span className="flex items-center gap-1">
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                            </svg>
                            Anno {doc.year}
                          </span>
                        )}
                        <span className="flex items-center gap-1">
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                          </svg>
                          Pubblicato il {formatDate(doc.createdAt)}
                        </span>
                      </div>
                    </div>

                    <button
                      onClick={() => handleDownload(doc.id)}
                      className="ml-6 px-4 py-2.5 bg-gradient-to-r from-neon-purple to-neon-pink text-white rounded-xl hover:shadow-glow-purple transition-all font-semibold flex items-center gap-2"
                    >
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
        <div className="glass-card rounded-3xl p-6 border border-neon-blue/30">
          <div className="flex gap-4">
            <div className="flex-shrink-0">
              <div className="p-3 rounded-xl bg-gradient-to-br from-neon-blue/20 to-neon-cyan/20 border border-neon-blue/30">
                <svg className="w-6 h-6 text-neon-blue" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                </svg>
              </div>
            </div>
            <div className="flex-1">
              <h3 className="text-lg font-semibold text-neon-blue mb-3">Informazioni sui documenti</h3>
              <ul className="space-y-2 text-sm text-gray-300">
                <li className="flex items-start gap-2">
                  <span className="text-neon-cyan mt-0.5">•</span>
                  <span>I documenti vengono pubblicati dal tuo datore di lavoro</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-neon-cyan mt-0.5">•</span>
                  <span>Puoi scaricare i documenti in qualsiasi momento</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-neon-cyan mt-0.5">•</span>
                  <span>I link di download sono validi per 5 minuti</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-neon-cyan mt-0.5">•</span>
                  <span>Per assistenza, contatta il reparto HR della tua azienda</span>
                </li>
              </ul>
            </div>
          </div>
        </div>
    </AppLayout>
  )
}

export default MyDocuments
