using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

public class HRDocumentService : IHRDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public HRDocumentService(ApplicationDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
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
            .Where(d => d.TenantId == tenantId)
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

    public async Task<HRDocumentDetailDto?> GetDocumentByIdAsync(int documentId, int tenantId)
    {
        var document = await _context.HRDocuments
            .Where(d => d.Id == documentId && d.TenantId == tenantId)
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

    public async Task<HRDocumentUploadResponseDto> CreateDocumentAsync(
        int tenantId,
        int userId,
        HRDocumentCreateDto dto)
    {
        // Verifica che l'employee appartenga al tenant
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId && e.MerchantId == tenantId);

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
            CreatedAt = DateTime.UtcNow,
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
            UploadedAt = DateTime.UtcNow,
            UploadedByUserId = userId
        };

        _context.HRDocumentVersions.Add(version);
        await _context.SaveChangesAsync();

        // Genera SAS URL per upload
        var uploadUrl = await _fileStorage.GenerateUploadSasUrlAsync(blobPath);
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

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
        var document = await _context.HRDocuments
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId);

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

        // Aggiorna versione
        version.FileName = dto.FileName;
        version.ContentType = dto.ContentType;
        version.FileSizeBytes = dto.FileSizeBytes;
        version.FileHash = dto.FileHash;
        version.UploadStatus = UploadStatus.Completed;

        // Pubblica documento
        document.Status = HRDocumentStatus.Published;
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<HRDocumentVersionUploadResponseDto> AddDocumentVersionAsync(
        int documentId,
        int tenantId,
        int userId,
        string? changeNotes = null)
    {
        var document = await _context.HRDocuments
            .Include(d => d.Versions)
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId);

        if (document == null)
            throw new UnauthorizedAccessException("Document not found or access denied");

        var nextVersion = document.CurrentVersion + 1;

        // Determina estensione dall'ultima versione
        var lastVersion = document.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        var extension = lastVersion != null
            ? Path.GetExtension(lastVersion.FileName).TrimStart('.')
            : "pdf";

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
            UploadedAt = DateTime.UtcNow,
            UploadedByUserId = userId
        };

        _context.HRDocumentVersions.Add(version);

        // Aggiorna versione corrente
        document.CurrentVersion = nextVersion;
        document.UpdatedAt = DateTime.UtcNow;
        document.UpdatedByUserId = userId;

        await _context.SaveChangesAsync();

        var uploadUrl = await _fileStorage.GenerateUploadSasUrlAsync(blobPath);
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

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
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId);

        if (document == null)
            return false;

        if (!string.IsNullOrEmpty(dto.Title))
            document.Title = dto.Title;

        if (dto.Description != null)
            document.Description = dto.Description;

        if (dto.Status.HasValue)
            document.Status = dto.Status.Value;

        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int tenantId)
    {
        var document = await _context.HRDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId);

        if (document == null)
            return false;

        document.IsDeleted = true;
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<HRDocumentDownloadDto> GenerateEmployeeDownloadUrlAsync(
        int documentId,
        int employeeId,
        int? versionNumber = null)
    {
        var document = await _context.HRDocuments
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.EmployeeId == employeeId);

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
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        return new HRDocumentDownloadDto
        {
            DownloadUrl = downloadUrl,
            FileName = version.FileName,
            ExpiresAt = expiresAt
        };
    }

    public async Task<HRDocumentDownloadDto> GenerateDownloadUrlAsync(
        int documentId,
        int tenantId,
        int? versionNumber = null)
    {
        var document = await _context.HRDocuments
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId);

        if (document == null)
            throw new UnauthorizedAccessException("Document not found or access denied");

        var version = versionNumber.HasValue
            ? document.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber.Value)
            : document.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

        if (version == null || version.UploadStatus != UploadStatus.Completed)
            throw new FileNotFoundException("Document version not found or not completed");

        var downloadUrl = await _fileStorage.GenerateDownloadSasUrlAsync(version.BlobPath);
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        return new HRDocumentDownloadDto
        {
            DownloadUrl = downloadUrl,
            FileName = version.FileName,
            ExpiresAt = expiresAt
        };
    }
}
