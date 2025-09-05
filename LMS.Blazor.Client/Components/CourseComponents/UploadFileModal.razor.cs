using LMS.Blazor.Client.Services;
using LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace LMS.Blazor.Client.Components.CourseComponents;
public partial class UploadFileModal
{
    [Inject] private DocumentsClient Api { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }
    [Parameter] public string ModalId { get; set; } = "uploadDocModal";
    [Parameter] public int? CourseId { get; set; }
    [Parameter] public int? ModuleId { get; set; }
    [Parameter] public int? ActivityId { get; set; }
    [Parameter] public bool IsSubmission { get; set; } = true;
    [Parameter] public string? StudentId { get; set; }
    [Parameter] public EventCallback OnUploaded { get; set; }
    [Parameter] public long MaxUploadBytes { get; set; } = 20 * 1024 * 1024; // 20 MB default

    protected string? Error { get; set; }
    protected bool IsSubmitting { get; set; }
    protected string DisplayName { get; set; } = string.Empty;
    protected string? Description { get; set; }


    private IBrowserFile? _file;

    protected bool HasFile => _file is not null;
    protected string SelectedFileName => _file?.Name ?? string.Empty;
    protected string SelectedFileSizeText => _file is null ? "" : FormatBytes(_file.Size);
    protected string MaxUploadLabel => FormatBytes(MaxUploadBytes);

    protected bool CanSubmit =>
        HasFile &&
        !string.IsNullOrWhiteSpace(DisplayName) &&
        (CourseId.HasValue || ModuleId.HasValue || ActivityId.HasValue);

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrWhiteSpace(StudentId) && AuthStateTask is not null)
        {
            var auth = await AuthStateTask;
            StudentId = auth.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? auth.User.FindFirst("sub")?.Value
                        ?? StudentId;
        }
    }

    protected Task OnFileSelected(InputFileChangeEventArgs e)
    {
        Error = null;
        _file = null;

        var file = e.File;
        if (file is null)
        {
            return Task.CompletedTask;
        }

        if (file.Size > MaxUploadBytes)
        {
            Error = $"Filen är för stor ({FormatBytes(file.Size)}). Maxstorlek är {MaxUploadLabel}.";
            return Task.CompletedTask;
        }

        _file = file;

        if (string.IsNullOrWhiteSpace(DisplayName))
            DisplayName = file.Name;

        return Task.CompletedTask;
    }

    protected async Task SubmitAsync()
    {
        if (!CanSubmit || _file is null)
            return;

        Error = null;
        IsSubmitting = true;
        try
        {
            var meta = new UploadProjDocumentDto
            {
                DisplayName = DisplayName,
                Description = Description,
                IsSubmission = IsSubmission,
                CourseId = CourseId,
                ModuleId = ModuleId,
                ActivityId = ActivityId,
                StudentId = StudentId
            };

            ProjDocumentDto? created = null;

            if (ActivityId.HasValue)
                created = await Api.UploadToActivityAsync(ActivityId.Value, meta, _file, MaxUploadBytes);
            else if (ModuleId.HasValue)
                created = await Api.UploadToModuleAsync(ModuleId.Value, meta, _file, MaxUploadBytes);
            else if (CourseId.HasValue)
                created = await Api.UploadToCourseAsync(CourseId.Value, meta, _file, MaxUploadBytes);
            else
                throw new InvalidOperationException("No target (course/module/activity) provided.");

            if (created is null)
            {
                Error = "Uppladdningen misslyckades.";
                return;
            }

            await HideAsync();
            ResetForm();
            if (OnUploaded.HasDelegate)
                await OnUploaded.InvokeAsync();
        }
        catch (IOException)
        {
            Error = "Filen kunde inte läsas. Kontrollera filen och försök igen.";
        }
        catch (Exception ex)
        {
            Error = "Ett fel inträffade: " + ex.Message;
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    public async Task ShowAsync()
    {
        Error = null;
        await JS.InvokeVoidAsync("bootstrap.Modal.getOrCreateInstance", $"#{ModalId}");
        await JS.InvokeVoidAsync("eval", $"bootstrap.Modal.getOrCreateInstance(document.getElementById('{ModalId}')).show()");
    }

    public async Task HideAsync()
    {
        await JS.InvokeVoidAsync("eval", $"bootstrap.Modal.getOrCreateInstance(document.getElementById('{ModalId}')).hide()");
    }

    private void ResetForm()
    {
        DisplayName = string.Empty;
        Description = string.Empty;
        _file = null;
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int i = 0;
        while (size >= 1024 && i < units.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:0.#} {units[i]}";
    }
}
