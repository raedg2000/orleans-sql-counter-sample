using Azure.Data.Tables;
using Orleans.Configuration;
using OrleansSilos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration["Orleans:AzureTableConnectionString"]
    ?? "UseDevelopmentStorage=true";

var sqlConnectionString = builder.Configuration.GetConnectionString("SqlServer")
    ?? "Server=(localdb)\\mssqllocaldb;Database=OrleansGrainState;Trusted_Connection=True;";

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = builder.Configuration["Orleans:ClusterId"] ?? "MyOrleansCluster";
            options.ServiceId = builder.Configuration["Orleans:ServiceId"] ?? "MyOrleansService";
        })
        .UseAzureStorageClustering(options =>
            options.TableServiceClient = new TableServiceClient(connectionString))
        .AddAzureTableGrainStorage("Default", options =>
            options.TableServiceClient = new TableServiceClient(connectionString))
        .AddSqlServerGrainStorage("counterStore", options =>
            options.ConnectionString = sqlConnectionString);
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
