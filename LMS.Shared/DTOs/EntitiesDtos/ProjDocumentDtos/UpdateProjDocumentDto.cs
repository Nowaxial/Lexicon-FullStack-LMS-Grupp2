using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos
{
    public record UpdateProjDocumentDto
    {
        public string DisplayName { get; init; } = null!;
        public string? Description { get; init; }
    }
}
