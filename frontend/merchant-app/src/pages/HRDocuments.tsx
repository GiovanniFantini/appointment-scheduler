import { useState, useEffect } from 'react'
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
    <div className="min-h-screen bg-gray-100">
      {/* Header */}
      <div className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
          <div className="flex justify-between items-center">
            <h1 className="text-2xl font-bold text-gray-900">Documenti HR/Payroll</h1>
            <div className="flex gap-4">
              <button
                onClick={() => setShowUploadForm(!showUploadForm)}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
              >
                {showUploadForm ? 'Chiudi' : 'Carica Documento'}
              </button>
              <button
                onClick={onLogout}
                className="bg-gray-600 text-white px-4 py-2 rounded-lg hover:bg-gray-700"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Upload Form */}
        {showUploadForm && (
          <div className="bg-white shadow rounded-lg p-6 mb-8">
            <h2 className="text-xl font-semibold mb-4">Carica Nuovo Documento</h2>
            <form onSubmit={handleUpload} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Dipendente *
                  </label>
                  <select
                    value={uploadForm.employeeId}
                    onChange={(e) => setUploadForm({ ...uploadForm, employeeId: e.target.value })}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2"
                    required
                  >
                    <option value="">Seleziona dipendente</option>
                    {employees.map(emp => (
                      <option key={emp.id} value={emp.id}>{emp.fullName}</option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Tipo Documento *
                  </label>
                  <select
                    value={uploadForm.documentType}
                    onChange={(e) => setUploadForm({ ...uploadForm, documentType: e.target.value })}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2"
                    required
                  >
                    {DOCUMENT_TYPES.map(type => (
                      <option key={type.value} value={type.value}>{type.label}</option>
                    ))}
                  </select>
                </div>

                <div className="col-span-2">
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Titolo *
                  </label>
                  <input
                    type="text"
                    value={uploadForm.title}
                    onChange={(e) => setUploadForm({ ...uploadForm, title: e.target.value })}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2"
                    placeholder="es. Busta Paga Gennaio 2026"
                    required
                  />
                </div>

                <div className="col-span-2">
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Descrizione
                  </label>
                  <textarea
                    value={uploadForm.description}
                    onChange={(e) => setUploadForm({ ...uploadForm, description: e.target.value })}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2"
                    rows={2}
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Anno
                  </label>
                  <input
                    type="number"
                    value={uploadForm.year}
                    onChange={(e) => setUploadForm({ ...uploadForm, year: e.target.value })}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2"
                    min="2000"
                    max="2099"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Mese
                  </label>
                  <select
                    value={uploadForm.month}
                    onChange={(e) => setUploadForm({ ...uploadForm, month: e.target.value })}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2"
                  >
                    <option value="">-</option>
                    {MONTHS.map(month => (
                      <option key={month.value} value={month.value}>{month.label}</option>
                    ))}
                  </select>
                </div>

                <div className="col-span-2">
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    File *
                  </label>
                  <input
                    type="file"
                    accept=".pdf,.docx,.jpg,.png"
                    onChange={(e) => setUploadForm({ ...uploadForm, file: e.target.files?.[0] || null })}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2"
                    required
                  />
                  <p className="text-xs text-gray-500 mt-1">
                    Formati accettati: PDF, DOCX, JPG, PNG (max 10MB)
                  </p>
                </div>
              </div>

              <div className="flex justify-end gap-4">
                <button
                  type="button"
                  onClick={() => {
                    setShowUploadForm(false)
                    resetUploadForm()
                  }}
                  className="bg-gray-300 text-gray-700 px-6 py-2 rounded-lg hover:bg-gray-400"
                  disabled={uploading}
                >
                  Annulla
                </button>
                <button
                  type="submit"
                  className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50"
                  disabled={uploading}
                >
                  {uploading ? 'Caricamento...' : 'Carica'}
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Filtri */}
        <div className="bg-white shadow rounded-lg p-6 mb-8">
          <h2 className="text-lg font-semibold mb-4">Filtri</h2>
          <div className="grid grid-cols-4 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Dipendente
              </label>
              <select
                value={filters.employeeId}
                onChange={(e) => setFilters({ ...filters, employeeId: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
              >
                <option value="">Tutti</option>
                {employees.map(emp => (
                  <option key={emp.id} value={emp.id}>{emp.fullName}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Tipo
              </label>
              <select
                value={filters.documentType}
                onChange={(e) => setFilters({ ...filters, documentType: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
              >
                <option value="">Tutti</option>
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
                <option value="">Tutti</option>
                {MONTHS.map(month => (
                  <option key={month.value} value={month.value}>{month.label}</option>
                ))}
              </select>
            </div>
          </div>
        </div>

        {/* Lista Documenti */}
        <div className="bg-white shadow rounded-lg overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200">
            <h2 className="text-lg font-semibold">Documenti ({documents.length})</h2>
          </div>

          {loading ? (
            <div className="p-8 text-center text-gray-500">Caricamento...</div>
          ) : documents.length === 0 ? (
            <div className="p-8 text-center text-gray-500">
              Nessun documento trovato. Carica il primo documento!
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                      Dipendente
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                      Tipo
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                      Titolo
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                      Periodo
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                      Versione
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                      Stato
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                      Creato
                    </th>
                    <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                      Azioni
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {documents.map(doc => (
                    <tr key={doc.id} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {doc.employeeName}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {doc.documentTypeText}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-900">
                        {doc.title}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {doc.month && doc.year ? `${doc.month}/${doc.year}` : doc.year || '-'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        v{doc.currentVersion}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className={`px-2 py-1 text-xs font-semibold rounded-full ${
                          doc.status === 'Published'
                            ? 'bg-green-100 text-green-800'
                            : doc.status === 'Draft'
                            ? 'bg-yellow-100 text-yellow-800'
                            : 'bg-gray-100 text-gray-800'
                        }`}>
                          {doc.statusText}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {formatDate(doc.createdAt)}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                        <button
                          onClick={() => handleDownload(doc.id)}
                          className="text-blue-600 hover:text-blue-900 mr-4"
                        >
                          Scarica
                        </button>
                        <button
                          onClick={() => handleDelete(doc.id)}
                          className="text-red-600 hover:text-red-900"
                        >
                          Elimina
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
