# Specifica Tecnica - Modulo ERP HR/Payroll Document Management

## 1. Obiettivo del Modulo

Introdurre nel portale Appointment Scheduler un **modulo ERP HR/Payroll Document Management** che NON è un sistema di calcolo paghe, ma un sistema per:

- **Gestire** documenti HR e payroll per ogni azienda (tenant)
- **Archiviare** in modo sicuro e versionato
- **Distribuire** documenti ai dipendenti con controllo accessi
- **Garantire** conformità GDPR

**Scope**: Caricamento, archiviazione, versioning e distribuzione di documenti generati esternamente (non calcolo stipendi).

---

## 2. Contesto Applicativo

### 2.1 Sistema Multi-Tenant Esistente

Il sistema Appointment Scheduler è già multi-tenant basato su `Merchant`:

```
1 account = 1 azienda = 1 Merchant (tenant)
```

**Entità chiave esistenti:**
- `Merchant` (id = tenantId)
- `Employee` (già relazionato a Merchant)
- `User` (sistema multi-ruolo: Admin, Merchant, Employee, Consumer)

**Autenticazione:**
- JWT Token contiene: `UserId`, `Email`, `Roles`, `MerchantId`
- Policy-based authorization già implementata

**Database:**
- PostgreSQL con Entity Framework Core
- Tutte le query già filtrate per `MerchantId`

**Azure Blob Storage:**
- ⚠️ NON ancora implementato nel progetto
- Da implementare come parte di questo modulo

---

## 3. Funzionalità Principali

### 3.1 Gestione Documenti per Dipendente

Ogni azienda (Merchant) può:

1. **Caricare documenti** per i propri dipendenti
2. **Organizzare documenti** per tipologia:
   - Buste paga (Payslip)
   - Contratti (Contract)
   - Bonus
   - Comunicazioni (Communication)
   - Cambi livello (LevelChange)
   - Altri documenti HR

3. **Consultare documenti** filtrati per:
   - Tipo documento
   - Anno
   - Mese
   - Dipendente

4. **Versionare documenti**:
   - Ogni modifica crea una nuova versione
   - Storico completo consultabile
   - Versione attiva marcata

### 3.2 Controllo Accessi per Ruolo

**Ruoli e Permessi:**

| Ruolo | Permessi |
|-------|----------|
| **Admin** (Piattaforma) | Accesso completo a tutti i tenant (solo emergenze) |
| **Merchant/HR** | CRUD documenti per tutti i dipendenti del proprio tenant |
| **Manager** | Lettura documenti team assegnato (future) |
| **Employee** | Solo lettura dei **propri** documenti |

**Regole di sicurezza:**
- Ogni richiesta verifica `tenantId` dal JWT token
- Employee vede solo documenti con `employeeId` = proprio id
- Merchant/HR può gestire solo documenti del proprio tenant
- Nessun accesso cross-tenant (isolamento totale)

---

## 4. Architettura Logica

### 4.1 Integrazione con Architettura Esistente

Il modulo ERP si integra come **modular monolith separato**:

```
AppointmentScheduler/
├── backend/
│   ├── AppointmentScheduler.API/
│   │   └── Controllers/
│   │       ├── HRDocumentsController.cs       # NUOVO
│   │       └── EmployeeDocumentsController.cs  # NUOVO
│   ├── AppointmentScheduler.Core/
│   │   ├── Services/
│   │   │   ├── IHRDocumentService.cs          # NUOVO
│   │   │   ├── HRDocumentService.cs           # NUOVO
│   │   │   ├── IFileStorageService.cs         # NUOVO
│   │   │   └── AzureBlobStorageService.cs     # NUOVO
│   │   └── Interfaces/
│   ├── AppointmentScheduler.Data/
│   │   └── ApplicationDbContext.cs            # + DbSet HR entities
│   └── AppointmentScheduler.Shared/
│       ├── Models/
│       │   ├── HRDocument.cs                  # NUOVO
│       │   └── HRDocumentVersion.cs           # NUOVO
│       └── DTOs/
│           ├── HRDocumentDto.cs               # NUOVO
│           └── HRDocumentUploadDto.cs         # NUOVO
└── frontend/
    ├── merchant-app/                          # Aggiungere sezione HR
    │   └── src/pages/hr/                      # NUOVO
    └── employee-app/                          # Aggiungere "I miei documenti"
        └── src/pages/documents/               # NUOVO
```

**Componenti riutilizzati:**
- ✅ API .NET esistente (aggiunta controller)
- ✅ PostgreSQL come database principale
- ✅ Autenticazione JWT esistente
- ✅ Entity `Employee` esistente
- ✅ Frontend React (nuove sezioni)

**Componenti nuovi:**
- 🆕 Azure Blob Storage Service
- 🆕 HR Document entities e DTOs
- 🆕 HR Document Service
- 🆕 Controller per HR e Employee documents
- 🆕 Pagine frontend HR

---

## 5. Modello Dati

### 5.1 Entità Database

#### 5.1.1 HRDocument (Metadati Documento)

```csharp
public class HRDocument
{
    // Primary Key
    public int Id { get; set; }

    // Multi-tenant
    public int TenantId { get; set; }  // FK a Merchant.Id
    public Merchant Tenant { get; set; }

    // Dipendente
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; }

    // Classificazione
    public HRDocumentType DocumentType { get; set; }
    public string Title { get; set; }  // max 200 char
    public string? Description { get; set; }  // max 1000 char

    // Periodo di riferimento
    public int? Year { get; set; }   // es. 2026
    public int? Month { get; set; }  // 1-12, nullable

    // Versioning
    public int CurrentVersion { get; set; }  // versione attiva
    public List<HRDocumentVersion> Versions { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public User CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
    public User? UpdatedBy { get; set; }

    // Stato
    public HRDocumentStatus Status { get; set; }  // Draft, Published, Archived
    public bool IsDeleted { get; set; }  // Soft delete
}
```

#### 5.1.2 HRDocumentVersion (Versioni File)

```csharp
public class HRDocumentVersion
{
    // Primary Key
    public int Id { get; set; }

    // Documento parent
    public int HRDocumentId { get; set; }
    public HRDocument HRDocument { get; set; }

    // Versione
    public int VersionNumber { get; set; }  // 1, 2, 3...

    // File nel Blob Storage
    public string BlobPath { get; set; }  // tenant-X/employee-Y/...
    public string FileName { get; set; }   // nome originale file
    public string ContentType { get; set; }  // application/pdf
    public long FileSizeBytes { get; set; }
    public string FileHash { get; set; }  // SHA256 per integrity check

    // Metadati versione
    public string? ChangeNotes { get; set; }  // motivo modifica

    // Audit
    public DateTime UploadedAt { get; set; }
    public int UploadedByUserId { get; set; }
    public User UploadedBy { get; set; }

    // Stato upload
    public UploadStatus UploadStatus { get; set; }  // Uploading, Completed, Failed
}
```

#### 5.1.3 Enumerazioni

```csharp
public enum HRDocumentType
{
    Payslip = 1,        // Busta paga
    Contract = 2,       // Contratto
    Bonus = 3,          // Bonus
    Communication = 4,  // Comunicazione
    LevelChange = 5,    // Cambio livello
    Other = 99          // Altro
}

public enum HRDocumentStatus
{
    Draft = 1,      // Bozza (non visibile a dipendente)
    Published = 2,  // Pubblicato (visibile)
    Archived = 3    // Archiviato (read-only)
}

public enum UploadStatus
{
    Uploading = 1,  // Upload in corso
    Completed = 2,  // Upload completato
    Failed = 3      // Upload fallito
}
```

### 5.2 Indici Database

```csharp
// In ApplicationDbContext.OnModelCreating()

modelBuilder.Entity<HRDocument>(entity =>
{
    // Indice per tenant isolation
    entity.HasIndex(e => e.TenantId);

    // Indice per query dipendente
    entity.HasIndex(e => new { e.TenantId, e.EmployeeId });

    // Indice per filtri tipo/anno/mese
    entity.HasIndex(e => new { e.TenantId, e.DocumentType, e.Year, e.Month });

    // Soft delete filter
    entity.HasQueryFilter(e => !e.IsDeleted);

    // Foreign keys
    entity.HasOne(e => e.Tenant)
        .WithMany()
        .HasForeignKey(e => e.TenantId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(e => e.Employee)
        .WithMany()
        .HasForeignKey(e => e.EmployeeId)
        .OnDelete(DeleteBehavior.Restrict);
});

modelBuilder.Entity<HRDocumentVersion>(entity =>
{
    // Indice unique per versione
    entity.HasIndex(e => new { e.HRDocumentId, e.VersionNumber })
        .IsUnique();

    // Foreign key
    entity.HasOne(e => e.HRDocument)
        .WithMany(d => d.Versions)
        .HasForeignKey(e => e.HRDocumentId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

---

## 6. Azure Blob Storage

### 6.1 Struttura Container

**Container unico condiviso:**
```
Container: erp-documents (Private Access)
```

**Struttura path:**
```
tenant-{tenantId}/
└── employee-{employeeId}/
    └── {documentType}/
        └── {year}/
            └── {month}/
                └── {documentId}_v{version}.{ext}
```

**Esempio concreto:**
```
erp-documents/
├── tenant-5/
│   ├── employee-12/
│   │   ├── payslip/
│   │   │   └── 2026/
│   │   │       ├── 01/
│   │   │       │   ├── doc-1001_v1.pdf
│   │   │       │   └── doc-1001_v2.pdf
│   │   │       └── 02/
│   │   │           └── doc-1002_v1.pdf
│   │   └── contract/
│   │       └── 2026/
│   │           └── doc-1003_v1.pdf
│   └── employee-13/
│       └── payslip/
│           └── 2026/
│               └── 01/
│                   └── doc-1004_v1.pdf
└── tenant-6/
    └── employee-20/
        └── ...
```

### 6.2 Configurazione Azure

**appsettings.json:**
```json
{
  "AzureBlobStorage": {
    "ConnectionString": "",  // Da environment variable
    "ContainerName": "erp-documents",
    "SasTokenExpirationMinutes": 5
  }
}
```

**Environment Variables (Production):**
```bash
AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;AccountName=...
```

**NuGet Package da aggiungere:**
```xml
<PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />
```

### 6.3 Sicurezza Blob Storage

**Principi:**
1. Container è **Private** (no anonymous access)
2. Frontend **MAI** accesso diretto al blob
3. Backend genera **SAS token temporanei** per ogni operazione
4. SAS token scade dopo 5 minuti
5. Ogni SAS è specifico per un file e un'operazione (read/write)

**Flusso sicurezza:**
```
Frontend → API (verifica tenant/ruolo) → Genera SAS → Restituisce URL firmato
           ↓
      Frontend usa URL firmato (valido 5 min) → Scarica da Blob Storage
```

---

## 7. API Endpoints

### 7.1 HRDocumentsController (per Merchant/HR)

**Base route:** `/api/hrdocuments`

**Policy:** `[Authorize(Policy = "MerchantOnly")]`

#### GET /api/hrdocuments
Lista documenti HR per il tenant corrente.

**Query params:**
- `employeeId` (optional): filtra per dipendente
- `documentType` (optional): filtra per tipo
- `year` (optional): filtra per anno
- `month` (optional): filtra per mese
- `status` (optional): Draft/Published/Archived

**Response 200:**
```json
[
  {
    "id": 1001,
    "tenantId": 5,
    "employeeId": 12,
    "employeeName": "Mario Rossi",
    "documentType": "Payslip",
    "title": "Busta Paga Gennaio 2026",
    "description": null,
    "year": 2026,
    "month": 1,
    "currentVersion": 2,
    "status": "Published",
    "createdAt": "2026-01-15T10:30:00Z",
    "updatedAt": "2026-01-16T14:20:00Z"
  }
]
```

#### GET /api/hrdocuments/{id}
Dettaglio documento con tutte le versioni.

**Response 200:**
```json
{
  "id": 1001,
  "tenantId": 5,
  "employeeId": 12,
  "documentType": "Payslip",
  "title": "Busta Paga Gennaio 2026",
  "currentVersion": 2,
  "status": "Published",
  "versions": [
    {
      "id": 5001,
      "versionNumber": 1,
      "fileName": "payslip_jan_v1.pdf",
      "fileSizeBytes": 245678,
      "uploadedAt": "2026-01-15T10:30:00Z",
      "uploadedBy": "admin@company.com"
    },
    {
      "id": 5002,
      "versionNumber": 2,
      "fileName": "payslip_jan_v2.pdf",
      "fileSizeBytes": 246012,
      "uploadedAt": "2026-01-16T14:20:00Z",
      "uploadedBy": "hr@company.com",
      "changeNotes": "Correzione importo bonus"
    }
  ]
}
```

#### POST /api/hrdocuments
Crea nuovo documento (step 1: metadati).

**Request Body:**
```json
{
  "employeeId": 12,
  "documentType": "Payslip",
  "title": "Busta Paga Gennaio 2026",
  "description": null,
  "year": 2026,
  "month": 1
}
```

**Response 201:**
```json
{
  "documentId": 1001,
  "uploadUrl": "https://storage.blob.core.windows.net/erp-documents/tenant-5/employee-12/payslip/2026/01/doc-1001_v1.pdf?sv=2021-06-08&se=2026-01-20T15%3A25%3A00Z&sr=b&sp=w&sig=ABC123...",
  "blobPath": "tenant-5/employee-12/payslip/2026/01/doc-1001_v1.pdf",
  "expiresAt": "2026-01-20T15:25:00Z"
}
```

**Note:**
- Backend crea record DB in stato `UploadStatus.Uploading`
- Genera SAS token write-only per upload
- Frontend carica file usando `uploadUrl`

#### PUT /api/hrdocuments/{id}/finalize
Finalizza upload documento (step 2).

**Request Body:**
```json
{
  "fileName": "payslip_jan.pdf",
  "fileSizeBytes": 245678,
  "contentType": "application/pdf",
  "fileHash": "sha256:abc123..."
}
```

**Response 200:**
```json
{
  "success": true,
  "documentId": 1001,
  "status": "Published"
}
```

**Note:**
- Backend verifica esistenza file nel blob
- Aggiorna record da `Uploading` a `Completed`
- Pubblica documento (visibile a dipendente)

#### POST /api/hrdocuments/{id}/versions
Aggiunge nuova versione (upload sostitutivo).

**Request Body:**
```json
{
  "changeNotes": "Correzione importo bonus"
}
```

**Response 201:**
```json
{
  "versionId": 5002,
  "versionNumber": 2,
  "uploadUrl": "https://...",
  "expiresAt": "2026-01-20T15:30:00Z"
}
```

#### PUT /api/hrdocuments/{id}
Aggiorna metadati documento.

**Request Body:**
```json
{
  "title": "Busta Paga Gennaio 2026 - Rettificata",
  "status": "Published"
}
```

#### DELETE /api/hrdocuments/{id}
Soft delete documento (marca `IsDeleted = true`).

**Response 204 No Content**

---

### 7.2 EmployeeDocumentsController (per Employee)

**Base route:** `/api/employee/documents`

**Policy:** `[Authorize(Policy = "EmployeeOnly")]`

#### GET /api/employee/documents
Lista documenti del dipendente autenticato (solo Published).

**Query params:**
- `documentType` (optional)
- `year` (optional)
- `month` (optional)

**Response 200:**
```json
[
  {
    "id": 1001,
    "documentType": "Payslip",
    "title": "Busta Paga Gennaio 2026",
    "year": 2026,
    "month": 1,
    "currentVersion": 2,
    "publishedAt": "2026-01-16T14:20:00Z"
  }
]
```

**Note:**
- Filtra automaticamente per `employeeId` dal JWT token
- Solo documenti con `Status = Published`
- No accesso a Draft o Archived

#### GET /api/employee/documents/{id}/download
Download file documento (versione corrente).

**Response 200:**
```json
{
  "downloadUrl": "https://storage.blob.core.windows.net/erp-documents/tenant-5/employee-12/payslip/2026/01/doc-1001_v2.pdf?sv=2021-06-08&se=2026-01-20T15%3A35%3A00Z&sr=b&sp=r&sig=XYZ789...",
  "fileName": "payslip_jan_v2.pdf",
  "expiresAt": "2026-01-20T15:35:00Z"
}
```

**Note:**
- Verifica che `employeeId` nel documento = `employeeId` nel JWT
- Genera SAS token read-only valido 5 minuti
- Frontend scarica file usando `downloadUrl`

#### GET /api/employee/documents/{id}/versions/{versionNumber}/download
Download versione specifica.

---

## 8. Servizi Backend

### 8.1 IFileStorageService

```csharp
public interface IFileStorageService
{
    /// <summary>
    /// Genera SAS URL per upload file
    /// </summary>
    Task<string> GenerateUploadSasUrlAsync(
        string blobPath,
        int expirationMinutes = 5);

    /// <summary>
    /// Genera SAS URL per download file
    /// </summary>
    Task<string> GenerateDownloadSasUrlAsync(
        string blobPath,
        int expirationMinutes = 5);

    /// <summary>
    /// Verifica esistenza file
    /// </summary>
    Task<bool> BlobExistsAsync(string blobPath);

    /// <summary>
    /// Ottiene proprietà file (size, contentType, eTag)
    /// </summary>
    Task<BlobProperties> GetBlobPropertiesAsync(string blobPath);

    /// <summary>
    /// Elimina file (hard delete)
    /// </summary>
    Task DeleteBlobAsync(string blobPath);

    /// <summary>
    /// Costruisce blob path secondo convenzioni
    /// </summary>
    string BuildBlobPath(
        int tenantId,
        int employeeId,
        HRDocumentType documentType,
        int? year,
        int? month,
        int documentId,
        int versionNumber,
        string fileExtension);
}
```

### 8.2 AzureBlobStorageService (Implementazione)

```csharp
public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly int _sasExpirationMinutes;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureBlobStorage:ConnectionString"];
        _blobServiceClient = new BlobServiceClient(connectionString);
        _containerName = configuration["AzureBlobStorage:ContainerName"];
        _sasExpirationMinutes = int.Parse(
            configuration["AzureBlobStorage:SasTokenExpirationMinutes"] ?? "5");
    }

    public async Task<string> GenerateUploadSasUrlAsync(
        string blobPath,
        int expirationMinutes = 5)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobPath,
            Resource = "b", // blob
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // clock skew
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var sasToken = blobClient.GenerateSasUri(sasBuilder);
        return sasToken.ToString();
    }

    public async Task<string> GenerateDownloadSasUrlAsync(
        string blobPath,
        int expirationMinutes = 5)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobPath,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasToken = blobClient.GenerateSasUri(sasBuilder);
        return sasToken.ToString();
    }

    public string BuildBlobPath(
        int tenantId,
        int employeeId,
        HRDocumentType documentType,
        int? year,
        int? month,
        int documentId,
        int versionNumber,
        string fileExtension)
    {
        var parts = new List<string>
        {
            $"tenant-{tenantId}",
            $"employee-{employeeId}",
            documentType.ToString().ToLowerInvariant()
        };

        if (year.HasValue)
            parts.Add(year.Value.ToString());

        if (month.HasValue)
            parts.Add(month.Value.ToString("D2"));

        var fileName = $"doc-{documentId}_v{versionNumber}.{fileExtension}";
        parts.Add(fileName);

        return string.Join("/", parts);
    }

    // ... altre implementazioni
}
```

### 8.3 IHRDocumentService

```csharp
public interface IHRDocumentService
{
    /// <summary>
    /// Lista documenti per tenant (filtri opzionali)
    /// </summary>
    Task<List<HRDocumentDto>> GetDocumentsAsync(
        int tenantId,
        int? employeeId = null,
        HRDocumentType? documentType = null,
        int? year = null,
        int? month = null,
        HRDocumentStatus? status = null);

    /// <summary>
    /// Dettaglio documento con versioni
    /// </summary>
    Task<HRDocumentDetailDto> GetDocumentByIdAsync(
        int documentId,
        int tenantId);

    /// <summary>
    /// Crea documento e prepara upload
    /// </summary>
    Task<HRDocumentUploadResponseDto> CreateDocumentAsync(
        int tenantId,
        int userId,
        HRDocumentCreateDto dto);

    /// <summary>
    /// Finalizza upload documento
    /// </summary>
    Task<bool> FinalizeDocumentUploadAsync(
        int documentId,
        int tenantId,
        HRDocumentFinalizeDto dto);

    /// <summary>
    /// Aggiunge nuova versione
    /// </summary>
    Task<HRDocumentVersionUploadResponseDto> AddDocumentVersionAsync(
        int documentId,
        int tenantId,
        int userId,
        string changeNotes);

    /// <summary>
    /// Aggiorna metadati documento
    /// </summary>
    Task<bool> UpdateDocumentAsync(
        int documentId,
        int tenantId,
        HRDocumentUpdateDto dto);

    /// <summary>
    /// Soft delete documento
    /// </summary>
    Task<bool> DeleteDocumentAsync(int documentId, int tenantId);

    /// <summary>
    /// Genera URL download per dipendente
    /// </summary>
    Task<HRDocumentDownloadDto> GenerateEmployeeDownloadUrlAsync(
        int documentId,
        int employeeId,
        int? versionNumber = null);
}
```

---

## 9. Frontend Integration

### 9.1 Merchant App - Sezione HR

**Nuove rotte in `/frontend/merchant-app/src/App.tsx`:**

```typescript
<Route path="/hr" element={<HRLayout />}>
  <Route path="documents" element={<HRDocumentsList />} />
  <Route path="documents/upload" element={<HRDocumentUpload />} />
  <Route path="documents/:id" element={<HRDocumentDetail />} />
</Route>
```

**Componenti:**

1. **HRDocumentsList.tsx**: Tabella documenti con filtri
2. **HRDocumentUpload.tsx**: Form caricamento nuovo documento
3. **HRDocumentDetail.tsx**: Dettaglio documento con versioni

### 9.2 Employee App - I Miei Documenti

**Nuove rotte in `/frontend/employee-app/src/App.tsx`:**

```typescript
<Route path="/documents" element={<MyDocuments />} />
<Route path="/documents/:id" element={<DocumentViewer />} />
```

**Componenti:**

1. **MyDocuments.tsx**: Lista documenti personali
2. **DocumentViewer.tsx**: Visualizzazione e download documento

### 9.3 Upload Flow (Frontend)

```typescript
// Step 1: Crea documento e ottieni upload URL
const createResponse = await apiClient.post('/api/hrdocuments', {
  employeeId: selectedEmployee,
  documentType: 'Payslip',
  title: 'Busta Paga Gennaio 2026',
  year: 2026,
  month: 1
});

const { documentId, uploadUrl, blobPath } = createResponse.data;

// Step 2: Upload file direttamente al Blob Storage
const file = fileInputRef.current.files[0];
await fetch(uploadUrl, {
  method: 'PUT',
  headers: {
    'x-ms-blob-type': 'BlockBlob',
    'Content-Type': file.type
  },
  body: file
});

// Step 3: Finalizza documento
await apiClient.put(`/api/hrdocuments/${documentId}/finalize`, {
  fileName: file.name,
  fileSizeBytes: file.size,
  contentType: file.type,
  fileHash: await calculateSHA256(file)
});
```

### 9.4 Download Flow (Frontend)

```typescript
// Richiedi URL download
const response = await apiClient.get(
  `/api/employee/documents/${documentId}/download`
);

const { downloadUrl, fileName } = response.data;

// Download file tramite SAS URL
const link = document.createElement('a');
link.href = downloadUrl;
link.download = fileName;
link.click();
```

---

## 10. Sicurezza e Compliance

### 10.1 Isolamento Multi-Tenant

**Database-Level:**
- Ogni query filtra per `tenantId`
- Foreign keys prevent cross-tenant access
- Query filter globale: `.HasQueryFilter(e => e.TenantId == currentTenantId)`

**Application-Level:**
- Ogni API endpoint verifica `tenantId` da JWT
- Employee può accedere solo se `employeeId` = proprio id
- Merchant può accedere solo a documenti del proprio tenant

**Storage-Level:**
- Path blob include `tenant-{id}`
- SAS token specifico per file singolo
- No list permission su container

### 10.2 GDPR Compliance

**Right to Access:**
- Employee può scaricare tutti i propri documenti
- API endpoint `/api/employee/documents/export` (future)

**Right to Erasure:**
- Soft delete documenti (`IsDeleted = true`)
- Hard delete blob storage su richiesta esplicita
- Audit log delle cancellazioni

**Data Minimization:**
- Solo metadati necessari nel DB
- File binari solo nel blob storage
- Retention policy configurabile per tipo documento

**Audit Trail:**
- Ogni operazione tracciata (`CreatedBy`, `UpdatedBy`, timestamps)
- Log accessi documento (future: `HRDocumentAccessLog`)

### 10.3 Validazioni Sicurezza

**File Upload:**
- Max size: 10 MB (configurabile)
- Allowed extensions: `.pdf`, `.docx`, `.jpg`, `.png`
- Content-Type validation
- Virus scanning (future: Azure Defender for Storage)
- File hash verification

**SAS Token:**
- Expiration: 5 minuti
- Permissions minime (read-only o write-only)
- No list/delete permissions
- Signed URL unico per operazione

**API Rate Limiting:**
- Max 100 requests/minute per utente (future)
- Throttling su upload endpoint

---

## 11. Migration Plan

### 11.1 Database Migration

```bash
# Crea migration
dotnet ef migrations add AddHRPayrollDocuments --project AppointmentScheduler.Data

# Applica migration
dotnet ef database update --project AppointmentScheduler.API
```

**Migration file:**
```csharp
public partial class AddHRPayrollDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HRDocuments",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TenantId = table.Column<int>(nullable: false),
                EmployeeId = table.Column<int>(nullable: false),
                // ... altre colonne
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HRDocuments", x => x.Id);
                table.ForeignKey(
                    name: "FK_HRDocuments_Merchants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Merchants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Indici
        migrationBuilder.CreateIndex(
            name: "IX_HRDocuments_TenantId",
            table: "HRDocuments",
            column: "TenantId");
    }
}
```

### 11.2 Azure Resources Setup

**Azure Blob Storage Account:**
```bash
# Crea Storage Account
az storage account create \
  --name appointmentschedsa \
  --resource-group appointment-scheduler-rg \
  --location westeurope \
  --sku Standard_LRS \
  --kind StorageV2

# Crea container
az storage container create \
  --name erp-documents \
  --account-name appointmentschedsa \
  --public-access off

# Ottieni connection string
az storage account show-connection-string \
  --name appointmentschedsa \
  --resource-group appointment-scheduler-rg
```

**App Service Configuration:**
```bash
az webapp config appsettings set \
  --name appointment-scheduler-api \
  --resource-group appointment-scheduler-rg \
  --settings AZURE_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;..."
```

---

## 12. Testing Strategy

### 12.1 Unit Tests

**HRDocumentServiceTests.cs:**
```csharp
[Fact]
public async Task CreateDocument_ValidRequest_ReturnsUploadUrl()
{
    // Arrange
    var mockContext = CreateMockDbContext();
    var mockStorage = new Mock<IFileStorageService>();
    var service = new HRDocumentService(mockContext, mockStorage.Object);

    // Act
    var result = await service.CreateDocumentAsync(
        tenantId: 5,
        userId: 10,
        dto: new HRDocumentCreateDto { ... });

    // Assert
    Assert.NotNull(result.UploadUrl);
    Assert.Contains("tenant-5", result.BlobPath);
}

[Fact]
public async Task GetDocuments_EmployeeFilter_ReturnsOnlyEmployeeDocuments()
{
    // Test isolamento per employeeId
}

[Fact]
public async Task GenerateDownloadUrl_WrongEmployee_ThrowsUnauthorized()
{
    // Test sicurezza accesso cross-employee
}
```

### 12.2 Integration Tests

**HRDocumentsControllerTests.cs:**
```csharp
[Fact]
public async Task GET_HRDocuments_WithTenantFilter_ReturnsOnlyTenantDocuments()
{
    // Arrange
    var client = _factory.CreateClient();
    client.SetAuthToken(GenerateJwtToken(tenantId: 5, role: "Merchant"));

    // Act
    var response = await client.GetAsync("/api/hrdocuments");

    // Assert
    response.EnsureSuccessStatusCode();
    var documents = await response.Content.ReadAsAsync<List<HRDocumentDto>>();
    Assert.All(documents, d => Assert.Equal(5, d.TenantId));
}
```

### 12.3 E2E Tests (Playwright)

```typescript
test('HR can upload document and employee can download it', async ({ page }) => {
  // Login as Merchant/HR
  await page.goto('/login');
  await page.fill('[name=email]', 'hr@company.com');
  await page.fill('[name=password]', 'password');
  await page.click('button[type=submit]');

  // Navigate to HR documents
  await page.click('text=HR Documents');

  // Upload document
  await page.click('text=Upload Document');
  await page.selectOption('[name=employeeId]', '12');
  await page.selectOption('[name=documentType]', 'Payslip');
  await page.setInputFiles('[name=file]', 'test-payslip.pdf');
  await page.click('button:has-text("Upload")');

  // Verify success
  await expect(page.locator('text=Document uploaded successfully')).toBeVisible();

  // Login as Employee
  await page.goto('/employee/login');
  await page.fill('[name=email]', 'employee@company.com');
  await page.fill('[name=password]', 'password');
  await page.click('button[type=submit]');

  // Check document is visible
  await page.goto('/documents');
  await expect(page.locator('text=Busta Paga')).toBeVisible();

  // Download document
  const [download] = await Promise.all([
    page.waitForEvent('download'),
    page.click('text=Download')
  ]);

  expect(download.suggestedFilename()).toContain('.pdf');
});
```

---

## 13. Monitoring e Logging

### 13.1 Application Insights

**Metriche da tracciare:**
- Numero upload documenti per tenant
- Tempo medio upload
- Numero download per documento
- Errori upload (failure rate)
- SAS token generated count
- Blob storage operations latency

**Custom Events:**
```csharp
_telemetryClient.TrackEvent("HRDocumentUploaded", new Dictionary<string, string>
{
    { "TenantId", tenantId.ToString() },
    { "DocumentType", documentType.ToString() },
    { "FileSizeBytes", fileSize.ToString() }
});

_telemetryClient.TrackEvent("HRDocumentDownloaded", new Dictionary<string, string>
{
    { "DocumentId", documentId.ToString() },
    { "EmployeeId", employeeId.ToString() }
});
```

### 13.2 Structured Logging

```csharp
_logger.LogInformation(
    "Document {DocumentId} uploaded by user {UserId} for tenant {TenantId}",
    documentId, userId, tenantId);

_logger.LogWarning(
    "Unauthorized access attempt to document {DocumentId} by employee {EmployeeId}",
    documentId, employeeId);

_logger.LogError(exception,
    "Failed to upload document {DocumentId} to blob storage",
    documentId);
```

---

## 14. Roadmap e Fasi Implementazione

### Fase 1: Foundation (Sprint 1-2)
- [ ] Setup Azure Blob Storage
- [ ] Implementare `AzureBlobStorageService`
- [ ] Creare entità `HRDocument` e `HRDocumentVersion`
- [ ] Database migration
- [ ] Unit tests per `FileStorageService`

### Fase 2: Backend API (Sprint 3-4)
- [ ] Implementare `HRDocumentService`
- [ ] Creare `HRDocumentsController` (CRUD)
- [ ] Creare `EmployeeDocumentsController` (read-only)
- [ ] Integration tests per API
- [ ] Swagger documentation

### Fase 3: Frontend Merchant (Sprint 5)
- [ ] Lista documenti HR
- [ ] Form upload documento
- [ ] Dettaglio documento con versioni
- [ ] Filtri e ricerca

### Fase 4: Frontend Employee (Sprint 6)
- [ ] Lista documenti personali
- [ ] Download documento
- [ ] UI responsive

### Fase 5: Security & Compliance (Sprint 7)
- [ ] Audit logging completo
- [ ] GDPR export funzionalità
- [ ] File validation e virus scanning
- [ ] Rate limiting

### Fase 6: Advanced Features (Sprint 8+)
- [ ] Notifiche email nuovo documento
- [ ] Firma digitale documenti
- [ ] Template documenti
- [ ] Bulk upload
- [ ] Analytics dashboard

---

## 15. Risorse e Documentazione

### 15.1 Azure Documentation
- [Azure Blob Storage Quickstart](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet)
- [Shared Access Signatures (SAS)](https://learn.microsoft.com/en-us/azure/storage/common/storage-sas-overview)

### 15.2 Entity Framework Core
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Query Filters](https://learn.microsoft.com/en-us/ef/core/querying/filters)

### 15.3 GDPR
- [GDPR Developer Guide](https://ec.europa.eu/info/law/law-topic/data-protection_en)

---

## Conclusioni

Questo modulo ERP HR/Payroll Document Management si integra perfettamente nell'architettura esistente di Appointment Scheduler, riutilizzando:

✅ Sistema multi-tenant basato su Merchant
✅ Autenticazione JWT e RBAC
✅ Entity Framework Core e PostgreSQL
✅ Pattern architetturali consolidati

Introduce:

🆕 Azure Blob Storage per archiviazione file
🆕 Sistema versioning documenti
🆕 Controllo accessi granulare per ruolo
🆕 Compliance GDPR ready

L'implementazione segue un approccio incrementale, permettendo rilasci graduali e testing continuo.
