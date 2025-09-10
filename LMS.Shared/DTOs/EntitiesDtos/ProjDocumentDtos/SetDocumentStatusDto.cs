using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DocumentStatus { Pending, Review, Approved, Rejected }

    public sealed class SetDocumentStatusDto
    {
        public DocumentStatus Status { get; set; }
        public string? Feedback { get; set; } 
    }

}
