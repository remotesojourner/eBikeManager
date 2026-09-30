using EBikeManager.Application.Enums;
using EBikeManager.Application.Services.Interfaces;

namespace EBikeManager.Web.Services;

public sealed class CurrentAccessService : ICurrentAccessService
{
    private readonly CircuitAccessService _circuit;
    private readonly HttpCurrentAccessService _http;

    public CurrentAccessService(CircuitAccessService circuit, HttpCurrentAccessService http)
    {
        _circuit = circuit;
        _http = http;
    }

    public Access Level => _circuit.Started ? _circuit.Level : _http.Level;

    public bool HasFullAccess => Level == Access.Full;
}
