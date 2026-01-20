# ERP HR/Payroll Document Management - Setup Guide

Questa guida descrive i passaggi necessari per completare il setup del modulo ERP HR/Payroll Document Management.

## 📋 Prerequisiti

- .NET 8 SDK
- PostgreSQL Database
- Azure Storage Account (o emulatore Azurite per sviluppo locale)
- Node.js 18+ (per frontend)

---

## 🗄️ Step 1: Database Migration

Il modulo aggiunge due nuove tabelle al database:
- `HRDocuments` - Metadati documenti
- `HRDocumentVersions` - Versioni file

### Creare la Migration

```bash
cd backend/AppointmentScheduler.API

# Crea migration
dotnet ef migrations add AddHRPayrollDocuments --project ../AppointmentScheduler.Data

# Applica migration al database
dotnet ef database update
```

### Verifica Migration

Dopo l'applicazione, dovresti vedere le nuove tabelle nel database PostgreSQL:

```sql
SELECT * FROM "HRDocuments";
SELECT * FROM "HRDocumentVersions";
```

---

## ☁️ Step 2: Azure Blob Storage Setup

### 2.1 Creare Storage Account (Produzione)

```bash
# Login ad Azure
az login

# Crea Storage Account
az storage account create \
  --name appointmentschedsa \
  --resource-group appointment-scheduler-rg \
  --location westeurope \
  --sku Standard_LRS \
  --kind StorageV2

# Crea container privato
az storage container create \
  --name erp-documents \
  --account-name appointmentschedsa \
  --public-access off

# Ottieni connection string
az storage account show-connection-string \
  --name appointmentschedsa \
  --resource-group appointment-scheduler-rg \
  --output tsv
```

### 2.2 Configurare Connection String

**Per sviluppo locale**, modifica `appsettings.Local.json` (NON committare):

```json
{
  "AzureBlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=appointmentschedsa;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net"
  }
}
```

**Per produzione**, configura variabile ambiente in Azure App Service:

```bash
az webapp config appsettings set \
  --name appointment-scheduler-api \
  --resource-group appointment-scheduler-rg \
  --settings AZURE_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;..."
```

### 2.3 Usare Azurite (Emulatore Locale - Opzionale)

Per sviluppo senza Azure:

```bash
# Installa Azurite
npm install -g azurite

# Avvia emulatore
azurite --silent --location ./azurite-data --debug ./azurite-debug.log

# Connection string per Azurite
# "UseDevelopmentStorage=true"
# oppure
# "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
```

Aggiorna `appsettings.Local.json`:

```json
{
  "AzureBlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true"
  }
}
```

---

## 🧪 Step 3: Testare il Backend

### 3.1 Avviare API

```bash
cd backend/AppointmentScheduler.API
dotnet run
```

L'API sarà disponibile su `https://localhost:7001`

### 3.2 Testare con Swagger

Apri browser: `https://localhost:7001/swagger`

**Endpoint disponibili:**

**Per Merchant/HR:**
- `GET /api/hrdocuments` - Lista documenti
- `POST /api/hrdocuments` - Crea documento
- `PUT /api/hrdocuments/{id}/finalize` - Finalizza upload
- `GET /api/hrdocuments/{id}/download` - Download documento

**Per Employee:**
- `GET /api/employee/documents` - Lista documenti personali
- `GET /api/employee/documents/{id}/download` - Download documento

### 3.3 Flow di Test Manuale

1. **Login come Merchant**:
   ```http
   POST /api/auth/login
   {
     "email": "merchant@example.com",
     "password": "password"
   }
   ```
   Copia il token JWT

2. **Crea documento**:
   ```http
   POST /api/hrdocuments
   Authorization: Bearer YOUR_TOKEN
   {
     "employeeId": 1,
     "documentType": "Payslip",
     "title": "Busta Paga Gennaio 2026",
     "year": 2026,
     "month": 1
   }
   ```
   Riceverai un `uploadUrl` SAS token

3. **Upload file al blob**:
   ```bash
   curl -X PUT "UPLOAD_URL_FROM_STEP_2" \
     -H "x-ms-blob-type: BlockBlob" \
     -H "Content-Type: application/pdf" \
     --data-binary @test-payslip.pdf
   ```

4. **Finalizza documento**:
   ```http
   PUT /api/hrdocuments/{documentId}/finalize
   Authorization: Bearer YOUR_TOKEN
   {
     "fileName": "payslip_jan_2026.pdf",
     "fileSizeBytes": 245678,
     "contentType": "application/pdf"
   }
   ```

5. **Download documento**:
   ```http
   GET /api/hrdocuments/{documentId}/download
   Authorization: Bearer YOUR_TOKEN
   ```
   Riceverai un `downloadUrl` SAS token valido 5 minuti

---

## 🎨 Step 4: Frontend Implementation

### 4.1 Merchant App - HR Section

Il frontend Merchant app deve implementare:

**Pagine:**
- `/hr/documents` - Lista documenti con filtri
- `/hr/documents/upload` - Form caricamento nuovo documento
- `/hr/documents/:id` - Dettaglio documento con versioni

**Componenti necessari:**
- `HRDocumentsList.tsx` - Tabella documenti
- `HRDocumentUpload.tsx` - Form upload (multi-step)
- `HRDocumentDetail.tsx` - Dettaglio con versioni
- `HRDocumentFilters.tsx` - Filtri (tipo, anno, mese)

**API Calls esempio:**

```typescript
// Fetch documenti
const fetchDocuments = async (filters: DocumentFilters) => {
  const params = new URLSearchParams({
    ...(filters.employeeId && { employeeId: filters.employeeId.toString() }),
    ...(filters.documentType && { documentType: filters.documentType }),
    ...(filters.year && { year: filters.year.toString() }),
    ...(filters.month && { month: filters.month.toString() })
  });

  const response = await apiClient.get(`/api/hrdocuments?${params}`);
  return response.data;
};

// Upload flow
const uploadDocument = async (data: DocumentCreateDto, file: File) => {
  // Step 1: Create document
  const createResponse = await apiClient.post('/api/hrdocuments', data);
  const { documentId, uploadUrl } = createResponse.data;

  // Step 2: Upload file to blob
  await fetch(uploadUrl, {
    method: 'PUT',
    headers: {
      'x-ms-blob-type': 'BlockBlob',
      'Content-Type': file.type
    },
    body: file
  });

  // Step 3: Finalize
  await apiClient.put(`/api/hrdocuments/${documentId}/finalize`, {
    fileName: file.name,
    fileSizeBytes: file.size,
    contentType: file.type
  });
};
```

### 4.2 Employee App - My Documents Section

Il frontend Employee app deve implementare:

**Pagine:**
- `/documents` - Lista documenti personali
- `/documents/:id` - Visualizzazione documento

**Componenti necessari:**
- `MyDocuments.tsx` - Lista documenti (read-only)
- `DocumentViewer.tsx` - Visualizzazione e download

**API Calls esempio:**

```typescript
// Fetch my documents
const fetchMyDocuments = async () => {
  const response = await apiClient.get('/api/employee/documents');
  return response.data;
};

// Download document
const downloadDocument = async (documentId: number) => {
  const response = await apiClient.get(`/api/employee/documents/${documentId}/download`);
  const { downloadUrl, fileName } = response.data;

  // Trigger download
  const link = document.createElement('a');
  link.href = downloadUrl;
  link.download = fileName;
  link.click();
};
```

---

## 🔒 Step 5: Security Configuration

### 5.1 CORS (se necessario)

Se frontend è hostato su dominio diverso:

```json
// appsettings.Production.json
{
  "CorsOrigins": [
    "https://merchant-app.example.com",
    "https://employee-app.example.com"
  ]
}
```

### 5.2 Blob Storage CORS

Configura CORS su Azure Blob Storage:

```bash
az storage cors add \
  --services b \
  --methods GET PUT POST \
  --origins "https://merchant-app.example.com" "https://employee-app.example.com" \
  --allowed-headers "*" \
  --exposed-headers "*" \
  --max-age 3600 \
  --account-name appointmentschedsa
```

### 5.3 File Upload Limits

In `Program.cs`, configura limiti upload:

```csharp
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760; // 10 MB
});
```

---

## 📊 Step 6: Monitoring

### 6.1 Application Insights (Opzionale)

Aggiungi telemetry per monitorare upload/download:

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

In `Program.cs`:

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### 6.2 Metriche da Monitorare

- Numero documenti caricati per tenant
- Tempo medio upload
- Errori upload
- SAS token generati
- Download per documento
- Storage usage per tenant

---

## 🐛 Troubleshooting

### Errore: "Azure Blob Storage connection string not configured"

**Soluzione:** Verifica che `appsettings.Local.json` o environment variable `AZURE_STORAGE_CONNECTION_STRING` siano configurati.

### Errore: "Blob not found" durante finalizzazione

**Soluzione:**
1. Verifica che l'upload al blob sia completato con successo
2. Controlla che il path blob corrisponda esattamente
3. Verifica che il SAS token non sia scaduto (5 minuti)

### Upload fallisce con 403 Forbidden

**Soluzione:**
1. Verifica che il SAS token abbia permessi Write/Create
2. Controlla che il container esista
3. Verifica CORS se upload da browser

### Employee non vede documenti

**Soluzione:**
1. Verifica che il documento sia in stato "Published"
2. Controlla che `employeeId` corrisponda
3. Verifica che l'employee sia collegato a un User

---

## ✅ Checklist Deployment

Prima di deployare in produzione:

- [ ] Database migration applicata
- [ ] Azure Storage Account creato
- [ ] Container `erp-documents` creato (private)
- [ ] Connection string configurata in Azure App Service
- [ ] CORS configurato su blob storage
- [ ] Testato upload/download flow completo
- [ ] Frontend implementato e testato
- [ ] Monitoring configurato
- [ ] Limiti file validati
- [ ] Backup strategy definita

---

## 📚 Risorse

- [Azure Blob Storage Documentation](https://learn.microsoft.com/en-us/azure/storage/blobs/)
- [SAS Token Best Practices](https://learn.microsoft.com/en-us/azure/storage/common/storage-sas-overview)
- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

---

## 🆘 Support

Per problemi o domande, consultare:
- Documentazione tecnica: `/docs/ERP_HR_PAYROLL_TECHNICAL_SPEC.md`
- GitHub Issues: https://github.com/GiovanniFantini/appointment-scheduler/issues
