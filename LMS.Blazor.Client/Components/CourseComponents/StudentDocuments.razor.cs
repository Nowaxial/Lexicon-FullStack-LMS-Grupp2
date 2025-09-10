using LMS.Blazor.Client.Services;
using LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LMS.Blazor.Client.Components.CourseComponents
{
    public partial class StudentDocuments : IDisposable
    {

        [Inject] private IApiService Api { get; set; } = default!;
        [Inject] private DocumentsClient DocsClient { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;


        [Parameter] public int? CourseId { get; set; }
        [Parameter] public int? ModuleId { get; set; }
        [Parameter] public int? ActivityId { get; set; }
        [Parameter] public string? StudentId { get; set; }
        [Parameter] public bool AutoLoad { get; set; } = true;


        private bool _isLoading = true;
        private string? _error;
        private List<ProjDocumentDto> _docs = new();

        private CancellationTokenSource? _cts;
        private string? _lastEndpoint;

        private readonly HashSet<int> _deleting = new();

        private static string LabelFor(DocumentStatus s) => s switch
        {
            DocumentStatus.Pending => "Ej bedömd",
            DocumentStatus.Review => "Granskning",
            DocumentStatus.Approved => "Godkänd",
            DocumentStatus.Rejected => "Underkänd",
            _ => s.ToString()
        };

        private static DocumentStatus ParseStatus(string? value)
        {
            var v = (value ?? "").Trim().ToLowerInvariant();
            return v switch
            {
                "ej bedömd" or "pending" => DocumentStatus.Pending,
                "granskning" or "review" => DocumentStatus.Review,
                "godkänd" or "approved" => DocumentStatus.Approved,
                "underkänd" or "rejected" => DocumentStatus.Rejected,
                _ => DocumentStatus.Pending
            };
        }

        private static string StatusCss(string? status) => (status ?? "").ToLowerInvariant() switch
        {
            "godkänd" or "approved" => "ok",
            "granskning" or "review" => "review",
            "underkänd" or "rejected" => "bad",
            _ => "pending"
        };

        protected override async Task OnParametersSetAsync()
        {
            if (AutoLoad)
                await ReloadAsync();
        }


        public Task RefreshAsync() => ReloadAsync(force: true);

        public async Task ReloadAsync(bool force = false)
        {
            _error = null;

            var endpoint = BuildEndpoint();
            if (endpoint is null)
            {
                _docs = new();
                _isLoading = false;
                StateHasChanged();
                return;
            }

            if (!force && string.Equals(endpoint, _lastEndpoint, StringComparison.OrdinalIgnoreCase))
                return;

            _lastEndpoint = endpoint;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _isLoading = true;
            StateHasChanged();

            try
            {
                var result = await Api.CallApiAsync<IEnumerable<ProjDocumentDto>>(endpoint, _cts.Token);

                var docs = (result ?? Enumerable.Empty<ProjDocumentDto>());

                if (!string.IsNullOrWhiteSpace(StudentId))
                    docs = docs.Where(d => string.Equals(d.StudentId ?? "", StudentId, StringComparison.OrdinalIgnoreCase));

                docs = docs.Where(d => d.IsSubmission);

                _docs = docs
                    .OrderByDescending(d => d.UploadedAt)
                    .ThenByDescending(d => d.Id)
                    .Take(50)
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                // ignore—newer load started
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                _docs = new();
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }

        private string? BuildEndpoint()
        {
            if (ActivityId.HasValue)
                return $"api/documents/by-activity/{ActivityId.Value}";
            if (ModuleId.HasValue)
                return $"api/documents/by-module/{ModuleId.Value}";
            if (CourseId.HasValue)
                return $"api/documents/by-course/{CourseId.Value}";
            if (!string.IsNullOrWhiteSpace(StudentId))
                return $"api/documents/by-student/{Uri.EscapeDataString(StudentId!)}";

            return null;
        }

        internal static string BuildDownloadUrl(int id)
            => $"proxy?endpoint={Uri.EscapeDataString($"api/documents/{id}/download")}";

        private static string GetExt(string? path)
        => string.IsNullOrWhiteSpace(path) ? "" : Path.GetExtension(path).TrimStart('.').ToUpperInvariant();

        private void Download(int id)
            => Nav.NavigateTo(BuildDownloadUrl(id), forceLoad: true);

        private async Task DeleteAsync(ProjDocumentDto doc)
        {
            try
            {
                var okAsk = await JS.InvokeAsync<bool>("confirm",
               $"Ta bort \"{doc.DisplayName}\"?");
                if (!okAsk) return;

                _deleting.Add(doc.Id);
                StateHasChanged();

                var ok = await DocsClient.DeleteAsync(doc.Id);
                if (ok)
                {
                    _docs.RemoveAll(d => d.Id == doc.Id);
                }
                else
                {
                    _error = "Kunde inte ta bort filen (saknas eller behörighet saknas).";
                }
            }
            catch (HttpRequestException ex)
            {
                _error = $"Borttagning misslyckades: {ex.Message}";
            }
            catch (Exception ex)
            {
                _error = $"Ett fel inträffade: {ex.Message}";
            }
            finally
            {
                _deleting.Remove(doc.Id);
                StateHasChanged();
            }
        }


        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }


    }
}
