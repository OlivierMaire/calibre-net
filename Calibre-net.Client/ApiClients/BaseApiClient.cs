namespace Calibre_net.Client.ApiClients;

using System.Net.Http.Headers;
using System.Security.Cryptography;
using Calibre_net.Client.Services;

// [SingletonRegistration]
public partial class BaseApiClient
{
    private readonly IHttpClientFactory httpClientFactory;

    public BaseApiClient(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public Task<HttpClient> CreateHttpClientAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(this.httpClientFactory.CreateClient("calibre-net.Api"));
    }


    static protected void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
    {
        settings.PropertyNameCaseInsensitive = true;
    }

    public static Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, System.Text.StringBuilder urlBuilder, CancellationToken ct)
    {
        // return PrepareRequestAsync(client, request, urlBuilder.ToString(), ct);
        return Task.CompletedTask;
    }
    public static async Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, string url, CancellationToken ct)
    {
        if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put)
        {
            if (request.Content is ByteArrayContent bytes)
            {
                using (var sha256hash = SHA256.Create())
                {
                    byte[] data = await bytes.ReadAsByteArrayAsync(ct);

                    byte[] payloadBytes = sha256hash.ComputeHash(data);
                    var digest = Convert.ToBase64String(payloadBytes);
                    digest = "SHA-256=" + digest;
                    request.Headers.Add("x-request-hash", digest);
                }
            }
        }
    }

    public static async Task ProcessResponseAsync(HttpClient client, HttpResponseMessage response, CancellationToken ct)
    {
        

            if (response.Content is ByteArrayContent bytes)
            {
                using (var sha256hash = SHA256.Create())
                {
                    byte[] data = await bytes.ReadAsByteArrayAsync(ct);

                    byte[] payloadBytes = sha256hash.ComputeHash(data);
                    var digest = Convert.ToBase64String(payloadBytes);
                    digest = "SHA-256=" + digest;
                    // response.Headers.Add("x-request-hash", digest);
                    // response.Headers.Add("etag", digest);
                }
            }
    }




}

// [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.0.0.0 (NJsonSchema v11.0.0.0 (Newtonsoft.Json v13.0.0.0))")]
// public partial class ProblemDetails
// {

//     [System.Text.Json.Serialization.JsonPropertyName("type")]
//     public string Type { get; set; }

//     [System.Text.Json.Serialization.JsonPropertyName("title")]
//     public string Title { get; set; }

//     [System.Text.Json.Serialization.JsonPropertyName("status")]
//     public int? Status { get; set; }

//     [System.Text.Json.Serialization.JsonPropertyName("detail")]
//     public string Detail { get; set; }

//     [System.Text.Json.Serialization.JsonPropertyName("instance")]
//     public string Instance { get; set; }

//     private System.Collections.Generic.IDictionary<string, object> _additionalProperties;

//     [System.Text.Json.Serialization.JsonExtensionData]
//     public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
//     {
//         get { return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>()); }
//         set { _additionalProperties = value; }
//     }

// }