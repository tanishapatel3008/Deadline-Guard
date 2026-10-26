using DeadlineGuard.Data;
using DeadlineGuard.Models;
using Microsoft.EntityFrameworkCore;

namespace DeadlineGuard.Services;

public class AttachmentUploadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DeadlineAttachment? Attachment { get; set; }

    public static AttachmentUploadResult Succeeded(DeadlineAttachment attachment) => new() { Success = true, Attachment = attachment };
    public static AttachmentUploadResult Failed(string error) => new() { Success = false, ErrorMessage = error };
}

public interface IAttachmentService
{
    Task<AttachmentUploadResult> UploadAttachmentAsync(int deadlineId, string userId, IFormFile file);
    Task<(Stream? Stream, string ContentType, string DownloadFileName)?> GetAttachmentFileAsync(int attachmentId, string userId);
    Task<bool> DeleteAttachmentAsync(int attachmentId, string userId);
    Task<List<DeadlineAttachment>> GetDeadlineAttachmentsAsync(int deadlineId, string userId);
}

public class AttachmentService : IAttachmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AttachmentService> _logger;

    private readonly string[] _permittedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".txt", ".docx", ".xlsx", ".zip" };
    private const long MaxFileSizeBytes = 15 * 1024 * 1024; // 15 MB

    public AttachmentService(ApplicationDbContext context, IWebHostEnvironment env, ILogger<AttachmentService> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    public async Task<AttachmentUploadResult> UploadAttachmentAsync(int deadlineId, string userId, IFormFile file)
    {
        // 1. Check deadline ownership
        var deadline = await _context.Deadlines.FirstOrDefaultAsync(d => d.Id == deadlineId && d.UserId == userId);
        if (deadline == null)
        {
            return AttachmentUploadResult.Failed("Deadline not found or unauthorized.");
        }

        // 2. Validate file size
        if (file == null || file.Length == 0)
        {
            return AttachmentUploadResult.Failed("No file was uploaded.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return AttachmentUploadResult.Failed($"File size exceeds the 15 MB limit (file size: {file.Length / (1024 * 1024.0):F1} MB).");
        }

        // 3. Validate file extension
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !_permittedExtensions.Contains(ext))
        {
            return AttachmentUploadResult.Failed($"Disallowed file type ({ext}). Allowed: {string.Join(", ", _permittedExtensions)}.");
        }

        // 4. Secure storage directory outside web root
        var uploadsFolder = Path.Combine(_env.ContentRootPath, "App_Data", "Uploads", userId);
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // 5. Randomized stored file name to avoid path traversal and name collisions
        var storedFileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsFolder, storedFileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // 6. Save metadata to database
        var attachment = new DeadlineAttachment
        {
            DeadlineId = deadlineId,
            UserId = userId,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            StoragePath = fullPath,
            UploadedAt = DateTime.UtcNow
        };

        _context.DeadlineAttachments.Add(attachment);

        // Record history
        _context.DeadlineHistories.Add(new DeadlineHistory
        {
            DeadlineId = deadlineId,
            UserId = userId,
            Action = "AttachmentAdded",
            NewValue = attachment.OriginalFileName,
            ChangedAt = DateTime.UtcNow,
            Notes = $"Uploaded file: {attachment.OriginalFileName} ({file.Length / 1024} KB)."
        });

        await _context.SaveChangesAsync();
        return AttachmentUploadResult.Succeeded(attachment);
    }

    public async Task<(Stream? Stream, string ContentType, string DownloadFileName)?> GetAttachmentFileAsync(int attachmentId, string userId)
    {
        var attachment = await _context.DeadlineAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.UserId == userId);

        if (attachment == null || !File.Exists(attachment.StoragePath))
        {
            return null;
        }

        var stream = new FileStream(attachment.StoragePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (stream, attachment.ContentType, attachment.OriginalFileName);
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId, string userId)
    {
        var attachment = await _context.DeadlineAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.UserId == userId);

        if (attachment == null) return false;

        if (File.Exists(attachment.StoragePath))
        {
            try
            {
                File.Delete(attachment.StoragePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove physical file {Path}", attachment.StoragePath);
            }
        }

        _context.DeadlineHistories.Add(new DeadlineHistory
        {
            DeadlineId = attachment.DeadlineId,
            UserId = userId,
            Action = "AttachmentRemoved",
            PreviousValue = attachment.OriginalFileName,
            ChangedAt = DateTime.UtcNow,
            Notes = $"Removed attachment: {attachment.OriginalFileName}."
        });

        _context.DeadlineAttachments.Remove(attachment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<DeadlineAttachment>> GetDeadlineAttachmentsAsync(int deadlineId, string userId)
    {
        return await _context.DeadlineAttachments
            .Where(a => a.DeadlineId == deadlineId && a.UserId == userId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync();
    }
}
