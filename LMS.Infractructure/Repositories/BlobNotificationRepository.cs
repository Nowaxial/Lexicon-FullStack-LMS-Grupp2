using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text;
using System.Threading.Tasks;

public class BlobNotificationRepository
{
    private readonly BlobContainerClient _container;

    public BlobNotificationRepository(string connectionString, string containerName)
    {
        _container = new BlobContainerClient(connectionString, containerName);
        _container.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task SaveAsync(string fileName, string json)
    {
        var blob = _container.GetBlobClient(fileName);
        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
        {
            await blob.UploadAsync(ms, overwrite: true);
        }
    }

    public async Task<string?> LoadAsync(string fileName)
    {
        var blob = _container.GetBlobClient(fileName);
        if (await blob.ExistsAsync())
        {
            var result = await blob.DownloadContentAsync();
            return result.Value.Content.ToString();
        }
        return null;
    }
}
