using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace HabitRPG.Api.Tests.Integration
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> PatchAsJsonAsync<T>(this HttpClient client, string requestUri, T value)
        {
            var json = JsonSerializer.Serialize(value);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Patch, requestUri)
            {
                Content = content
            };
            return await client.SendAsync(request);
        }
    }
}