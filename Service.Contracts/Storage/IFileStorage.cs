using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Storage
{
    public interface IFileStorage
    {
        Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct = default);
        Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default);
        Task DeleteAsync(string relativePath, CancellationToken ct = default);
    }
}
