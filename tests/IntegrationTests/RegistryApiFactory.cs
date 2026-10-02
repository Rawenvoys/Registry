using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Registry.Application.Accounts;
using Registry.Infrastructure.Identity;
using Registry.Infrastructure.Identity.External;

namespace Registry.IntegrationTests;

public sealed class RegistryApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"registry-tests-{Guid.NewGuid():N}.db");

    public CapturingEmailSender Emails { get; } = new();

    public FakeTokenValidator Google { get; } = new(ExternalProviders.Google);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Registry", $"Data Source={_databasePath}");
        builder.UseSetting("Authentication:External:Google:ClientIds:0", "test-client");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IEmailSender<User>>(Emails);
            services.AddKeyedSingleton<IExternalTokenValidator>(ExternalProviders.Google, Google);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}

public sealed class CapturingEmailSender : IEmailSender<User>
{
    public ConcurrentDictionary<string, string> ConfirmationLinks { get; } = new();

    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        ConfirmationLinks[email] = confirmationLink;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink) => Task.CompletedTask;

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode) => Task.CompletedTask;
}

/// <summary>Accepts tokens registered by the test, standing in for the real provider.</summary>
public sealed class FakeTokenValidator(string provider) : IExternalTokenValidator
{
    private readonly ConcurrentDictionary<string, ExternalIdentity> _tokens = new();

    public string Issue(string subject, string? email, bool emailVerified = true)
    {
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = new ExternalIdentity(provider, subject, email, emailVerified, "Test User");
        return token;
    }

    public Task<ExternalIdentity?> ValidateAsync(string token, CancellationToken cancellationToken) =>
        Task.FromResult(_tokens.GetValueOrDefault(token));
}
