using Chaos;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Simmy;
using Polly.Simmy.Fault;
using Polly.Simmy.Latency;
using Polly.Simmy.Outcomes;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

//Integrating IChaosManager with IServiceCollection: include IChaosManager in the Dependency Injection (DI) container
services.TryAddSingleton<IChaosManager, ChaosManager>();
services.AddHttpContextAccessor();

var httpClientBuilder = builder.Services.AddHttpClient<TodosClient>(client => client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com"));

//adding standard resilience to handle the chaos
httpClientBuilder.AddStandardResilienceHandler().Configure(options => 
    {
        // Update attempt timeout to 1 second
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);

        // Update circuit breaker to handle transient errors and InvalidOperationException
        options.CircuitBreaker.ShouldHandle = args => args.Outcome switch
        {
            {} outcome when HttpClientResiliencePredicates.IsTransient(outcome) => PredicateResult.True(),
            { Exception: InvalidOperationException } => PredicateResult.True(),
            _ => PredicateResult.False()
        };

        // Update retry strategy to handle transient errors and InvalidOperationException
        options.Retry.ShouldHandle = args => args.Outcome switch
        {
            {} outcome when HttpClientResiliencePredicates.IsTransient(outcome) => PredicateResult.True(),
            { Exception: InvalidOperationException } => PredicateResult.True(),
            _ => PredicateResult.False()
        };
    });

//injecting chaos
httpClientBuilder.AddResilienceHandler("chaos", (builder, context) => 
{
    // Get IChaosManager from dependency injection
    var chaosManager = context.ServiceProvider.GetRequiredService<IChaosManager>();

    builder
        .AddChaosLatency(new ChaosLatencyStrategyOptions
        {
            EnabledGenerator = args => chaosManager.IsChaosEnabledAsync(args.Context),
            InjectionRateGenerator = args => chaosManager.GetInjectionRateAsync(args.Context),
            Latency = TimeSpan.FromSeconds(5)
        })
        .AddChaosFault(new ChaosFaultStrategyOptions
        {
            EnabledGenerator = args => chaosManager.IsChaosEnabledAsync(args.Context),
            InjectionRateGenerator = args => chaosManager.GetInjectionRateAsync(args.Context),
            FaultGenerator = new FaultGenerator().AddException(() => new InvalidOperationException("Chaos strategy injection!"))
        })
        .AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>
        {
            EnabledGenerator = args => chaosManager.IsChaosEnabledAsync(args.Context),
            InjectionRateGenerator = args => chaosManager.GetInjectionRateAsync(args.Context),
            OutcomeGenerator = new OutcomeGenerator<HttpResponseMessage>().AddResult(() => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError))
        });           
});

//run the app
var app = builder.Build();
app.MapGet("/", (TodosClient client, CancellationToken cancellationToken) => client.GetTodosAsync(cancellationToken));
app.Run();