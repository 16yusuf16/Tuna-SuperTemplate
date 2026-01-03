using Tuna.SuperTemplate.HttpClient.Models;

namespace Tuna.SuperTemplate.HttpClient.Interface;

public interface IHttpClient
{
    public Task<ResponseMessage<TResult>> SendAsync<TResult>(HttpRequestMessage requestMessage, HttpClientHandler httpClientHandler = null, CancellationToken cancellationToken = default);
    public Task<HttpResponseModel> SendAsync(HttpRequestMessage requestMessage,  CancellationToken cancellationToken = default);
}
