using Azure.Data.Tables;
using Orleans.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration["Orleans:AzureTableConnectionString"]
    ?? "UseDevelopmentStorage=true";

builder.Host.UseOrleansClient(clientBuilder =>
{
    clientBuilder
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = builder.Configuration["Orleans:ClusterId"] ?? "MyOrleansCluster";
            options.ServiceId = builder.Configuration["Orleans:ServiceId"] ?? "MyOrleansService";
        })
        .UseAzureStorageClustering(options =>
            options.TableServiceClient = new TableServiceClient(connectionString));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
