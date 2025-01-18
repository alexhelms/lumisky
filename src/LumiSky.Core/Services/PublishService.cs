using LumiSky.Core.Utilities;
using System.Net.Http.Headers;
using LumiSky.Core.Profile;
using System.Net.Http.Json;
using System.Text.Json;
namespace LumiSky.Core.Services;

public class PublishService
{
    private readonly IProfileProvider _profile;
    private readonly JsonSerializerOptions _jsonOptions;

    public PublishService(IProfileProvider profile)
    {
        _profile = profile;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };
    }

    private HttpClient CreateHttpClient(string? baseUrl = null)
    {
        baseUrl ??= _profile.Current.Publish.CfWorkerUrl;
        if (baseUrl.EndsWith('/'))
            baseUrl = baseUrl[..^1];

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(3)
        };

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _profile.Current.Publish.CfWorkerApiKey);
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", RuntimeUtil.UserAgent);
        return httpClient;
    }

    public async Task<bool> CheckConnection(string? baseUrl = null)
    {
        try
        {
            using var client = CreateHttpClient(baseUrl);
            var response = await client.GetAsync("/api/check");
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<PublishMetadata> GetMetadata()
    {
        using var client = CreateHttpClient();
        return await client.GetFromJsonAsync<PublishMetadata>("/api/metadata", _jsonOptions) ?? new();
    }

    public Task SetMetadata()
    {
        PublishMetadata metadata = new()
        {
            Title = _profile.Current.Publish.Title,
            ShowImage = _profile.Current.Publish.ShowPublishedImage,
            ShowPanorama = _profile.Current.Publish.ShowPublishedPanorama,
            ShowNightTimelapse = _profile.Current.Publish.ShowPublishedNightTimelapse,
            ShowDayTimelapse = _profile.Current.Publish.ShowPublishedDayTimelapse,
        };

        return SetMetadata(metadata);
    }

    public async Task SetMetadata(PublishMetadata metadata)
    {
        using var client = CreateHttpClient();
        var response = await client.PostAsJsonAsync("/api/metadata", metadata, _jsonOptions);
        response.EnsureSuccessStatusCode();

        Log.Information("Published metadata");
    }

    public async Task Upload(string filename, string keyName)
    {
        using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        string justFilename = Path.GetFileName(filename) ?? string.Empty;
        string extension = Path.GetExtension(filename);
        string contentType = Util.ExtensionToMimeType(extension);
        await SendToCloudflareWorker(stream, keyName, filename, contentType);
    }

    public Task Upload(Stream stream, string keyName, string filename, string contentType)
    {
        return SendToCloudflareWorker(stream, keyName, filename, contentType);
    }

    private async Task SendToCloudflareWorker(Stream stream, string keyName, string filename, string contentType)
    {
        using var _ = Serilog.Context.LogContext.PushProperty("SourceContext", GetType().Name);

        try
        {
            if (stream.Length == 0)
                throw new InvalidOperationException("Stream length must be greater than zero");

            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            streamContent.Headers.ContentLength = stream.Length;
            streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"file\"",
                FileName = $"\"{Path.GetFileName(filename)!}\"",
                FileNameStar = null  // cf worker does not like filename* parameter.
            };

            using var formData = new MultipartFormDataContent
            {
                streamContent
            };

            // Remove quotes around the boundary parameter, cf worker does not like them.
            var boundaryHeader = formData.Headers.ContentType!.Parameters.First(x => x.Name == "boundary");
            boundaryHeader.Value = boundaryHeader.Value!.Replace("\"", string.Empty);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/upload/{keyName}")
            {
                Content = formData,
            };

            using var client = CreateHttpClient();
            var response = await client.SendAsync(request);
            
            Log.Information("Published {KeyName}", keyName);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error publishing {KeyName} {Filename}", keyName, filename);
        }
    }
}

public record PublishMetadata
{
    public string Title { get; set; } = string.Empty;
    public bool ShowImage { get; set; }
    public bool ShowPanorama { get; set; }
    public bool ShowNightTimelapse { get; set; }
    public bool ShowDayTimelapse { get; set; }
}