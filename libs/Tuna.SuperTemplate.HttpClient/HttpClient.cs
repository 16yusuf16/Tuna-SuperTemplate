using System.Text.Json;
using System.Text.Json.Serialization;
using Tuna.SuperTemplate.HttpClient.Interface;
using Tuna.SuperTemplate.HttpClient.Models;

namespace Tuna.SuperTemplate.HttpClient
{
    public class HttpClient : IHttpClient
    {
        private readonly System.Net.Http.HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        public HttpClient()
        {
            _httpClient = new System.Net.Http.HttpClient{
                Timeout = TimeSpan.FromMinutes(3)
            };
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, 
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, 
            };
            _jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public async Task<ResponseMessage<TResult>> SendAsync<TResult>(HttpRequestMessage requestMessage, HttpClientHandler httpClientHandler = null, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return await ReadErrorAsync<TResult>(response);

            var serialized = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<TResult>(serialized, _jsonSerializerOptions);
            return new ResponseMessage<TResult>(result, response.StatusCode);
        }

        public static async Task<ResponseMessage<TResult>> ReadErrorAsync<TResult>(HttpResponseMessage response)
        {
            string content = string.Empty;
            try
            {
                content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new ResponseMessage<TResult>(
                    new HttpResponseException(((int)response.StatusCode).ToString(), ex.Message),
                    response.StatusCode);
            }

            int statusCode = (int)response.StatusCode;
            var exceptionMessages = new List<HttpResponseException>();

            try
            {
                using JsonDocument doc = JsonDocument.Parse(content);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("errors", out JsonElement errorTokens))
                {
                    exceptionMessages.Add(new HttpResponseException(statusCode.ToString(), content));
                    return new ResponseMessage<TResult>(exceptionMessages, response.StatusCode);
                }

                ParseElement("errors", errorTokens, exceptionMessages, statusCode);

                return new ResponseMessage<TResult>(exceptionMessages, response.StatusCode);
            }
            catch (JsonException ex)
            {
                exceptionMessages.Add(new HttpResponseException(
                    statusCode.ToString(),
                    $"Failed to parse error response: {ex.Message}. Raw content: {content}"));
                return new ResponseMessage<TResult>(exceptionMessages, response.StatusCode);
            }
        }

        public static void ParseElement(string key, JsonElement element, List<HttpResponseException> exceptionsOutput, int statusCode)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        ParseElement(key, item, exceptionsOutput, statusCode);
                    }
                    break;

                case JsonValueKind.Object:
                    if (element.TryGetProperty("code", out JsonElement codeProp) &&
                        element.TryGetProperty("message", out JsonElement messageProp))
                    {
                        string codeVal = codeProp.ValueKind == JsonValueKind.Null ? statusCode.ToString() : codeProp.GetString() ?? statusCode.ToString();
                        string messageVal = messageProp.ValueKind == JsonValueKind.Null ? "<null>" : messageProp.GetString() ?? element.GetRawText();

                        exceptionsOutput.Add(new HttpResponseException(codeVal, messageVal));
                    }
                    else
                    {
                        exceptionsOutput.Add(new HttpResponseException(key, element.GetRawText()));
                    }
                    break;

                default:
                    string text = element.ValueKind == JsonValueKind.Null ? "<null>" : element.ToString();
                    exceptionsOutput.Add(new HttpResponseException(statusCode.ToString(), text));
                    break;
            }
        }

        public async Task<HttpResponseModel> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
                var httpResponseModel = new HttpResponseModel
                {
                    Content = await response.Content.ReadAsStringAsync(cancellationToken),
                    StatusCode = response.StatusCode
                };
                return httpResponseModel;
            }
            catch (Exception ex)
            {
                var httpResponseModel = new HttpResponseModel
                {
                    Content = await new StringContent(JsonSerializer.Serialize(new
                    {
                        detail = ex.Message,
                    })).ReadAsStringAsync(cancellationToken),
                    StatusCode = System.Net.HttpStatusCode.InternalServerError
                };
                return httpResponseModel;
            }
        }
    }
}
