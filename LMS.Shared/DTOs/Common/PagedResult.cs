using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Shared.DTOs.Common
{
    public class PagedResult<T>
    {
        public required IReadOnlyList<T> Items { get; init; }
        public int Total { get; init; }
        public int Page { get; init; }
        public int Size { get; init; }
    }
}
