using LMS.Shared.DTOs.Demo;
using Microsoft.AspNetCore.Components;

namespace LMS.Blazor.Client.Components
{
    public partial class DemoAuthComponent
    {
        [Parameter]
        public required DemoAuthDto DTO { get; set; }
    }
}