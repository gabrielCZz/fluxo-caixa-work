using Microsoft.JSInterop;

namespace FluxoCaixa.Blazor.Services;

public sealed class LocalStorageService
{
    private readonly IJSRuntime _jsRuntime;

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public ValueTask SetAsync(string key, string value)
    {
        return _jsRuntime.InvokeVoidAsync("storage.set", key, value);
    }

    public ValueTask<string?> GetAsync(string key)
    {
        return _jsRuntime.InvokeAsync<string?>("storage.get", key);
    }

    public ValueTask RemoveAsync(string key)
    {
        return _jsRuntime.InvokeVoidAsync("storage.remove", key);
    }
}
