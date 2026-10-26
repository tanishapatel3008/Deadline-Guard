using System.Security.Claims;
using DeadlineGuard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeadlineGuard.Controllers;

[Authorize]
public class AttachmentsController : Controller
{
    private readonly IAttachmentService _attachmentService;

    public AttachmentsController(IAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(int deadlineId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a valid file to upload.";
            return RedirectToAction("Details", "Deadlines", new { id = deadlineId });
        }

        var result = await _attachmentService.UploadAttachmentAsync(deadlineId, CurrentUserId, file);
        if (result.Success)
        {
            TempData["SuccessMessage"] = $"File \"{result.Attachment?.OriginalFileName}\" attached successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "File upload failed.";
        }

        return RedirectToAction("Details", "Deadlines", new { id = deadlineId });
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var fileData = await _attachmentService.GetAttachmentFileAsync(id, CurrentUserId);
        if (fileData == null || fileData.Value.Stream == null)
        {
            return NotFound("File not found or access denied.");
        }

        return File(fileData.Value.Stream, fileData.Value.ContentType, fileData.Value.DownloadFileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int deadlineId)
    {
        var success = await _attachmentService.DeleteAttachmentAsync(id, CurrentUserId);
        if (success)
        {
            TempData["SuccessMessage"] = "Attachment deleted.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to remove attachment.";
        }

        return RedirectToAction("Details", "Deadlines", new { id = deadlineId });
    }
}
