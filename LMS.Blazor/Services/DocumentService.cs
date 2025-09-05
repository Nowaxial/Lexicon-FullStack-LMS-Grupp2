using Domain.Models.Entities;

public class DocumentService
{
    private readonly HttpClient _http;

    public DocumentService(HttpClient http)
    {
        _http = http;
    }

    public async Task UploadAsync(MultipartFormDataContent content)
    {
        await _http.PostAsync("api/documents/upload", content);
    }

    public async Task<byte[]> DownloadAsync(int id)
    {
        return await _http.GetByteArrayAsync($"api/documents/download/{id}");
    }

    public async Task<List<ProjDocument>> GetAllAsync()
    {
        return await _http.GetFromJsonAsync<List<ProjDocument>>("api/documents") ?? new();
    }
}