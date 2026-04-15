using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Shrine_ACU_Web_Application.Services;

public sealed class UserSessionAuthenticationProvider : IAuthenticationProvider
{
    private readonly UserSessionService? _userSession;
    private readonly IServiceProvider? _serviceProvider;

    public UserSessionAuthenticationProvider(UserSessionService userSession)
    {
        _userSession = userSession;
    }

    public UserSessionAuthenticationProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        var session = _userSession ?? _serviceProvider?.GetService<UserSessionService>();

        if (session?.IsAuthenticated == true && session.CurrentUser is not null)
        {
            var username = session.CurrentUser.Username;
            var password = session.CurrentUser.Password;

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
                request.Headers.Add("Authorization", $"Basic {credentials}");
            }
        }

        return Task.CompletedTask;
    }
}
