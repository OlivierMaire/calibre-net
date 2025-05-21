using System.Reflection;
using System.Security.Claims;
using Calibre_net.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Calibre_net.Client;

// This is a client-side AuthenticationStateProvider that determines the user's authentication state by
// looking for data persisted in the page when it was rendered on the server. This authentication state will
// be fixed for the lifetime of the WebAssembly application. So, if the user needs to log in or out, a full
// page reload is required.
//
// This only provides a user name and email for display purposes. It does not actually include any tokens
// that authenticate to the server when making subsequent requests. That works separately using a
// cookie that will be included on HttpClient requests to the server.
internal class PersistentAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> defaultUnauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly Task<AuthenticationState> authenticationStateTask = defaultUnauthenticatedTask;
private const string PersistenceKey = $"__internal__{nameof(AuthenticationState)}";
    public PersistentAuthenticationStateProvider(PersistentComponentState state)
    {
 Console.WriteLine("PersistentAuthenticationStateProvider constructor called");

        if (!state.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo) || userInfo is null)
        {
            return;
        }

        var data = state.GetType().GetFields(BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance)
        .ToDictionary
        (
            propInfo => propInfo.Name,
            propInfo => propInfo.GetValue(state)
        );

           if (!state.TryTakeFromJson<AuthenticationStateData>(PersistenceKey, out var stateData) || stateData is null)
        {
 Console.WriteLine($"AuthenticationStateData {PersistenceKey} failed or empty");
            return;
        }
        foreach (var claim in stateData.Claims)
        {
            Console.WriteLine($"stateData Claim: {claim.Type} = {claim.Value}");
        }

      

        Claim[] claims = [
            new Claim(ClaimTypes.NameIdentifier, userInfo.UserId),
            new Claim(ClaimTypes.Name, userInfo.Email),
            new Claim(ClaimTypes.Email, userInfo.Email),
            // new Claim("Permissions", string.Join(",",userInfo.Permissions)) ];
            ..userInfo.Permissions.Select(p =>   new Claim("Permissions", p) ).ToArray(),
            new Claim("preferedlocale", userInfo.PreferredLocale)
            ];


        authenticationStateTask = Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims,
                authenticationType: nameof(PersistentAuthenticationStateProvider)))));

        if (!string.IsNullOrEmpty(userInfo.PreferredLocale) &&
         userInfo.PreferredLocale != Thread.CurrentThread.CurrentCulture.Name)
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(userInfo.PreferredLocale);
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(userInfo.PreferredLocale);
        }
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => authenticationStateTask;
}
