# ERP HR/Payroll Document Management - Specifica Tecnica

## 1. Obiettivo

Sistema per gestire, archiviare e distribuire documenti HR/payroll ai dipendenti con:
- Archiviazione sicura e versionata
- Distribuzione con controllo accessi
- Conformità GDPR

**Scope**: Caricamento, versioning e distribuzione documenti (non calcolo stipendi).

## 2. Contesto

### Multi-Tenant Esistente
- 1 account = 1 Merchant (tenant)
- Entità: `Merchant`, `Employee`, `User`
- Auth: JWT Token con `UserId`, `Email`, `Roles`, `MerchantId`
- DB: PostgreSQL + EF Core
- Storage: Azure Blob Storage (da implementare)

## 3. Funzionalità

### Gestione Documenti
- Caricamento per dipendente
- Tipologie: Payslip, Contract, Bonus, Communication, LevelChange, Other
- Filtri: tipo, anno, mese, dipendente
- Versioning: ogni modifica crea nuova versione con storico

### Controllo Accessi

| Ruolo | Permessi |
|-------|----------|
| Admin | Accesso completo tutti tenant |
| Merchant/HR | CRUD documenti proprio tenant |
| Manager | Lettura documenti team (future) |
| Employee | Lettura solo propri documenti |

**Sicurezza:**
- Ogni richiesta verifica `tenantId` da JWT
- Employee vede solo `employeeId` = proprio id
- Isolamento totale cross-tenant

## 4. Architettura

### Integrazione
```
backend/
├── API/Controllers/
│   ├── HRDocumentsController.cs          # Nuovo
│   └── EmployeeDocumentsController.cs    # Nuovo
├── Core/Services/
│   ├── IHRDocumentService.cs             # Nuovo
│   ├── HRDocumentService.cs              # Nuovo
│   ├── IFileStorageService.cs            # Nuovo
│   └── AzureBlobStorageService.cs        # Nuovo
├── Data/ApplicationDbContext.cs          # + DbSet HR entities
└── Shared/Models/
    ├── HRDocument.cs                     # Nuovo
    └── HRDocumentVersion.cs              # Nuovo

frontend/
├── merchant-app/src/pages/hr/           # Nuovo
└── employee-app/src/pages/documents/    # Nuovo
```

## 5. Modello Dati

### HRDocument
```csharp
public class HRDocument
{
    public int Id { get; set; }
    public int TenantId { get; set; }  // FK Merchant
    public int EmployeeId { get; set; }
    public HRDocumentType DocumentType { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int CurrentVersion { get; set; }
    public List<HRDocumentVersion> Versions { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public HRDocumentStatus Status { get; set; }  // Draft/Published/Archived
    public bool IsDeleted { get; set; }
}
```

### HRDocumentVersion
```csharp
public class HRDocumentVersion
{
    public int Id { get; set; }
    public int HRDocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string BlobPath { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileHash { get; set; }  // SHA256
    public string? ChangeNotes { get; set; }
    public DateTime UploadedAt { get; set; }
    public int UploadedByUserId { get; set; }
    public UploadStatus UploadStatus { get; set; }  // Uploading/Completed/Failed
}
```

### Enum
```csharp
public enum HRDocumentType { Payslip = 1, Contract, Bonus, Communication, LevelChange, Other = 99 }
public enum HRDocumentStatus { Draft = 1, Published, Archived }
public enum UploadStatus { Uploading = 1, Completed, Failed }
```

### Indici
```csharp
entity.HasIndex(e => e.TenantId);
entity.HasIndex(e => new { e.TenantId, e.EmployeeId });
entity.HasIndex(e => new { e.TenantId, e.DocumentType, e.Year, e.Month });
entity.HasQueryFilter(e => !e.IsDeleted);
```

## 6. Azure Blob Storage

### Struttura
```
Container: erp-documents (Private)

Path: tenant-{tenantId}/employee-{employeeId}/{documentType}/{year}/{month}/doc-{id}_v{version}.{ext}

Esempio:
tenant-5/employee-12/payslip/2026/01/doc-1001_v1.pdf
```

### Config
```json
{
  "AzureBlobStorage": {
    "ConnectionString": "",  // Da env variable
    "ContainerName": "erp-documents",
    "SasTokenExpirationMinutes": 5
  }
}
```

**NuGet:** `Azure.Storage.Blobs` v12.19.1

### Sicurezza
- Container Private (no anonymous)
- Backend genera SAS token temporanei (5 min)
- SAS specifico per file e operazione (read/write)
- Frontend usa URL firmato per download/upload

## 7. API Endpoints

### HRDocumentsController (Merchant/HR)
**Route:** `/api/hrdocuments`
**Policy:** `MerchantOnly`

```
GET    /api/hrdocuments                     # Lista con filtri
GET    /api/hrdocuments/{id}                # Dettaglio + versioni
POST   /api/hrdocuments                     # Crea (step 1: metadati)
PUT    /api/hrdocuments/{id}/finalize       # Finalizza upload (step 2)
POST   /api/hrdocuments/{id}/versions       # Aggiungi versione
PUT    /api/hrdocuments/{id}                # Aggiorna metadati
DELETE /api/hrdocuments/{id}                # Soft delete
```

### EmployeeDocumentsController (Employee)
**Route:** `/api/employee/documents`
**Policy:** `EmployeeOnly`

```
GET /api/employee/documents                          # Lista propri documenti
GET /api/employee/documents/{id}/download            # Download versione corrente
GET /api/employee/documents/{id}/versions/{v}/download  # Download versione specifica
```

### Upload Flow
```typescript
// 1. Crea documento → ricevi upload URL
const { documentId, uploadUrl } = await api.post('/api/hrdocuments', metadata);

// 2. Upload file direttamente a Blob Storage
await fetch(uploadUrl, { method: 'PUT', body: file, headers: { 'x-ms-blob-type': 'BlockBlob' } });

// 3. Finalizza documento
await api.put(`/api/hrdocuments/${documentId}/finalize`, { fileName, fileSizeBytes, contentType, fileHash });
```

## 8. Servizi Backend

### IFileStorageService
```csharp
Task<string> GenerateUploadSasUrlAsync(string blobPath, int expirationMinutes = 5);
Task<string> GenerateDownloadSasUrlAsync(string blobPath, int expirationMinutes = 5);
Task<bool> BlobExistsAsync(string blobPath);
Task<BlobProperties> GetBlobPropertiesAsync(string blobPath);
Task DeleteBlobAsync(string blobPath);
string BuildBlobPath(int tenantId, int employeeId, HRDocumentType type, int? year, int? month, int docId, int version, string ext);
```

### IHRDocumentService
```csharp
Task<List<HRDocumentDto>> GetDocumentsAsync(int tenantId, filters...);
Task<HRDocumentDetailDto> GetDocumentByIdAsync(int documentId, int tenantId);
Task<HRDocumentUploadResponseDto> CreateDocumentAsync(int tenantId, int userId, HRDocumentCreateDto dto);
Task<bool> FinalizeDocumentUploadAsync(int documentId, int tenantId, HRDocumentFinalizeDto dto);
Task<HRDocumentVersionUploadResponseDto> AddDocumentVersionAsync(int documentId, int tenantId, int userId, string changeNotes);
Task<bool> UpdateDocumentAsync(int documentId, int tenantId, HRDocumentUpdateDto dto);
Task<bool> DeleteDocumentAsync(int documentId, int tenantId);
Task<HRDocumentDownloadDto> GenerateEmployeeDownloadUrlAsync(int documentId, int employeeId, int? versionNumber);
```

## 9. Frontend

### Merchant App
```typescript
Routes:
/hr/documents        # Lista documenti HR
/hr/documents/upload # Upload nuovo documento
/hr/documents/:id    # Dettaglio + versioni
```

### Employee App
```typescript
Routes:
/documents     # Lista documenti personali
/documents/:id # Viewer + download
```

## 10. Sicurezza

### Isolamento Multi-Tenant
- **DB**: Query filter globale per `tenantId`
- **App**: Verifica `tenantId` da JWT in ogni endpoint
- **Storage**: Path blob include `tenant-{id}`, SAS specifico per file

### GDPR
- **Access**: Employee scarica propri documenti
- **Erasure**: Soft delete + hard delete blob
- **Minimization**: Solo metadati necessari
- **Audit**: `CreatedBy`, `UpdatedBy`, timestamps

### Validazioni
- Max size: 10 MB
- Extensions: .pdf, .docx, .jpg, .png
- Content-Type validation
- File hash verification (SHA256)
- SAS token 5 min expiration

## 11. Migration

### Database
```bash
dotnet ef migrations add AddHRPayrollDocuments --project AppointmentScheduler.Data
dotnet ef database update --project AppointmentScheduler.API
```

### Azure Resources
```bash
# Storage Account
az storage account create --name appointmentschedsa --sku Standard_LRS --kind StorageV2

# Container
az storage container create --name erp-documents --account-name appointmentschedsa --public-access off

# App Service Config
az webapp config appsettings set --settings AZURE_STORAGE_CONNECTION_STRING="..."
```

## 12. Testing

### Unit Tests
```csharp
CreateDocument_ValidRequest_ReturnsUploadUrl()
GetDocuments_EmployeeFilter_ReturnsOnlyEmployeeDocuments()
GenerateDownloadUrl_WrongEmployee_ThrowsUnauthorized()
```

### Integration Tests
```csharp
GET_HRDocuments_WithTenantFilter_ReturnsOnlyTenantDocuments()
```

### E2E (Playwright)
```typescript
test('HR uploads document, employee downloads it')
```

## 13. Monitoring

**Metriche:**
- Upload/download count per tenant
- Tempo medio upload
- Failure rate
- SAS token generated
- Blob storage latency

**Events:**
```csharp
_telemetryClient.TrackEvent("HRDocumentUploaded", { TenantId, DocumentType, FileSizeBytes });
_telemetryClient.TrackEvent("HRDocumentDownloaded", { DocumentId, EmployeeId });
```

## 14. Roadmap

### Fase 1: Foundation
- Setup Azure Blob Storage
- Implementare `AzureBlobStorageService`
- Entità `HRDocument`, `HRDocumentVersion`
- Migration

### Fase 2: Backend API
- `HRDocumentService`
- `HRDocumentsController`, `EmployeeDocumentsController`
- Integration tests

### Fase 3-4: Frontend
- Merchant HR pages
- Employee documents pages

### Fase 5: Security & Compliance
- Audit logging
- GDPR export
- File validation, virus scanning
- Rate limiting

### Fase 6: Advanced
- Email notifications
- Digital signatures
- Template documenti
- Bulk upload
- Analytics dashboard

---

Questo modulo riutilizza l'architettura esistente (multi-tenant Merchant, JWT, EF Core, PostgreSQL) e introduce Azure Blob Storage per archiviazione file con versioning e controllo accessi GDPR-compliant.
