using System.Security.Claims;
using EBikeManager.Application.Enums;
using EBikeManager.Web.Utils;
using Microsoft.AspNetCore.Components.Authorization;

namespace EBikeManager.Web.Services;

public sealed class CircuitAccessService
{
    private readonly AuthenticationStateProvider _authenticationState;
    private readonly AuthSettingsService _auth;
    private ClaimsPrincipal? _user;

    public CircuitAccessService(AuthenticationStateProvider authenticationState, AuthSettingsService auth)
    {
        _authenticationState = authenticationState;
        _auth = auth;
    }

    public bool Started { get; private set; }

    public Access Level => AccessPolicy.Decide(_auth.IsActive, _user, _auth.Stamp);

    public bool SignedIn => _auth.IsActive && Level == Access.Full;

    public string? UserName => SignedIn ? SignedInUser.DisplayName(_user) : null;

    public async Task StartAsync()
    {
        // Sign-in and sign-out reload the page and create a new circuit.
#pragma warning disable BL0013
        _user = (await _authenticationState.GetAuthenticationStateAsync()).User;
#pragma warning restore BL0013
        Started = true;
    }
}
