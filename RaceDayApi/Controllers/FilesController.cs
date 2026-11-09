using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDayApi.Models.DTOs;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

/// <summary>
/// File upload endpoint (Part 3 - Azure Blob Storage). Any authenticated user
/// can upload an image: Organisers use it for event banners, Participants for
/// profile pictures. The file is stored in Azure Blob Storage and the public
/// URL is returned so it can be saved on the event or user record.
/// The MVC app never contacts Azure directly - only this API does.
/// </summary>
[ApiController]
[Route("api/files")]
[Authorize]
[Produces("application/json")]
public class FilesController : ControllerBase
{
    private readonly BlobStorageService _blob;

    public FilesController(BlobStorageService blob) => _blob = blob;

    /// <summary>
    /// Uploads an image (multipart/form-data, field name "file") to Azure Blob
    /// Storage and returns its public URL. Maximum size 5 MB; images only.
    /// </summary>
    /// <param name="file">The image file, sent as form field "file".</param>
    /// <returns>200 OK with the blob URL and name; 400 if no file, wrong type or too large; 401 if not logged in; 503 if Azure Blob Storage is not configured on the API.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BlobUploadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (!_blob.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Azure Blob Storage is not configured. Add the connection string in user-secrets (see README)." });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file was received. Send the image as form field 'file'." });

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only image files (jpg, png, gif, webp) can be uploaded." });

        var url = await _blob.UploadImageAsync(file);
        if (url == null)
            return BadRequest(new { message = "Upload failed - the file must be an image under 5 MB." });

        return Ok(new BlobUploadResult
        {
            Url = url,
            BlobName = url[(url.LastIndexOf('/') + 1)..]
        });
    }
}
