using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Service.Contracts.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Infractructure.Storage
{
    public sealed class LocalFileStorage : IFileStorage
    {
        private readonly ILogger<LocalFileStorage> _logger;
        private readonly string _root;
        private readonly string _publicBase;

        public LocalFileStorage(IOptions<FileStorageOptions> options, ILogger<LocalFileStorage> logger)
        {
            _logger = logger;
            _root = options.Value.RootPath;
            _publicBase = options.Value.PublicBasePath.Trim().TrimStart('/').TrimEnd('/');

            Directory.CreateDirectory(_root);
        }

        public async Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct = default)
        {
            var safeName = Path.GetFileName(originalFileName);
            var now = DateTime.UtcNow;

            var subDir = Path.Combine(now.Year.ToString(), now.Month.ToString("00"));
            var folder = Path.Combine(_root, subDir);
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}_{safeName}";
            var fullPath = Path.Combine(folder, fileName);

            await using (var fs = File.Create(fullPath))
            {
                await content.CopyToAsync(fs, ct);
            }

            var relative = $"{_publicBase}/{subDir.Replace('\\', '/')}/{fileName}";
            _logger.LogInformation("Saved file at {RelativePath}", relative);
            return relative;
        }

        public Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default)
        {
            var rel = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            var full = Path.Combine("wwwroot", rel); // assumes PublicBasePath lives under wwwroot
            Stream s = File.OpenRead(full);
            return Task.FromResult(s);
        }

        public Task DeleteAsync(string relativePath, CancellationToken ct = default)
        {
            try
            {
                var rel = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
                var full = Path.Combine("wwwroot", rel);
                if (File.Exists(full))
                {
                    File.Delete(full);
                    _logger.LogInformation("Deleted file {RelativePath}", relativePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file {RelativePath}", relativePath);
            }

            return Task.CompletedTask;
        }
    }
}
