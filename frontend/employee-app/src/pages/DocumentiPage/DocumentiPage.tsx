import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { documentsApi } from '../../lib/api/documents'
import { HRDocumentType } from '../../types/documents'
import type {
  DocumentUploadTarget,
  HRDocument,
  HRDocumentAccessRow,
  HRDocumentDetail,
} from '../../types/documents'
import { formatBrowserDate } from '../../lib/dateUtils'
import './DocumentiPage.css'

type FeatureAccessLevel = 'ReadOnly' | 'Operator' | 'Manager'

interface DocumentiPageProps {
  accessLevel?: FeatureAccessLevel
}

const documentTypeOptions: Array<{ value: 'all' | HRDocumentType; label: string }> = [
  { value: 'all', label: 'Tutti i tipi' },
  { value: 2, label: 'Contratto' },
  { value: 1, label: 'Busta paga' },
  { value: 9, label: 'Cedolino' },
  { value: 3, label: 'Bonus' },
  { value: 4, label: 'Comunicazioni HR' },
  { value: 5, label: 'Variazione livello' },
  { value: 6, label: 'Attestato/Certificazione' },
  { value: 7, label: 'Provvedimento disciplinare' },
  { value: 8, label: 'Fattura' },
  { value: 99, label: 'Altro' },
]

function getRefLabel(year?: number, month?: number): string {
  if (year && month) {
    return `${month.toString().padStart(2, '0')}/${year}`
  }
  if (year) {
    return `${year} - senza mese`
  }
  return 'Senza anno di riferimento'
}

// Limite allineato a MaxUploadSizeBytes nel backend (HRDocumentService).
// Validare lato client evita di caricare il blob per poi vederlo rifiutato a finalize.
const MAX_UPLOAD_SIZE_BYTES = 50 * 1024 * 1024

const ALLOWED_EXTENSIONS = ['pdf', 'doc', 'docx', 'xls', 'xlsx', 'png', 'jpg', 'jpeg', 'txt']

/**
 * Valida un file prima dell'upload. Ritorna un messaggio d'errore oppure null
 * se il file è accettabile.
 */
function validateUploadFile(file: File): string | null {
  if (file.size <= 0) {
    return 'Il file selezionato è vuoto'
  }
  if (file.size > MAX_UPLOAD_SIZE_BYTES) {
    return 'Il file supera il limite di 50 MB'
  }
  const extension = file.name.split('.').pop()?.toLowerCase()
  if (!extension || !ALLOWED_EXTENSIONS.includes(extension)) {
    return 'Formato non supportato. Ammessi: PDF, Word, Excel, immagini, testo'
  }
  return null
}

function getDefaultContentType(file: File): string {
  if (file.type) {
    return file.type
  }

  const extension = file.name.split('.').pop()?.toLowerCase()
  switch (extension) {
    case 'pdf':
      return 'application/pdf'
    case 'doc':
      return 'application/msword'
    case 'docx':
      return 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
    case 'xls':
      return 'application/vnd.ms-excel'
    case 'xlsx':
      return 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    case 'png':
      return 'image/png'
    case 'jpg':
    case 'jpeg':
      return 'image/jpeg'
    case 'txt':
      return 'text/plain'
    default:
      return 'application/octet-stream'
  }
}

export default function DocumentiPage({ accessLevel = 'ReadOnly' }: DocumentiPageProps) {
  const canUploadForOthers = accessLevel === 'Operator' || accessLevel === 'Manager'
  const canDeleteDocuments = accessLevel === 'Manager'

  const [documents, setDocuments] = useState<HRDocument[]>([])
  const [selectedType, setSelectedType] = useState<'all' | HRDocumentType>('all')
  const [selectedYear, setSelectedYear] = useState<'all' | number>('all')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [loadingId, setLoadingId] = useState<number | null>(null)
  const [selectedDetail, setSelectedDetail] = useState<HRDocumentDetail | null>(null)
  const [detailError, setDetailError] = useState('')
  const [uploadTargets, setUploadTargets] = useState<DocumentUploadTarget[]>([])
  const [targetsError, setTargetsError] = useState('')
  const [uploading, setUploading] = useState(false)
  const [uploadSuccess, setUploadSuccess] = useState('')
  const [uploadForm, setUploadForm] = useState({
    employeeId: '',
    documentType: HRDocumentType.Contract,
    title: '',
    description: '',
    year: '',
    month: '',
  })
  const [uploadFile, setUploadFile] = useState<File | null>(null)
  const [versionFile, setVersionFile] = useState<File | null>(null)
  const [versionNotes, setVersionNotes] = useState('')
  const [ackingVersion, setAckingVersion] = useState<number | null>(null)
  const [accessLog, setAccessLog] = useState<HRDocumentAccessRow[]>([])
  const [accessLogError, setAccessLogError] = useState('')

  const fetchDocuments = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await documentsApi.getMyDocuments({
        documentType: selectedType === 'all' ? undefined : selectedType,
        year: selectedYear === 'all' ? undefined : selectedYear,
      })
      setDocuments(Array.isArray(data) ? data : [])
    } catch {
      setError('Errore nel caricamento dei documenti')
    } finally {
      setLoading(false)
    }
  }, [selectedType, selectedYear])

  useEffect(() => {
    fetchDocuments()
  }, [fetchDocuments])

  const fetchUploadTargets = useCallback(async () => {
    if (!canUploadForOthers) {
      return
    }

    setTargetsError('')
    try {
      const targets = await documentsApi.getUploadTargets()
      setUploadTargets(Array.isArray(targets) ? targets : [])
    } catch {
      setTargetsError('Impossibile caricare l\'elenco risorse per l\'upload delegato')
    }
  }, [canUploadForOthers])

  useEffect(() => {
    fetchUploadTargets()
  }, [fetchUploadTargets])

  const years = useMemo(() => {
    const uniqueYears = new Set<number>()
    documents.forEach(d => {
      if (typeof d.year === 'number') {
        uniqueYears.add(d.year)
      }
    })
    return Array.from(uniqueYears).sort((a, b) => b - a)
  }, [documents])

  const handleDownload = async (id: number, versionNumber?: number) => {
    setLoadingId(id)
    try {
      const payload = await documentsApi.getDownloadUrl(id, versionNumber)
      window.open(payload.downloadUrl, '_blank', 'noopener,noreferrer')
    } catch {
      setError('Download non disponibile al momento')
    } finally {
      setLoadingId(null)
    }
  }

  const handleOpenDetail = async (id: number) => {
    setDetailError('')
    setAccessLog([])
    setAccessLogError('')
    try {
      const detail = await documentsApi.getMyDocumentById(id)
      setSelectedDetail(detail)
    } catch {
      setDetailError('Impossibile aprire il dettaglio documento')
      return
    }

    // Gli operatori vedono anche chi ha scaricato/preso visione.
    if (canUploadForOthers) {
      try {
        const log = await documentsApi.getAccessLog(id)
        setAccessLog(Array.isArray(log) ? log : [])
      } catch {
        setAccessLogError('Impossibile caricare il log accessi')
      }
    }
  }

  const refreshDetail = async (id: number) => {
    try {
      const detail = await documentsApi.getMyDocumentById(id)
      setSelectedDetail(detail)
    } catch {
      setDetailError('Impossibile aggiornare il dettaglio documento')
    }
  }

  const handleAcknowledge = async (id: number, versionNumber: number) => {
    // La presa visione è un atto formale e non revocabile: chiediamo conferma.
    if (!window.confirm(
      `Confermi di aver preso visione della versione ${versionNumber}? ` +
      'L\'azione viene registrata e non può essere annullata.'
    )) {
      return
    }

    setDetailError('')
    setAckingVersion(versionNumber)
    try {
      await documentsApi.acknowledgeVersion(id, versionNumber)
      await refreshDetail(id)
    } catch {
      setDetailError('Impossibile registrare la presa visione')
    } finally {
      setAckingVersion(null)
    }
  }

  const handleDeleteDocument = async (id: number) => {
    if (!window.confirm('Confermi la cancellazione di questo documento?')) {
      return
    }

    setError('')
    try {
      await documentsApi.deleteDocument(id)
      setSelectedDetail(null)
      await fetchDocuments()
    } catch {
      setError('Impossibile cancellare il documento')
    }
  }

  const handleAddVersion = async (id: number) => {
    setError('')

    if (!versionFile) {
      setError('Seleziona un file per la nuova versione')
      return
    }

    const versionFileError = validateUploadFile(versionFile)
    if (versionFileError) {
      setError(versionFileError)
      return
    }

    setUploading(true)
    try {
      const versionPayload = await documentsApi.addVersion(id, {
        changeNotes: versionNotes.trim() || undefined,
      })

      const contentType = getDefaultContentType(versionFile)
      const uploadRes = await fetch(versionPayload.uploadUrl, {
        method: 'PUT',
        headers: {
          'Content-Type': contentType,
          'x-ms-blob-type': 'BlockBlob',
        },
        body: versionFile,
      })

      if (!uploadRes.ok) {
        throw new Error('Upload nuova versione non riuscito')
      }

      await documentsApi.finalizeUpload(id, {
        fileName: versionFile.name,
        fileSizeBytes: versionFile.size,
        contentType,
      })

      setVersionFile(null)
      setVersionNotes('')
      setSelectedDetail(null)
      await fetchDocuments()
    } catch {
      setError('Impossibile caricare la nuova versione')
    } finally {
      setUploading(false)
    }
  }

  const handleUploadSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setUploadSuccess('')

    if (!uploadForm.employeeId) {
      setError('Seleziona la risorsa destinataria del documento')
      return
    }

    if (!uploadForm.title.trim()) {
      setError('Inserisci un titolo documento')
      return
    }

    if (!uploadFile) {
      setError('Seleziona un file da caricare')
      return
    }

    const uploadFileError = validateUploadFile(uploadFile)
    if (uploadFileError) {
      setError(uploadFileError)
      return
    }

    setUploading(true)
    try {
      const yearNumber = uploadForm.year ? Number(uploadForm.year) : undefined
      const monthNumber = uploadForm.month ? Number(uploadForm.month) : undefined

      const createPayload = await documentsApi.createForEmployee({
        employeeId: Number(uploadForm.employeeId),
        documentType: uploadForm.documentType,
        title: uploadForm.title.trim(),
        description: uploadForm.description.trim() || undefined,
        year: Number.isFinite(yearNumber) ? yearNumber : undefined,
        month: Number.isFinite(monthNumber) ? monthNumber : undefined,
      })

      const contentType = getDefaultContentType(uploadFile)
      const uploadRes = await fetch(createPayload.uploadUrl, {
        method: 'PUT',
        headers: {
          'Content-Type': contentType,
          'x-ms-blob-type': 'BlockBlob',
        },
        body: uploadFile,
      })

      if (!uploadRes.ok) {
        throw new Error('Upload blob non riuscito')
      }

      await documentsApi.finalizeUpload(createPayload.documentId, {
        fileName: uploadFile.name,
        fileSizeBytes: uploadFile.size,
        contentType,
      })

      setUploadSuccess('Documento caricato e pubblicato con successo')
      setUploadForm({
        employeeId: '',
        documentType: HRDocumentType.Contract,
        title: '',
        description: '',
        year: '',
        month: '',
      })
      setUploadFile(null)
      await fetchDocuments()
    } catch {
      setError('Errore durante l\'upload del documento delegato')
    } finally {
      setUploading(false)
    }
  }

  return (
    <div className="documenti-page">
      <div className="documenti-header">
        <h1 className="documenti-title">I miei documenti HR</h1>
      </div>

      {canUploadForOthers && (
        <form className="documenti-upload-card" onSubmit={handleUploadSubmit}>
          <h2 className="documenti-upload-title">Carica documento per altra risorsa</h2>

          {targetsError && <div className="documenti-error">{targetsError}</div>}
          {uploadSuccess && <div className="documenti-success">{uploadSuccess}</div>}

          <div className="documenti-upload-grid">
            <label className="documenti-filter-label" htmlFor="uploadEmployee">Destinatario</label>
            <select
              id="uploadEmployee"
              className="documenti-filter-select"
              value={uploadForm.employeeId}
              onChange={(e) => setUploadForm(prev => ({ ...prev, employeeId: e.target.value }))}
              required
            >
              <option value="">Seleziona risorsa</option>
              {uploadTargets.map(target => (
                <option key={target.employeeId} value={target.employeeId}>
                  {target.fullName}{target.hasUserAccount ? '' : ' (senza account)'}
                </option>
              ))}
            </select>

            <label className="documenti-filter-label" htmlFor="uploadType">Tipo</label>
            <select
              id="uploadType"
              className="documenti-filter-select"
              value={uploadForm.documentType}
              onChange={(e) => setUploadForm(prev => ({ ...prev, documentType: Number(e.target.value) as HRDocumentType }))}
            >
              {documentTypeOptions.filter(x => x.value !== 'all').map(option => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>

            <label className="documenti-filter-label" htmlFor="uploadTitle">Titolo</label>
            <input
              id="uploadTitle"
              className="documenti-filter-select"
              value={uploadForm.title}
              onChange={(e) => setUploadForm(prev => ({ ...prev, title: e.target.value }))}
              placeholder="Es. Cedolino maggio 2026"
              maxLength={150}
              required
            />

            <label className="documenti-filter-label" htmlFor="uploadDescription">Descrizione</label>
            <input
              id="uploadDescription"
              className="documenti-filter-select"
              value={uploadForm.description}
              onChange={(e) => setUploadForm(prev => ({ ...prev, description: e.target.value }))}
              placeholder="Note opzionali"
              maxLength={500}
            />

            <label className="documenti-filter-label" htmlFor="uploadYear">Anno rif.</label>
            <input
              id="uploadYear"
              type="number"
              className="documenti-filter-select"
              value={uploadForm.year}
              onChange={(e) => setUploadForm(prev => ({ ...prev, year: e.target.value }))}
              min={2000}
              max={2100}
              placeholder="opzionale"
            />

            <label className="documenti-filter-label" htmlFor="uploadMonth">Mese rif.</label>
            <input
              id="uploadMonth"
              type="number"
              className="documenti-filter-select"
              value={uploadForm.month}
              onChange={(e) => setUploadForm(prev => ({ ...prev, month: e.target.value }))}
              min={1}
              max={12}
              placeholder="opzionale"
            />

            <label className="documenti-filter-label" htmlFor="uploadFile">File</label>
            <input
              id="uploadFile"
              className="documenti-filter-select"
              type="file"
              onChange={(e) => setUploadFile(e.target.files?.[0] ?? null)}
              required
            />
          </div>

          <div className="documenti-upload-actions">
            <button className="btn-primary" type="submit" disabled={uploading}>
              {uploading ? 'Caricamento in corso...' : 'Carica documento'}
            </button>
          </div>
        </form>
      )}

      <div className="documenti-filters">
        <label className="documenti-filter-label" htmlFor="docTypeFilter">Tipo documento</label>
        <select
          id="docTypeFilter"
          className="documenti-filter-select"
          value={selectedType}
          onChange={(e) => {
            const value = e.target.value
            setSelectedType(value === 'all' ? 'all' : Number(value) as HRDocumentType)
          }}
        >
          {documentTypeOptions.map(option => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>

        <label className="documenti-filter-label" htmlFor="docYearFilter">Anno</label>
        <select
          id="docYearFilter"
          className="documenti-filter-select"
          value={selectedYear}
          onChange={(e) => {
            const value = e.target.value
            setSelectedYear(value === 'all' ? 'all' : Number(value))
          }}
        >
          <option value="all">Tutti gli anni</option>
          {years.map(year => (
            <option key={year} value={year}>{year}</option>
          ))}
        </select>
      </div>

      {error && <div className="documenti-error">{error}</div>}
      {detailError && <div className="documenti-error">{detailError}</div>}

      {loading ? (
        <div className="documenti-loading">
          <div className="spinner" />
        </div>
      ) : documents.length === 0 ? (
        <div className="documenti-empty">
          <div className="empty-illustration">
            <svg viewBox="0 0 80 80" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect width="80" height="80" rx="20" fill="rgba(99,102,241,0.08)" />
              <path
                d="M50 20H28a4 4 0 00-4 4v32a4 4 0 004 4h24a4 4 0 004-4V30l-6-10z"
                stroke="#6366f1"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
              <path
                d="M50 20v10h10M35 40h10M35 46h10M35 52h6"
                stroke="#6366f1"
                strokeWidth="2"
                strokeLinecap="round"
              />
            </svg>
          </div>
          <h2 className="empty-title">Nessun documento disponibile</h2>
          <p className="empty-subtitle">
            I documenti verranno pubblicati dal tuo responsabile.
            <br />
            Verrai notificato quando saranno disponibili.
          </p>
        </div>
      ) : (
        <div className="documenti-list">
          {documents.map(doc => (
            <div className="documenti-item" key={doc.id}>
              <div className="documenti-item-main">
                <div className="documenti-item-title">{doc.title}</div>
                <div className="documenti-item-meta">
                  <span>{doc.documentTypeText}</span>
                  <span>{getRefLabel(doc.year, doc.month)}</span>
                  <span>Versione {doc.currentVersion}</span>
                  <span>Pubblicato {formatBrowserDate(new Date(doc.createdAt))}</span>
                </div>
              </div>
              <div className="documenti-item-actions">
                <button
                  className="btn-link"
                  type="button"
                  onClick={() => handleOpenDetail(doc.id)}
                >
                  Dettaglio
                </button>
                <button
                  className="btn-primary"
                  type="button"
                  onClick={() => handleDownload(doc.id)}
                  disabled={loadingId === doc.id}
                >
                  {loadingId === doc.id ? 'Apro...' : 'Scarica'}
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {selectedDetail && (
        <div className="documenti-modal-backdrop" role="presentation" onClick={() => setSelectedDetail(null)}>
          <div className="documenti-modal" role="dialog" aria-modal="true" onClick={e => e.stopPropagation()}>
            <div className="documenti-modal-header">
              <h2>{selectedDetail.title}</h2>
              <button type="button" className="btn-link" onClick={() => setSelectedDetail(null)}>Chiudi</button>
            </div>
            <div className="documenti-versions-list">
              {selectedDetail.versions.map(version => (
                <div className="documenti-version-item" key={version.id}>
                  <div className="documenti-version-info">
                    <div className="documenti-item-title">Versione {version.versionNumber}</div>
                    <div className="documenti-item-meta">
                      <span>{version.fileName}</span>
                      <span>{Math.max(1, Math.round(version.fileSizeBytes / 1024))} KB</span>
                      <span>{formatBrowserDate(new Date(version.uploadedAt))}</span>
                    </div>
                    <div className="documenti-version-ack">
                      {version.acknowledgedAt ? (
                        <span className="documenti-ack-badge documenti-ack-badge--done">
                          ✓ Presa visione il {formatBrowserDate(new Date(version.acknowledgedAt))}
                        </span>
                      ) : (
                        <span className="documenti-ack-badge documenti-ack-badge--pending">
                          Presa visione non confermata
                        </span>
                      )}
                      {version.lastDownloadedAt && (
                        <span className="documenti-ack-note">
                          Scaricato {version.downloadCount}× — ultimo {formatBrowserDate(new Date(version.lastDownloadedAt))}
                        </span>
                      )}
                    </div>
                  </div>
                  <div className="documenti-version-actions">
                    <button
                      className="btn-primary"
                      type="button"
                      onClick={() => handleDownload(selectedDetail.id, version.versionNumber)}
                      disabled={loadingId === selectedDetail.id}
                    >
                      Scarica
                    </button>
                    {!version.acknowledgedAt && (
                      <button
                        className="btn-link"
                        type="button"
                        onClick={() => handleAcknowledge(selectedDetail.id, version.versionNumber)}
                        disabled={ackingVersion === version.versionNumber}
                      >
                        {ackingVersion === version.versionNumber ? 'Registro...' : 'Confermo presa visione'}
                      </button>
                    )}
                  </div>
                </div>
              ))}

              {canUploadForOthers && (
                <div className="documenti-access-log">
                  <div className="documenti-upload-title">Stato presa visione</div>
                  {accessLogError && <div className="documenti-error">{accessLogError}</div>}
                  {accessLog.length === 0 ? (
                    <p className="documenti-ack-note">Nessun accesso registrato finora.</p>
                  ) : (
                    <table className="documenti-access-table">
                      <thead>
                        <tr>
                          <th>Risorsa</th>
                          <th>Versione</th>
                          <th>Download</th>
                          <th>Ultimo download</th>
                          <th>Presa visione</th>
                        </tr>
                      </thead>
                      <tbody>
                        {accessLog.map(row => (
                          <tr key={`${row.versionNumber}-${row.employeeId}`}>
                            <td>{row.employeeName}</td>
                            <td>v{row.versionNumber}</td>
                            <td>{row.downloadCount}</td>
                            <td>{row.lastDownloadedAt ? formatBrowserDate(new Date(row.lastDownloadedAt)) : '—'}</td>
                            <td>{row.acknowledgedAt ? formatBrowserDate(new Date(row.acknowledgedAt)) : '—'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>
              )}
              {accessLevel !== 'ReadOnly' && (
                <div className="documenti-version-upload">
                  <div className="documenti-upload-title">Nuova versione</div>
                  <div className="documenti-upload-grid">
                    <label className="documenti-filter-label" htmlFor="versionNotes">Note</label>
                    <input
                      id="versionNotes"
                      className="documenti-filter-select"
                      value={versionNotes}
                      onChange={(e) => setVersionNotes(e.target.value)}
                      placeholder="Note opzionali per la nuova versione"
                      maxLength={500}
                    />
                    <label className="documenti-filter-label" htmlFor="versionFile">File</label>
                    <input
                      id="versionFile"
                      className="documenti-filter-select"
                      type="file"
                      onChange={(e) => setVersionFile(e.target.files?.[0] ?? null)}
                    />
                  </div>
                  <div className="documenti-upload-actions">
                    <button
                      className="btn-primary"
                      type="button"
                      onClick={() => handleAddVersion(selectedDetail.id)}
                      disabled={uploading}
                    >
                      {uploading ? 'Caricamento...' : 'Carica nuova versione'}
                    </button>
                  </div>
                </div>
              )}
              {canDeleteDocuments && (
                <div className="documenti-modal-footer">
                  <button
                    className="btn-danger"
                    type="button"
                    onClick={() => handleDeleteDocument(selectedDetail.id)}
                  >
                    Elimina documento
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
