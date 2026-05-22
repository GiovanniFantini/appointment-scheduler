export enum HRDocumentType {
  Payslip = 1,
  Contract = 2,
  Bonus = 3,
  Communication = 4,
  LevelChange = 5,
  Certification = 6,
  DisciplinaryAction = 7,
  Invoice = 8,
  PayrollStatement = 9,
  Other = 99,
}

export enum HRDocumentStatus {
  Draft = 1,
  Published = 2,
  Archived = 3,
}

export enum UploadStatus {
  Uploading = 1,
  Completed = 2,
  Failed = 3,
}

export interface HRDocument {
  id: number
  tenantId: number
  employeeId: number
  employeeName: string
  documentType: HRDocumentType
  documentTypeText: string
  title: string
  description?: string
  year?: number
  month?: number
  currentVersion: number
  status: HRDocumentStatus
  statusText: string
  createdAt: string
  updatedAt?: string
}

export interface HRDocumentVersion {
  id: number
  versionNumber: number
  fileName: string
  contentType: string
  fileSizeBytes: number
  changeNotes?: string
  uploadStatus: UploadStatus
  uploadedAt: string
  uploadedByEmail: string
}

export interface HRDocumentDetail extends HRDocument {
  createdByEmail: string
  updatedByEmail?: string
  versions: HRDocumentVersion[]
}

export interface HRDocumentDownload {
  downloadUrl: string
  fileName: string
  expiresAt: string
}

export interface DocumentUploadTarget {
  employeeId: number
  fullName: string
  kind: number
  hasUserAccount: boolean
}

export interface HRDocumentCreateRequest {
  employeeId: number
  documentType: HRDocumentType
  title: string
  description?: string
  year?: number
  month?: number
}

export interface HRDocumentUploadResponse {
  documentId: number
  uploadUrl: string
  blobPath: string
  expiresAt: string
}

export interface HRDocumentVersionUploadResponse {
  versionId: number
  versionNumber: number
  uploadUrl: string
  blobPath: string
  expiresAt: string
}

export interface HRDocumentFinalizeRequest {
  fileName: string
  fileSizeBytes: number
  contentType: string
  fileHash?: string
}

export interface AddVersionRequest {
  changeNotes?: string
}
