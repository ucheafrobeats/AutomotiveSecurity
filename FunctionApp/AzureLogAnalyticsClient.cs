using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AutomotiveWorld
{
    internal class AzureLogAnalyticsClient
    {
        private const string RequestContentHeaderContentType = "application/json";
        private const string RequestHeaderAuthorization = "Authorization";
        private const string RequestHeaderXMsDate = "x-ms-date";
        private const string RequestHeaderLogType = "Log-Type";

        public string WorkspaceId { get; set; }

        public string SharedKey { get; set; }

        public string ApiVersion { get; set; }

        public AzureLogAnalyticsClient(string workspaceId, string sharedKey, string apiVersion = "2016-04-01")
        {
            WorkspaceId = workspaceId;
            SharedKey = sharedKey;
            ApiVersion = apiVersion;
        }
        public async Task Post(string logType, string json)
        {
            string requestUriString = $"https://{WorkspaceId}.ods.opinsights.azure.com/api/logs?api-version={ApiVersion}";
            string dateString = DateTime.UtcNow.ToString("r");
            string signature = GetSignature(HttpMethod.Post.ToString(), json.Length, dateString, "/api/logs");
            using HttpClient client = new HttpClient();

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(requestUriString),
                Headers = {
                    { RequestHeaderAuthorization, signature },
                    { RequestHeaderXMsDate,  dateString },
                    { RequestHeaderLogType, logType }
                },
                Content = new StringContent(json)
            };
            httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(RequestContentHeaderContentType);

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
        }

        private string GetSignature(string method, int contentLength, string date, string resource)
        {

            string message = $"{method}\n{contentLength}\n{RequestContentHeaderContentType}\nx-ms-date:{date}\n{resource}";
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            byte[] decodedKey = Convert.FromBase64String(SharedKey);
            using (HMACSHA256 encryptor = new HMACSHA256(decodedKey))
            {
                return $"SharedKey {WorkspaceId}:{Convert.ToBase64String(encryptor.ComputeHash(bytes))}";
            }
        }
    }
}