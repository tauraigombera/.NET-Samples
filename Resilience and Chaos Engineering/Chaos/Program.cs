using Chaos;
using Polly;
using Polly.Simmy;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var httpClientBuilder = builder.Services.AddHttpClient<TodosClient>(client => client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com"));

//injecting chaos
httpClientBuilder.AddResilienceHandler("chaos", (ResiliencePipelineBuilder<HttpResponseMessage> builder) => 
{
    // Set the chaos injection rate to 5%
    const double InjectionRate = 0.05;

    builder
        .AddChaosLatency(InjectionRate, TimeSpan.FromSeconds(5)) // Add latency to simulate network delays
        .AddChaosFault(InjectionRate, () => new InvalidOperationException("Chaos strategy injection!")) // Inject faults to simulate system errors
        .AddChaosOutcome(InjectionRate, () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)); // Simulate server errors
});

//run the app
var app = builder.Build();
app.MapGet("/", (TodosClient client, CancellationToken cancellationToken) => client.GetTodosAsync(cancellationToken));
app.Run();