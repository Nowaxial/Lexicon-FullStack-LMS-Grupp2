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

            // DEBUG: Logga var filerna sparas
            _logger.LogInformation("FileStorage initialized with root: {Root}", _root);
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
            _logger.LogInformation("Saved file at full path: {FullPath}, returning relative: {RelativePath}", fullPath, relative);
            return relative;
        }

        public Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default)
        {
            // ÄNDRAT: Använd _root istället för "wwwroot"
            var rel = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);

            // Ta bort publicBase från början om det finns där
            if (rel.StartsWith(_publicBase + Path.DirectorySeparatorChar) || rel.StartsWith(_publicBase + '/'))
            {
                rel = rel.Substring(_publicBase.Length + 1);
            }

            var full = Path.Combine(_root, rel);

            _logger.LogInformation("Reading file from: {FullPath}, exists: {Exists}", full, File.Exists(full));

            Stream s = File.OpenRead(full);
            return Task.FromResult(s);
        }

        public Task DeleteAsync(string relativePath, CancellationToken ct = default)
        {
            try
            {
                // ÄNDRAT: Använd _root istället för "wwwroot"
                var rel = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);

                // Ta bort publicBase från början om det finns där
                if (rel.StartsWith(_publicBase + Path.DirectorySeparatorChar) || rel.StartsWith(_publicBase + '/'))
                {
                    rel = rel.Substring(_publicBase.Length + 1);
                }

                var full = Path.Combine(_root, rel);

                if (File.Exists(full))
                {
                    File.Delete(full);
                    _logger.LogInformation("Deleted file {FullPath}", full);
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
