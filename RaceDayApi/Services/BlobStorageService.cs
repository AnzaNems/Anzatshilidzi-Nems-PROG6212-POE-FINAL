using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDayApi.Services;

/// <summary>
/// Wraps Azure Blob Storage for image uploads (event banners and profile
/// pictures). The MVC app never talks to Azure directly - it always sends
/// the file to the API, and only the API holds the connection string
/// (stored in user-secrets during development).
///
/// If no connection string is configured (e.g. a fresh clone), IsConfigured
/// is false and /api/files answers 503 instead of crashing - everything else
/// in the system keeps working.
/// </summary>
public class BlobStorageService
{
    private readonly string? _connectionString;
    private const string ContainerName = "raceday-images";
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;   // 5 MB

    public BlobStorageService(IConfiguration configuration)
    {
        _connectionString = configuration["AzureBlob:ConnectionString"];
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    /// <summary>
    /// Uploads an image and returns its full public URL, which callers store
    /// in the database (Event.BannerImageUrl / User.ProfilePictureUrl).
    /// A GUID prefix prevents one upload from overwriting another.
    /// </summary>
    public async Task<string?> UploadImageAsync(IFormFile file)
    {
        if (!IsConfigured || file.Length == 0 || file.Length > MaxFileSizeBytes)
            return null;

        var extension = Path.GetExtension(file.FileName);      // e.g. ".jpg"
        var blobName = $"{Guid.NewGuid():N}{extension}";

        var container = new BlobContainerClient(_connectionString, ContainerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var blob = container.GetBlobClient(blobName);
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blob.Uri.ToString();
    }
}
