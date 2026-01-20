import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import apiClient from '../lib/axios'

interface Employee {
  id: number
  fullName: string
}

interface HRDocument {
  id: number
  employeeId: number
  employeeName: string
  documentType: string
  documentTypeText: string
  title: string
  description: string | null
  year: number | null
  month: number | null
  currentVersion: number
  status: string
  statusText: string
  createdAt: string
  updatedAt: string | null
}

interface HRDocumentsProps {
  onLogout: () => void
}

const DOCUMENT_TYPES = [
  { value: 'Payslip', label: 'Busta Paga' },
  { value: 'Contract', label: 'Contratto' },
  { value: 'Bonus', label: 'Bonus' },
  { value: 'Communication', label: 'Comunicazione' },
  { value: 'LevelChange', label: 'Cambio Livello' },
  { value: 'Other', label: 'Altro' }
]

const MONTHS = [
  { value: 1, label: 'Gennaio' },
  { value: 2, label: 'Febbraio' },
  { value: 3, label: 'Marzo' },
  { value: 4, label: 'Aprile' },
  { value: 5, label: 'Maggio' },
  { value: 6, label: 'Giugno' },
  { value: 7, label: 'Luglio' },
  { value: 8, label: 'Agosto' },
  { value: 9, label: 'Settembre' },
  { value: 10, label: 'Ottobre' },
  { value: 11, label: 'Novembre' },
  { value: 12, label: 'Dicembre' }
]

function HRDocuments({ onLogout }: HRDocumentsProps) {
  const [documents, setDocuments] = useState<HRDocument[]>([])
  const [employees, setEmployees] = useState<Employee[]>([])
  const [loading, setLoading] = useState(true)
  const [showUploadForm, setShowUploadForm] = useState(false)
  const [uploading, setUploading] = useState(false)

  // Filtri
  const [filters, setFilters] = useState({
    employeeId: '',
    documentType: '',
    year: '',
    month: ''
  })

  // Form upload
  const [uploadForm, setUploadForm] = useState({
    employeeId: '',
    documentType: 'Payslip',
    title: '',
    description: '',
    year: new Date().getFullYear().toString(),
    month: (new Date().getMonth() + 1).toString(),
    file: null as File | null
  })

  useEffect(() => {
    fetchEmployees()
    fetchDocuments()
  }, [])

  useEffect(() => {
    fetchDocuments()
  }, [filters])

  const fetchEmployees = async () => {
    try {
      const response = await apiClient.get('/employees/my-employees')
      setEmployees(response.data)
    } catch (err) {
      console.error('Errore caricamento dipendenti:', err)
    }
  }

  const fetchDocuments = async () => {
    try {
      setLoading(true)
      const params = new URLSearchParams()
      if (filters.employeeId) params.append('employeeId', filters.employeeId)
      if (filters.documentType) params.append('documentType', filters.documentType)
      if (filters.year) params.append('year', filters.year)
      if (filters.month) params.append('month', filters.month)

      const response = await apiClient.get(`/hrdocuments?${params.toString()}`)
      setDocuments(response.data)
    } catch (err) {
      console.error('Errore caricamento documenti:', err)
      alert('Errore nel caricamento dei documenti')
    } finally {
      setLoading(false)
    }
  }

  const handleUpload = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!uploadForm.file) {
      alert('Seleziona un file da caricare')
      return
    }

    if (!uploadForm.employeeId) {
      alert('Seleziona un dipendente')
      return
    }

    try {
      setUploading(true)

      // Step 1: Crea documento e ottieni upload URL
      const createResponse = await apiClient.post('/hrdocuments', {
        employeeId: parseInt(uploadForm.employeeId),
        documentType: uploadForm.documentType,
        title: uploadForm.title,
        description: uploadForm.description || null,
        year: uploadForm.year ? parseInt(uploadForm.year) : null,
        month: uploadForm.month ? parseInt(uploadForm.month) : null
      })

      const { documentId, uploadUrl } = createResponse.data

      // Step 2: Upload file al blob storage
      await fetch(uploadUrl, {
        method: 'PUT',
        headers: {
          'x-ms-blob-type': 'BlockBlob',
          'Content-Type': uploadForm.file.type
        },
        body: uploadForm.file
      })

      // Step 3: Finalizza upload
      await apiClient.put(`/hrdocuments/${documentId}/finalize`, {
        fileName: uploadForm.file.name,
        fileSizeBytes: uploadForm.file.size,
        contentType: uploadForm.file.type
      })

      alert('Documento caricato con successo!')
      setShowUploadForm(false)
      resetUploadForm()
      fetchDocuments()
    } catch (err: any) {
      console.error('Errore upload:', err)
      alert(err.response?.data?.message || 'Errore durante il caricamento del documento')
    } finally {
      setUploading(false)
    }
  }

  const handleDownload = async (documentId: number) => {
    try {
      const response = await apiClient.get(`/hrdocuments/${documentId}/download`)
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

  const handleDelete = async (documentId: number) => {
    if (!confirm('Sei sicuro di voler eliminare questo documento?')) return

    try {
      await apiClient.delete(`/hrdocuments/${documentId}`)
      alert('Documento eliminato')
      fetchDocuments()
    } catch (err: any) {
      console.error('Errore eliminazione:', err)
      alert(err.response?.data?.message || 'Errore durante l\'eliminazione')
    }
  }

  const resetUploadForm = () => {
    setUploadForm({
      employeeId: '',
      documentType: 'Payslip',
      title: '',
      description: '',
      year: new Date().getFullYear().toString(),
      month: (new Date().getMonth() + 1).toString(),
      file: null
    })
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('it-IT')
  }

  return (
    <div className="min-h-screen bg-gradient-dark">
      {/* Header */}
      <header className="glass-card border-b border-white/10 sticky top-0 z-40 backdrop-blur-xl">
        <div className="container mx-auto px-4 py-4">
          <div className="flex justify-between items-center">
            <h1 className="text-2xl font-bold gradient-text flex items-center gap-2">
              <svg className="w-8 h-8 text-neon-pink" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              Documenti HR/Payroll
            </h1>
            <div className="flex items-center gap-3">
              <button
                onClick={() => setShowUploadForm(!showUploadForm)}
                className={`px-5 py-2.5 rounded-xl transition-all font-semibold border ${
                  showUploadForm
                    ? 'bg-gradient-to-r from-red-500/20 to-red-600/20 text-red-300 border-red-500/50 hover:shadow-glow-pink'
                    : 'bg-gradient-to-r from-neon-pink/20 to-neon-purple/20 text-neon-pink border-neon-pink/50 hover:shadow-glow-pink'
                }`}
              >
                {showUploadForm ? '✕ Chiudi' : '+ Carica Documento'}
              </button>
              <Link to="/" className="glass-card-dark px-5 py-2.5 rounded-xl hover:border-neon-blue/50 transition-all font-semibold text-gray-300 hover:text-neon-blue border border-white/10">
                Dashboard
              </Link>
              <button onClick={onLogout} className="glass-card-dark px-5 py-2.5 rounded-xl hover:border-red-500/50 transition-all font-semibold text-gray-300 hover:text-red-400 border border-white/10">
                Esci
              </button>
            </div>
          </div>
        </div>
      </header>

      <div className="container mx-auto px-4 py-8">
        {/* Upload Form */}
        {showUploadForm && (
          <div className="glass-card rounded-3xl p-6 mb-8 border border-neon-pink/30 animate-scale-in">
            <div className="flex items-center gap-3 mb-6">
              <div className="p-3 rounded-xl bg-gradient-to-br from-neon-pink/20 to-neon-purple/20 border border-neon-pink/30">
                <svg className="w-6 h-6 text-neon-pink" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                </svg>
              </div>
              <h2 className="text-xl font-bold gradient-text">Carica Nuovo Documento</h2>
            </div>
            <form onSubmit={handleUpload} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    Dipendente *
                  </label>
                  <select
                    value={uploadForm.employeeId}
                    onChange={(e) => setUploadForm({ ...uploadForm, employeeId: e.target.value })}
                    className="w-full glass-card-dark border border-white/10 rounded-xl px-4 py-2.5 text-white focus:border-neon-pink/50 focus:outline-none transition-colors"
                    required
                  >
                    <option value="">Seleziona dipendente</option>
                    {employees.map(emp => (
                      <option key={emp.id} value={emp.id}>{emp.fullName}</option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    Tipo Documento *
                  </label>
                  <select
                    value={uploadForm.documentType}
                    onChange={(e) => setUploadForm({ ...uploadForm, documentType: e.target.value })}
                    className="w-full glass-card-dark border border-white/10 rounded-xl px-4 py-2.5 text-white focus:border-neon-pink/50 focus:outline-none transition-colors"
                    required
                  >
                    {DOCUMENT_TYPES.map(type => (
                      <option key={type.value} value={type.value}>{type.label}</option>
                    ))}
                  </select>
                </div>

                <div className="col-span-2">
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    Titolo *
                  </label>
                  <input
                    type="text"
                    value={uploadForm.title}
                    onChange={(e) => setUploadForm({ ...uploadForm, title: e.target.value })}
                    className="w-full glass-card-dark border border-white/10 rounded-xl px-4 py-2.5 text-white focus:border-neon-pink/50 focus:outline-none transition-colors"
                    placeholder="es. Busta Paga Gennaio 2026"
                    required
                  />
                </div>

                <div className="col-span-2">
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    Descrizione
                  </label>
                  <textarea
                    value={uploadForm.description}
                    onChange={(e) => setUploadForm({ ...uploadForm, description: e.target.value })}
                    className="w-full glass-card-dark border border-white/10 rounded-xl px-4 py-2.5 text-white focus:border-neon-pink/50 focus:outline-none transition-colors"
                    rows={2}
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    Anno
                  </label>
                  <input
                    type="number"
                    value={uploadForm.year}
                    onChange={(e) => setUploadForm({ ...uploadForm, year: e.target.value })}
                    className="w-full glass-card-dark border border-white/10 rounded-xl px-4 py-2.5 text-white focus:border-neon-pink/50 focus:outline-none transition-colors"
                    min="2000"
                    max="2099"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    Mese
                  </label>
                  <select
                    value={uploadForm.month}
                    onChange={(e) => setUploadForm({ ...uploadForm, month: e.target.value })}
                    className="w-full glass-card-dark border border-white/10 rounded-xl px-4 py-2.5 text-white focus:border-neon-pink/50 focus:outline-none transition-colors"
                  >
                    <option value="">-</option>
                    {MONTHS.map(month => (
                      <option key={month.value} value={month.value}>{month.label}</option>
                    ))}
                  </select>
                </div>

                <div className="col-span-2">
                  <label className="block text-sm font-medium text-gray-300 mb-2">
                    File *
                  </label>
                  <input
                    type="file"
                    accept=".pdf,.docx,.jpg,.png"
                    onChange={(e) => setUploadForm({ ...uploadForm, file: e.target.files?.[0] || null })}
                    className="w-full glass-card-dark border border-white/10 rounded-xl px-4 py-2.5 text-white file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-neon-pink/20 file:text-neon-pink hover:file:bg-neon-pink/30 transition-colors"
                    required
                  />
                  <p className="text-xs text-gray-400 mt-2">
                    Formati accettati: PDF, DOCX, JPG, PNG (max 10MB)
                  </p>
                </div>
              </div>

              <div className="flex justify-end gap-4 pt-4">
                <button
                  type="button"
                  onClick={() => {
                    setShowUploadForm(false)
                    resetUploadForm()
                  }}
                  className="glass-card-dark px-6 py-2.5 rounded-xl hover:border-white/20 transition-all font-semibold text-gray-300 border border-white/10"
                  disabled={uploading}
                >
                  Annulla
                </button>
                <button
                  type="submit"
                  className="bg-gradient-to-r from-neon-pink to-neon-purple text-white px-6 py-2.5 rounded-xl hover:shadow-glow-pink transition-all font-semibold disabled:opacity-50"
                  disabled={uploading}
                >
                  {uploading ? 'Caricamento...' : '📤 Carica'}
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Filtri */}
        <div className="glass-card rounded-3xl p-6 mb-8 border border-white/10">
          <div className="flex items-center gap-3 mb-4">
            <div className="p-2 rounded-xl bg-gradient-to-br from-neon-cyan/20 to-neon-blue/20 border border-neon-cyan/30">
              <svg className="w-5 h-5 text-neon-cyan" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" />
              </svg>
            </div>
            <h2 className="text-lg font-semibold gradient-text">Filtri</h2>
          </div>
          <div className="grid grid-cols-4 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Dipendente
              </label>
              <select
                value={filters.employeeId}
                onChange={(e) => setFilters({ ...filters, employeeId: e.target.value })}
                className="w-full glass-card-dark border border-white/10 rounded-xl px-3 py-2 text-white focus:border-neon-cyan/50 focus:outline-none transition-colors"
              >
                <option value="">Tutti</option>
                {employees.map(emp => (
                  <option key={emp.id} value={emp.id}>{emp.fullName}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Tipo
              </label>
              <select
                value={filters.documentType}
                onChange={(e) => setFilters({ ...filters, documentType: e.target.value })}
                className="w-full glass-card-dark border border-white/10 rounded-xl px-3 py-2 text-white focus:border-neon-cyan/50 focus:outline-none transition-colors"
              >
                <option value="">Tutti</option>
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
                <option value="">Tutti</option>
                {MONTHS.map(month => (
                  <option key={month.value} value={month.value}>{month.label}</option>
                ))}
              </select>
            </div>
          </div>
        </div>

        {/* Lista Documenti */}
        <div className="glass-card rounded-3xl overflow-hidden border border-white/10">
          <div className="px-6 py-4 border-b border-white/10 bg-gradient-to-r from-white/5 to-transparent">
            <h2 className="text-lg font-semibold gradient-text">Documenti ({documents.length})</h2>
          </div>

          {loading ? (
            <div className="p-12 text-center">
              <svg className="animate-spin h-12 w-12 text-neon-cyan mx-auto mb-4" fill="none" viewBox="0 0 24 24">
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
              <p className="text-gray-400 text-lg mb-2">Nessun documento trovato</p>
              <p className="text-gray-500 text-sm">Carica il primo documento per iniziare!</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full">
                <thead className="glass-card-dark border-b border-white/10">
                  <tr>
                    <th className="px-6 py-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">
                      Dipendente
                    </th>
                    <th className="px-6 py-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">
                      Tipo
                    </th>
                    <th className="px-6 py-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">
                      Titolo
                    </th>
                    <th className="px-6 py-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">
                      Periodo
                    </th>
                    <th className="px-6 py-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">
                      Versione
                    </th>
                    <th className="px-6 py-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">
                      Stato
                    </th>
                    <th className="px-6 py-4 text-left text-xs font-semibold text-gray-400 uppercase tracking-wider">
                      Creato
                    </th>
                    <th className="px-6 py-4 text-right text-xs font-semibold text-gray-400 uppercase tracking-wider">
                      Azioni
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-white/10">
                  {documents.map(doc => (
                    <tr key={doc.id} className="hover:bg-white/5 transition-colors">
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-white font-medium">
                        {doc.employeeName}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm">
                        <span className="px-2 py-1 rounded-lg bg-neon-cyan/20 text-neon-cyan text-xs font-semibold border border-neon-cyan/30">
                          {doc.documentTypeText}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-300">
                        {doc.title}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-400">
                        {doc.month && doc.year ? `${doc.month}/${doc.year}` : doc.year || '-'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-400">
                        <span className="px-2 py-1 rounded-lg bg-white/10 text-gray-300 text-xs">
                          v{doc.currentVersion}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className={`px-2 py-1 text-xs font-semibold rounded-lg border ${
                          doc.status === 'Published'
                            ? 'bg-neon-green/20 text-neon-green border-neon-green/30'
                            : doc.status === 'Draft'
                            ? 'bg-yellow-500/20 text-yellow-300 border-yellow-500/30'
                            : 'bg-gray-500/20 text-gray-400 border-gray-500/30'
                        }`}>
                          {doc.statusText}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-400">
                        {formatDate(doc.createdAt)}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                        <button
                          onClick={() => handleDownload(doc.id)}
                          className="text-neon-blue hover:text-neon-cyan mr-4 transition-colors"
                        >
                          📥 Scarica
                        </button>
                        <button
                          onClick={() => handleDelete(doc.id)}
                          className="text-red-400 hover:text-red-300 transition-colors"
                        >
                          🗑️ Elimina
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

export default HRDocuments
