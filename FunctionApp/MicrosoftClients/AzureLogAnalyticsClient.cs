using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AutomotiveWorld.Network
{
    public class AzureLogAnalyticsClient
    {
        private const string LogAnalyticsDataPlaneUrl = "https://{0}.ods.opinsights.azure.com/api/logs?api-version={1}";
        private const string RequestContentHeaderContentType = "application/json";
        private const string RequestHeaderAuthorization = "Authorization";
        private const string RequestHeaderXMsDate = "x-ms-date";
        private const string RequestHeaderLogType = "Log-Type";
        private const string ApiLogsEndpoint = "/api/logs";

        private readonly HttpClient HttpClient;

        public string WorkspaceId { get; set; }

        public string SharedKey { get; set; }

        public string ApiVersion { get; set; }

        public AzureLogAnalyticsClient(string workspaceId, string sharedKey, HttpClient httpClient, string apiVersion = "2016-04-01")
        {
            WorkspaceId = workspaceId;
            SharedKey = sharedKey;
            ApiVersion = apiVersion;
            HttpClient = httpClient;
        }
        public async Task Post(string logType, string json)
        {
            string requestUriString = string.Format(LogAnalyticsDataPlaneUrl, WorkspaceId, ApiVersion);
            string dateString = DateTime.UtcNow.ToString("r");
            string signature = GetSignature(HttpMethod.Post.ToString(), json.Length, dateString, ApiLogsEndpoint);

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

            HttpResponseMessage response = await HttpClient.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
        }

        private string GetSignature(string method, int contentLength, string date, string resource)
        {

            string message = $"{method}\n{contentLength}\n{RequestContentHeaderContentType}\n{RequestHeaderXMsDate}:{date}\n{resource}";
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            byte[] decodedKey = Convert.FromBase64String(SharedKey);
            using (HMACSHA256 encryptor = new HMACSHA256(decodedKey))
            {
                return $"SharedKey {WorkspaceId}:{Convert.ToBase64String(encryptor.ComputeHash(bytes))}";
            }
        }
    }
}