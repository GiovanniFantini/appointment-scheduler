import apiClient from '../axios'
import type {
  DocumentUploadTarget,
  HRDocument,
  HRDocumentCreateRequest,
  HRDocumentDetail,
  HRDocumentDownload,
  HRDocumentFinalizeRequest,
  AddVersionRequest,
  HRDocumentType,
  HRDocumentUploadResponse,
  HRDocumentVersionUploadResponse,
} from '../../types/documents'

interface GetMyDocumentsParams {
  documentType?: HRDocumentType
  year?: number
  month?: number
}

export const documentsApi = {
  async getMyDocuments(params: GetMyDocumentsParams = {}): Promise<HRDocument[]> {
    const res = await apiClient.get<HRDocument[]>('/employee/documents', { params })
    return res.data
  },

  async getMyDocumentById(id: number): Promise<HRDocumentDetail> {
    const res = await apiClient.get<HRDocumentDetail>(`/employee/documents/${id}`)
    return res.data
  },

  async getDownloadUrl(id: number, versionNumber?: number): Promise<HRDocumentDownload> {
    const res = await apiClient.get<HRDocumentDownload>(`/employee/documents/${id}/download`, {
      params: { versionNumber },
    })
    return res.data
  },

  async getUploadTargets(): Promise<DocumentUploadTarget[]> {
    const res = await apiClient.get<DocumentUploadTarget[]>('/employee/documents/upload-targets')
    return res.data
  },

  async createForEmployee(payload: HRDocumentCreateRequest): Promise<HRDocumentUploadResponse> {
    const res = await apiClient.post<HRDocumentUploadResponse>('/employee/documents', payload)
    return res.data
  },

  async finalizeUpload(id: number, payload: HRDocumentFinalizeRequest): Promise<void> {
    await apiClient.put(`/employee/documents/${id}/finalize`, payload)
  },

  async deleteDocument(id: number): Promise<void> {
    await apiClient.delete(`/employee/documents/${id}`)
  },

  async addVersion(id: number, payload: AddVersionRequest = {}): Promise<HRDocumentVersionUploadResponse> {
    const res = await apiClient.post<HRDocumentVersionUploadResponse>(`/employee/documents/${id}/versions`, payload)
    return res.data
  },
}
