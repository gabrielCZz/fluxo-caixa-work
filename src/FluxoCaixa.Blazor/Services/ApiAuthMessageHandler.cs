using System.Net.Http.Headers;

namespace FluxoCaixa.Blazor.Services;

public sealed class ApiAuthMessageHandler : DelegatingHandler
{
    private readonly AuthService _authService;

    public ApiAuthMessageHandler(AuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _authService.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var refreshed = await _authService.RefreshAsync();
            if (refreshed)
            {
                var newToken = await _authService.GetAccessTokenAsync();
                if (!string.IsNullOrWhiteSpace(newToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                    response = await base.SendAsync(request, cancellationToken);
                }
            }
        }

        return response;
    }
}
