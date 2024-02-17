using Polly;

namespace Chaos;
internal class ChaosManager(IWebHostEnvironment environment, IHttpContextAccessor contextAccessor) : IChaosManager
{
    private const string UserQueryParam = "user";

    private const string TestUser = "test";

    public ValueTask<bool> IsChaosEnabledAsync(ResilienceContext context)
    {
        if (environment.IsDevelopment())
        {
            return ValueTask.FromResult(true);
        }

        // This condition serves as a demonstration and is not advisable for use in actual applications.
        if (environment.IsProduction() &&
            contextAccessor.HttpContext is {} httpContext && 
            httpContext.Request.Query.TryGetValue(UserQueryParam, out var values) &&
            values == TestUser)
        {
            // Enable chaos mode for the 'test' user, even within the production environment. 
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    public ValueTask<double> GetInjectionRateAsync(ResilienceContext context)
    {
        if (environment.IsDevelopment())
        {
            return ValueTask.FromResult(0.05);
        }

        if (environment.IsProduction())
        {
            return ValueTask.FromResult(0.03);
        }

        return ValueTask.FromResult(0.0);
    }
}