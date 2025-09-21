using System.Net;
using System.Text.Json;

namespace HttpClientReview;

// Intentionally flawed code for refactoring

//.Result used instead of awaiting
//JsonDocument.Parse(json).RootElement.GetString() wasn't working
//status code not returned as expected
public class UserClient
{
    private readonly HttpClient _http;

    public UserClient(HttpClient http)
    {
        _http = http ?? new HttpClient();
    }

    public async Task<string?> GetUserName(int id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var req = new HttpRequestMessage(HttpMethod.Get, $"api/users/{id}");
        req.Headers.TryAddWithoutValidation("Accept", "application/json");

        var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

        if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NotFound)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);

            throw new HttpRequestException(
                $"GET {req.RequestUri} failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}", null, resp.StatusCode);
        }
        else if(resp.StatusCode != HttpStatusCode.NotFound)
        {
            var json = await resp.Content.ReadAsStringAsync(ct);

            if (JsonDocument.Parse(json).RootElement.TryGetProperty("name", out JsonElement name))
            {
                return name.ToString();
            }
        }

        return null;
    }
}
