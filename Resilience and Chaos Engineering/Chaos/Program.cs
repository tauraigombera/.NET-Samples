using Chaos;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var httpClientBuilder = builder.Services.AddHttpClient<TodosClient>(client => client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com"));

var app = builder.Build();
app.MapGet("/", (TodosClient client, CancellationToken cancellationToken) => client.GetTodosAsync(cancellationToken));
app.Run();