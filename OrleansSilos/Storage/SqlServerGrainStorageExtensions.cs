using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Orleans.Storage;
using OrleansSilos.Storage;

namespace OrleansSilos;

public static class SqlServerGrainStorageExtensions
{
    public static ISiloBuilder AddSqlServerGrainStorage(
        this ISiloBuilder builder,
        string name,
        Action<SqlServerGrainStorageOptions> configureOptions)
    {
        builder.Services.AddOptions<SqlServerGrainStorageOptions>(name)
            .Configure(configureOptions);

        builder.Services.AddKeyedSingleton<IGrainStorage>(name, (sp, key) =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<SqlServerGrainStorageOptions>>()
                            .Get(key?.ToString() ?? name);
            var logger = sp.GetRequiredService<ILogger<SqlServerGrainStorage>>();
            return new SqlServerGrainStorage(key?.ToString() ?? name, options, logger);
        });

        builder.Services.AddSingleton<ILifecycleParticipant<ISiloLifecycle>>(sp =>
            (ILifecycleParticipant<ISiloLifecycle>)sp.GetRequiredKeyedService<IGrainStorage>(name));

        return builder;
    }
}

