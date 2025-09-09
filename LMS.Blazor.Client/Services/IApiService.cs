namespace LMS.Blazor.Client.Services;

public interface IApiService
{
    Task<T?> CallApiAsync<T>(string endpoint, CancellationToken ct = default);
    Task<T?> PostAsync<T>(string endpoint, object data, CancellationToken ct = default);
    Task<bool> PutAsync(string endpoint, object data, CancellationToken ct = default);
    Task<T?> PutAsync<T>(string endpoint, object data, CancellationToken ct = default);

    Task<bool> DeleteAsync(string endpoint, CancellationToken ct = default);
}