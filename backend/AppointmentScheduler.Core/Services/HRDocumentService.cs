using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

public class HRDocumentService : IHRDocumentService
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly INotificationService _notificationService;
    private readonly IUtcClock _clock;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "pdf", "doc", "docx", "xls", "xlsx", "png", "jpg", "jpeg", "txt"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "image/png",
        "image/jpeg",
        "text/plain"
    };

    private const long MaxUploadSizeBytes = 50L * 1024 * 1024;

    public HRDocumentService(
        IApplicationDbContext context,
        IFileStorageService fileStorage,
        INotificationService notificationService,
        IUtcClock clock)
    {
        _context = context;
        _fileStorage = fileStorage;
        _notificationService = notificationService;
        _clock = clock;
    }

    public async Task<List<HRDocumentDto>> GetDocumentsAsync(
        int tenantId,
        int? employeeId = null,
        HRDocumentType? documentType = null,
        int? year = null,
        int? month = null,
        HRDocumentStatus? status = null)
    {
        var query = _context.HRDocuments
            .Where(d => d.TenantId == tenantId && !d.IsDeleted)
            .Include(d => d.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(d => d.EmployeeId == employeeId.Value);

        if (documentType.HasValue)
            query = query.Where(d => d.DocumentType == documentType.Value);

        if (year.HasValue)
            query = query.Where(d => d.Year == year.Value);

        if (month.HasValue)
            query = query.Where(d => d.Month == month.Value);

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        var documents = await query
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return documents.Select(d => new HRDocumentDto
        {
            Id = d.Id,
            TenantId = d.TenantId,
            EmployeeId = d.EmployeeId,
            EmployeeName = $"{d.Employee.FirstName} {d.Employee.LastName}",
            DocumentType = d.DocumentType,
            DocumentTypeText = d.DocumentType.ToString(),
            Title = d.Title,
            Description = d.Description,
            Year = d.Year,
            Month = d.Month,
            CurrentVersion = d.CurrentVersion,
            Status = d.Status,
            StatusText = d.Status.ToString(),
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        }).ToList();
    }

    public async Task<List<HRDocumentDto>> GetEmployeeDocumentsAsync(
        int tenantId,
        int employeeId,
        HRDocumentType? documentType = null,
        int? year = null,
        int? month = null)
    {
        var query = _context.HRDocuments
            .Where(d =>
                d.TenantId == tenantId &&
                d.EmployeeId == employeeId &&
                !d.IsDeleted &&
                d.Status == HRDocumentStatus.Published)
            .Include(d => d.Employee)
            .AsQueryable();

        if (documentType.HasValue)
            query = query.Where(d => d.DocumentType == documentType.Value);

        if (year.HasValue)
            query = query.Where(d => d.Year == year.Value);

        if (month.HasValue)
            query = query.Where(d => d.Month == month.Value);

        var documents = await query
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return documents.Select(d => new HRDocumentDto
        {
            Id = d.Id,
            TenantId = d.TenantId,
            EmployeeId = d.EmployeeId,
            EmployeeName = $"{d.Employee.FirstName} {d.Employee.LastName}",
            DocumentType = d.DocumentType,
            DocumentTypeText = d.DocumentType.ToString(),
            Title = d.Title,
            Description = d.Description,
            Year = d.Year,
            Month = d.Month,
            CurrentVersion = d.CurrentVersion,
            Status = d.Status,
            StatusText = d.Status.ToString(),
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        }).ToList();
    }

    public async Task<HRDocumentDetailDto?> GetDocumentByIdAsync(int documentId, int tenantId)
    {
        var document = await _context.HRDocuments
            .Where(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted)
            .Include(d => d.Employee)
            .Include(d => d.CreatedBy)
            .Include(d => d.UpdatedBy)
            .Include(d => d.Versions)
                .ThenInclude(v => v.UploadedBy)
            .FirstOrDefaultAsync();

        if (document == null)
            return null;

        return new HRDocumentDetailDto
        {
            Id = document.Id,
            TenantId = document.TenantId,
            EmployeeId = document.EmployeeId,
            EmployeeName = $"{document.Employee.FirstName} {document.Employee.LastName}",
            DocumentType = document.DocumentType,
            DocumentTypeText = document.DocumentType.ToString(),
            Title = document.Title,
            Description = document.Description,
            Year = document.Year,
            Month = document.Month,
            CurrentVersion = document.CurrentVersion,
            Status = document.Status,
            StatusText = document.Status.ToString(),
            CreatedAt = document.CreatedAt,
            CreatedByEmail = document.CreatedBy.Email,
            UpdatedAt = document.UpdatedAt,
            UpdatedByEmail = document.UpdatedBy?.Email,
            // I campi di tracciamento per-dipendente (DownloadCount, AcknowledgedAt)
            // restano vuoti nella vista merchant: qui non c'è un "dipendente
            // corrente". Per il quadro completo il merchant usa GetDocumentAccessLogAsync.
            Versions = document.Versions
                .OrderBy(v => v.VersionNumber)
                .Select(v => new HRDocumentVersionDto
                {
                    Id = v.Id,
                    VersionNumber = v.VersionNumber,
                    FileName = v.FileName,
                    ContentType = v.ContentType,
                    FileSizeBytes = v.FileSizeBytes,
                    ChangeNotes = v.ChangeNotes,
                    UploadStatus = v.UploadStatus,
                    UploadedAt = v.UploadedAt,
                    UploadedByEmail = v.UploadedBy.Email
                }).ToList()
        };
    }

    public async Task<HRDocumentDetailDto?> GetEmployeeDocumentByIdAsync(int documentId, int tenantId, int employeeId)
    {
        var document = await _context.HRDocuments
            .Where(d =>
                d.Id == documentId &&
                d.TenantId == tenantId &&
                d.EmployeeId == employeeId &&
                !d.IsDeleted &&
                d.Status == HRDocumentStatus.Published)
            .Include(d => d.Employee)
            .Include(d => d.CreatedBy)
            .Include(d => d.UpdatedBy)
            .Include(d => d.Versions)
                .ThenInclude(v => v.UploadedBy)
            .Include(d => d.Versions)
                .ThenInclude(v => v.Downloads)
            .Include(d => d.Versions)
                .ThenInclude(v => v.Acknowledgements)
            // Split query: evita l'esplosione cartesiana dei JOIN su più
            // collection annidate (Versions × Downloads × Acknowledgements).
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (document == null)
            return null;

        return new HRDocumentDetailDto
        {
            Id = document.Id,
            TenantId = document.TenantId,
            EmployeeId = document.EmployeeId,
            EmployeeName = $"{document.Employee.FirstName} {document.Employee.LastName}",
            DocumentType = document.DocumentType,
            DocumentTypeText = document.DocumentType.ToString(),
            Title = document.Title,
            Description = document.Description,
            Year = document.Year,
            Month = document.Month,
            CurrentVersion = document.CurrentVersion,
            Status = document.Status,
            StatusText = document.Status.ToString(),
            CreatedAt = document.CreatedAt,
            CreatedByEmail = document.CreatedBy.Email,
            UpdatedAt = document.UpdatedAt,
            UpdatedByEmail = document.UpdatedBy?.Email,
            Versions = document.Versions
                .Where(v => v.UploadStatus == UploadStatus.Completed)
                .OrderBy(v => v.VersionNumber)
                .Select(v => new HRDocumentVersionDto
                {
                    Id = v.Id,
                    VersionNumber = v.VersionNumber,
                    FileName = v.FileName,
                    ContentType = v.ContentType,
                    FileSizeBytes = v.FileSizeBytes,
                    ChangeNotes = v.ChangeNotes,
                    UploadStatus = v.UploadStatus,
                    UploadedAt = v.UploadedAt,
                    UploadedByEmail = v.UploadedBy.Email,
                    // Tracciamento riferito al dipendente corrente.
                    DownloadCount = v.Downloads.Count(dl => dl.EmployeeId == employeeId),
                    LastDownloadedAt = v.Downloads
                        .Where(dl => dl.EmployeeId == employeeId)
                        .Select(dl => (DateTime?)dl.DownloadedAt)
                        .Max(),
                    AcknowledgedAt = v.Acknowledgements
                        .Where(a => a.EmployeeId == employeeId)
                        .Select(a => (DateTime?)a.AcknowledgedAt)
                        .FirstOrDefault()
                }).ToList()
        };
    }

    public async Task<HRDocumentUploadResponseDto> CreateDocumentAsync(
        int tenantId,
        int userId,
        HRDocumentCreateDto dto)
    {
        var now = _clock.UtcNow;
        // Verifica che l'employee appartenga al tenant
        var employee = await _context.Employees
            .Include(e => e.Memberships)
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId && e.Memberships.Any(m => m.MerchantId == tenantId && m.IsActive));

        if (employee == null)
            throw new UnauthorizedAccessException("Employee not found or access denied");

        // Crea documento
        var document = new HRDocument
        {
            TenantId = tenantId,
            EmployeeId = dto.EmployeeId,
            DocumentType = dto.DocumentType,
            Title = dto.Title,
            Description = dto.Description,
            Year = dto.Year,
            Month = dto.Month,
            CurrentVersion = 1,
            Status = HRDocumentStatus.Draft,
            CreatedAt = now,
            CreatedByUserId = userId
        };

        _context.HRDocuments.Add(document);
        await _context.SaveChangesAsync();

        // Crea prima versione
        var blobPath = _fileStorage.BuildBlobPath(
            tenantId,
            dto.EmployeeId,
            dto.DocumentType,
            dto.Year,
            dto.Month,
            document.Id,
            1,
            "pdf" // Default, verrà aggiornato alla finalizzazione
        );

        var version = new HRDocumentVersion
        {
            HRDocumentId = document.Id,
            VersionNumber = 1,
            BlobPath = blobPath,
            FileName = "pending",
            ContentType = "application/octet-stream",
            FileSizeBytes = 0,
            UploadStatus = UploadStatus.Uploading,
            UploadedAt = now,
            UploadedByUserId = userId
        };

        _context.HRDocumentVersions.Add(version);
        await _context.SaveChangesAsync();

        // Genera SAS URL per upload
        var uploadUrl = await _fileStorage.GenerateUploadSasUrlAsync(blobPath);
    var expiresAt = now.AddMinutes(5);

        return new HRDocumentUploadResponseDto
        {
            DocumentId = document.Id,
            UploadUrl = uploadUrl,
            BlobPath = blobPath,
            ExpiresAt = expiresAt
        };
    }

    public async Task<bool> FinalizeDocumentUploadAsync(
        int documentId,
        int tenantId,
        HRDocumentFinalizeDto dto)
    {
        var now = _clock.UtcNow;
        if (!IsFinalizePayloadValid(dto))
            return false;

        var document = await _context.HRDocuments
            .Include(d => d.Versions)
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

        if (document == null)
            return false;

        // Trova la versione in stato Uploading
        var version = document.Versions
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault(v => v.UploadStatus == UploadStatus.Uploading);

        if (version == null)
            return false;

        // Verifica che il file esista nel blob storage
        if (!await _fileStorage.BlobExistsAsync(version.BlobPath))
        {
            version.UploadStatus = UploadStatus.Failed;
            await _context.SaveChangesAsync();
            return false;
        }

        // Verifica server-side: i metadati nel DTO arrivano dal client e non sono
        // affidabili. Confrontiamo la size dichiarata con quella reale del blob e
        // rifiutiamo upload incoerenti o oltre il limite (es. file gonfiato che
        // dichiara una size minore).
        var blobProperties = await _fileStorage.GetBlobPropertiesAsync(version.BlobPath);
        if (blobProperties.ContentLength <= 0 ||
            blobProperties.ContentLength > MaxUploadSizeBytes ||
            blobProperties.ContentLength != dto.FileSizeBytes)
        {
            version.UploadStatus = UploadStatus.Failed;
            await _context.SaveChangesAsync();
            return false;
        }

        // Aggiorna versione: la size è quella reale del blob, non quella del DTO.
        version.FileName = dto.FileName;
        version.ContentType = dto.ContentType;
        version.FileSizeBytes = blobProperties.ContentLength;
        version.FileHash = dto.FileHash;
        version.UploadStatus = UploadStatus.Completed;

        // CurrentVersion punta sempre all'ultima versione effettivamente caricata:
        // viene allineata solo qui, a upload completato (vedi AddDocumentVersionAsync).
        if (version.VersionNumber > document.CurrentVersion)
            document.CurrentVersion = version.VersionNumber;

        // Pubblica documento
        document.Status = HRDocumentStatus.Published;
        document.UpdatedAt = now;

        await _context.SaveChangesAsync();

        if (document.Employee.UserId.HasValue)
        {
            await _notificationService.CreateAsync(
                document.Employee.UserId.Value,
                "Nuovo documento disponibile",
                $"{document.Title} è ora disponibile nella sezione Documenti.",
                NotificationType.DocumentPublished,
                document.Id);
        }

        return true;
    }

    public async Task<HRDocumentVersionUploadResponseDto> AddDocumentVersionAsync(
        int documentId,
        int tenantId,
        int userId,
        string? changeNotes = null)
    {
        var now = _clock.UtcNow;
        var document = await _context.HRDocuments
            .Include(d => d.Versions)
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

        if (document == null)
            throw new UnauthorizedAccessException("Document not found or access denied");

        // Cleanup self-healing: una versione precedente rimasta in Uploading è un
        // upload abbandonato (il client non ha mai finalizzato). La marchiamo
        // Failed prima di crearne una nuova, così non resta orfana per sempre.
        foreach (var orphan in document.Versions.Where(v => v.UploadStatus == UploadStatus.Uploading))
            orphan.UploadStatus = UploadStatus.Failed;

        // Il numero della nuova versione è progressivo sul max esistente: non si
        // basa su CurrentVersion, che ora viene allineata solo a upload completato.
        var maxVersion = document.Versions.Count > 0
            ? document.Versions.Max(v => v.VersionNumber)
            : document.CurrentVersion;
        var nextVersion = maxVersion + 1;

        // Determina l'estensione dall'ultima versione completata e la valida:
        // FileName arriva dal client, va sanificato prima di finire nel blob path.
        var lastVersion = document.Versions
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();
        var extension = SanitizeExtension(
            lastVersion != null ? Path.GetExtension(lastVersion.FileName) : null);

        var blobPath = _fileStorage.BuildBlobPath(
            tenantId,
            document.EmployeeId,
            document.DocumentType,
            document.Year,
            document.Month,
            document.Id,
            nextVersion,
            extension
        );

        var version = new HRDocumentVersion
        {
            HRDocumentId = document.Id,
            VersionNumber = nextVersion,
            BlobPath = blobPath,
            FileName = "pending",
            ContentType = "application/octet-stream",
            FileSizeBytes = 0,
            ChangeNotes = changeNotes,
            UploadStatus = UploadStatus.Uploading,
            UploadedAt = now,
            UploadedByUserId = userId
        };

        _context.HRDocumentVersions.Add(version);

        // CurrentVersion NON viene incrementata qui: l'upload potrebbe non essere
        // mai finalizzato. Viene allineata da FinalizeDocumentUploadAsync quando
        // la versione passa a Completed.
        document.UpdatedAt = now;
        document.UpdatedByUserId = userId;

        await _context.SaveChangesAsync();

        var uploadUrl = await _fileStorage.GenerateUploadSasUrlAsync(blobPath);
    var expiresAt = now.AddMinutes(5);

        return new HRDocumentVersionUploadResponseDto
        {
            VersionId = version.Id,
            VersionNumber = nextVersion,
            UploadUrl = uploadUrl,
            BlobPath = blobPath,
            ExpiresAt = expiresAt
        };
    }

    public async Task<bool> UpdateDocumentAsync(
        int documentId,
        int tenantId,
        HRDocumentUpdateDto dto)
    {
        var document = await _context.HRDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

        if (document == null)
            return false;

        if (!string.IsNullOrEmpty(dto.Title))
            document.Title = dto.Title;

        if (dto.Description != null)
            document.Description = dto.Description;

        if (dto.Status.HasValue)
            document.Status = dto.Status.Value;

        document.UpdatedAt = _clock.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int tenantId)
    {
        var document = await _context.HRDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

        if (document == null)
            return false;

        document.IsDeleted = true;
        document.UpdatedAt = _clock.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<HRDocumentDownloadDto> GenerateEmployeeDownloadUrlAsync(
        int documentId,
        int tenantId,
        int employeeId,
        int? versionNumber = null)
    {
        var now = _clock.UtcNow;
        // Scoping difensivo: oltre a EmployeeId filtriamo anche per TenantId, così
        // un documento di un altro merchant non è mai raggiungibile nemmeno se gli
        // id collidessero.
        var document = await _context.HRDocuments
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d =>
                d.Id == documentId &&
                d.TenantId == tenantId &&
                d.EmployeeId == employeeId &&
                !d.IsDeleted);

        if (document == null)
            throw new UnauthorizedAccessException("Document not found or access denied");

        if (document.Status != HRDocumentStatus.Published)
            throw new UnauthorizedAccessException("Document is not published");

        var version = versionNumber.HasValue
            ? document.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber.Value)
            : document.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

        if (version == null || version.UploadStatus != UploadStatus.Completed)
            throw new FileNotFoundException("Document version not found or not completed");

        var downloadUrl = await _fileStorage.GenerateDownloadSasUrlAsync(version.BlobPath);
        var expiresAt = now.AddMinutes(5);

        // Audit trail: registriamo che il dipendente ha richiesto il download di
        // questa versione. È il dato "debole" — la presa visione formale richiede
        // una conferma esplicita (AcknowledgeVersionAsync).
        _context.HRDocumentDownloads.Add(new HRDocumentDownload
        {
            HRDocumentVersionId = version.Id,
            EmployeeId = employeeId,
            DownloadedAt = now
        });
        await _context.SaveChangesAsync();

        return new HRDocumentDownloadDto
        {
            DownloadUrl = downloadUrl,
            FileName = version.FileName,
            ExpiresAt = expiresAt
        };
    }

    public async Task<bool> AcknowledgeVersionAsync(
        int documentId,
        int tenantId,
        int employeeId,
        int versionNumber)
    {
        var now = _clock.UtcNow;
        // Il documento deve appartenere al dipendente, al suo merchant ed essere
        // pubblicato: la presa visione è un'azione self-service sui propri documenti.
        var document = await _context.HRDocuments
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d =>
                d.Id == documentId &&
                d.TenantId == tenantId &&
                d.EmployeeId == employeeId &&
                !d.IsDeleted);

        if (document == null || document.Status != HRDocumentStatus.Published)
            throw new UnauthorizedAccessException("Document not found or access denied");

        var version = document.Versions
            .FirstOrDefault(v => v.VersionNumber == versionNumber && v.UploadStatus == UploadStatus.Completed);
        if (version == null)
            throw new FileNotFoundException("Document version not found or not completed");

        // Idempotente: se la conferma esiste già non ne creiamo una seconda
        // (l'indice unico la rifiuterebbe comunque).
        var alreadyAcknowledged = await _context.HRDocumentAcknowledgements
            .AnyAsync(a => a.HRDocumentVersionId == version.Id && a.EmployeeId == employeeId);
        if (alreadyAcknowledged)
            return false;

        _context.HRDocumentAcknowledgements.Add(new HRDocumentAcknowledgement
        {
            HRDocumentVersionId = version.Id,
            EmployeeId = employeeId,
            AcknowledgedAt = now
        });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<HRDocumentAccessRowDto>> GetDocumentAccessLogAsync(
        int documentId,
        int tenantId)
    {
        var document = await _context.HRDocuments
            .Include(d => d.Versions)
                .ThenInclude(v => v.Downloads)
                    .ThenInclude(dl => dl.Employee)
            .Include(d => d.Versions)
                .ThenInclude(v => v.Acknowledgements)
                    .ThenInclude(a => a.Employee)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

        if (document == null)
            return new List<HRDocumentAccessRowDto>();

        var rows = new List<HRDocumentAccessRowDto>();
        foreach (var version in document.Versions.Where(v => v.UploadStatus == UploadStatus.Completed))
        {
            // Una riga per dipendente che ha avuto un qualsiasi accesso (download
            // o conferma) a questa versione.
            var employeeIds = version.Downloads.Select(dl => dl.EmployeeId)
                .Concat(version.Acknowledgements.Select(a => a.EmployeeId))
                .Distinct();

            foreach (var empId in employeeIds)
            {
                var downloads = version.Downloads.Where(dl => dl.EmployeeId == empId).ToList();
                var ack = version.Acknowledgements.FirstOrDefault(a => a.EmployeeId == empId);
                var employee = downloads.FirstOrDefault()?.Employee ?? ack?.Employee;

                rows.Add(new HRDocumentAccessRowDto
                {
                    EmployeeId = empId,
                    EmployeeName = employee != null
                        ? $"{employee.FirstName} {employee.LastName}"
                        : string.Empty,
                    VersionNumber = version.VersionNumber,
                    DownloadCount = downloads.Count,
                    LastDownloadedAt = downloads.Count > 0
                        ? downloads.Max(dl => dl.DownloadedAt)
                        : null,
                    AcknowledgedAt = ack?.AcknowledgedAt
                });
            }
        }

        return rows
            .OrderByDescending(r => r.VersionNumber)
            .ThenBy(r => r.EmployeeName)
            .ToList();
    }

    public async Task<HRDocumentDownloadDto> GenerateDownloadUrlAsync(
        int documentId,
        int tenantId,
        int? versionNumber = null)
    {
        var now = _clock.UtcNow;
        var document = await _context.HRDocuments
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId && !d.IsDeleted);

        if (document == null)
            throw new UnauthorizedAccessException("Document not found or access denied");

        var version = versionNumber.HasValue
            ? document.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber.Value)
            : document.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

        if (version == null || version.UploadStatus != UploadStatus.Completed)
            throw new FileNotFoundException("Document version not found or not completed");

        var downloadUrl = await _fileStorage.GenerateDownloadSasUrlAsync(version.BlobPath);
        var expiresAt = now.AddMinutes(5);

        return new HRDocumentDownloadDto
        {
            DownloadUrl = downloadUrl,
            FileName = version.FileName,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// Normalizza un'estensione (potenzialmente derivata da un FileName fornito
    /// dal client) a un valore sicuro per il blob path: niente separatori, solo
    /// estensioni note. Fallback a "pdf" se assente o non riconosciuta.
    /// </summary>
    private static string SanitizeExtension(string? rawExtension)
    {
        var extension = (rawExtension ?? string.Empty).TrimStart('.').Trim();
        return AllowedExtensions.Contains(extension)
            ? extension.ToLowerInvariant()
            : "pdf";
    }

    private static bool IsFinalizePayloadValid(HRDocumentFinalizeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FileName) || dto.FileName.Length > 255)
            return false;

        if (string.IsNullOrWhiteSpace(dto.ContentType) || !AllowedContentTypes.Contains(dto.ContentType))
            return false;

        if (dto.FileSizeBytes <= 0 || dto.FileSizeBytes > MaxUploadSizeBytes)
            return false;

        var extension = Path.GetExtension(dto.FileName).TrimStart('.');
        return !string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension);
    }
}
