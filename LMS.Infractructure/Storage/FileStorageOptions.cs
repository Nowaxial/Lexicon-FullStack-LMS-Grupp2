using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Infractructure.Storage
{
    public sealed class FileStorageOptions
    {
        public string RootPath { get; set; } = Path.Combine("wwwroot", "uploads");
        public string PublicBasePath { get; set; } = "uploads";
    }
}
