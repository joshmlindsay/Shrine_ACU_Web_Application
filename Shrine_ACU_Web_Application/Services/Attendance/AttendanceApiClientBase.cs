using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Shrine_ACU_Web_Application.Models.Attendance;

namespace Shrine_ACU_Web_Application.Services.Attendance;

public abstract class AttendanceApiClientBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    protected AttendanceApiClientBase(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        _httpClient = httpClientFactory.CreateClient("AcuCarShowApi");
        _logger = logger;
    }

    protected async Task<T?> GetAsync<T>(string uri, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await SendAsync(request, null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<T>(AttendanceApiJson.SerializerOptions, cancellationToken);
    }

    protected async Task<PagedResult<T>> GetPagedAsync<T>(string uri, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var data = await GetAsync<PagedResult<T>>(uri, cancellationToken);
        if (data is not null)
        {
            data.Items ??= [];
            if (data.Page <= 0) data.Page = page;
            if (data.PageSize <= 0) data.PageSize = pageSize;
            return data;
        }

        var fallback = await GetAsync<List<T>>(uri, cancellationToken) ?? [];
        return new PagedResult<T>
        {
            Items = fallback,
            TotalCount = fallback.Count,
            Page = page,
            PageSize = pageSize
        };
    }

    protected async Task<T?> PostAsync<T>(string uri, object? payload, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = payload is null
                ? null
                : new StringContent(JsonSerializer.Serialize(payload, AttendanceApiJson.SerializerOptions), Encoding.UTF8, "application/json")
        };

        using var response = await SendAsync(request, payload, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        if (response.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(AttendanceApiJson.SerializerOptions, cancellationToken);
    }

    protected async Task PostAsync(string uri, object? payload, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = payload is null
                ? null
                : new StringContent(JsonSerializer.Serialize(payload, AttendanceApiJson.SerializerOptions), Encoding.UTF8, "application/json")
        };

        using var response = await SendAsync(request, payload, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    protected async Task PutAsync(string uri, object payload, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, AttendanceApiJson.SerializerOptions), Encoding.UTF8, "application/json")
        };

        using var response = await SendAsync(request, payload, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    protected async Task DeleteAsync(string uri, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, uri);
        using var response = await SendAsync(request, null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    protected async Task<(byte[] Data, string? ContentType, string? FileName)> DownloadAsync(string uri, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await SendAsync(request, null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType;
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName;
        return (data, contentType, fileName?.Trim('"'));
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, object? payload, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            _logger.LogInformation("Attendance API {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms. Payload: {Payload}",
                request.Method,
                request.RequestUri,
                (int)response.StatusCode,
                timer.ElapsedMilliseconds,
                payload);
            return response;
        }
        catch
        {
            _logger.LogWarning("Attendance API {Method} {Path} failed in {ElapsedMs}ms. Payload: {Payload}",
                request.Method,
                request.RequestUri,
                timer.ElapsedMilliseconds,
                payload);
            throw;
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var rawBody = response.Content is null ? null : await response.Content.ReadAsStringAsync(cancellationToken);
        var message = ExtractErrorMessage(rawBody) ?? response.ReasonPhrase ?? "Unexpected API error.";

        throw new ApiErrorException(new ApiError
        {
            StatusCode = (int)response.StatusCode,
            Message = message,
            RawBody = rawBody
        });
    }

    private static string? ExtractErrorMessage(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("message", out var messageProp))
                {
                    return messageProp.GetString();
                }

                if (doc.RootElement.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Object)
                {
                    var validationErrors = errorsProp.EnumerateObject()
                        .SelectMany(error => error.Value.ValueKind == JsonValueKind.Array
                            ? error.Value.EnumerateArray().Select(item => item.GetString())
                            : [])
                        .Where(text => !string.IsNullOrWhiteSpace(text))
                        .Take(3)
                        .ToArray();

                    if (validationErrors.Length > 0)
                    {
                        return string.Join(" ", validationErrors);
                    }
                }

                if (doc.RootElement.TryGetProperty("title", out var titleProp))
                {
                    return titleProp.GetString();
                }
            }
        }
        catch
        {
            return rawBody;
        }

        return rawBody;
    }
}
