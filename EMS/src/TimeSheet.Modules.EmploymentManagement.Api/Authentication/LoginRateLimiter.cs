using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace TimeSheet.Modules.EmploymentManagement.Api.Authentication;

public sealed class LoginRateLimiter : IAsyncDisposable
{
    private readonly PartitionedRateLimiter<HttpContext> _rateLimiter;

    public LoginRateLimiter(
        IOptions<LoginRateLimitOptions> options,
        ILoginRateLimitKeyProvider keyProvider)
    {
        var value = options.Value;

        _rateLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: keyProvider.GetPartitionKey(httpContext),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = value.PermitLimit,
                    Window = TimeSpan.FromMinutes(value.WindowMinutes),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = value.QueueLimit,
                    AutoReplenishment = true
                }));
    }

    public ValueTask<RateLimitLease> AcquireAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        return _rateLimiter.AcquireAsync(httpContext, 1, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _rateLimiter.DisposeAsync();
    }
}
