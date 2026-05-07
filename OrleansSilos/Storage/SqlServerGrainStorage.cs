using Microsoft.Data.SqlClient;
using Orleans.Runtime;
using Orleans.Storage;
using OrleansGrains.Model;
using System.Data;

namespace OrleansSilos.Storage;

public sealed class SqlServerGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
{
    private readonly string _name;
    private readonly SqlServerGrainStorageOptions _options;
    private readonly ILogger<SqlServerGrainStorage> _logger;

    private const string CreateTableSql = """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Counter')
        CREATE TABLE Counter
        (
            Id         UNIQUEIDENTIFIER NOT NULL,
            Name       NVARCHAR(255)    NOT NULL,
            Value      INT              NULL,
            RowVersion ROWVERSION       NOT NULL,
            CONSTRAINT PK_Counter PRIMARY KEY (Id)
        );
        """;

    private const string ReadSql = """
        SELECT Id, Name, Value, RowVersion
        FROM Counter
        WHERE Id = @Id;
        """;

    private const string InsertSql = """
        INSERT INTO Counter (Id, Name, Value)
        OUTPUT inserted.RowVersion
        VALUES (@Id, @Name, @Value);
        """;

    private const string UpdateSql = """
        UPDATE Counter
        SET Name = @Name, Value = @Value
        OUTPUT inserted.RowVersion
        WHERE Id = @Id AND RowVersion = @RowVersionExpected;
        """;

    private const string DeleteSql = """
        DELETE FROM Counter WHERE Id = @Id;
        """;

    private const string SeedSql = """
        INSERT INTO Counter (Id, Name, Value)
        SELECT @Id, @Name, 0
        WHERE NOT EXISTS (SELECT 1 FROM Counter WHERE Id = @Id);
        """;

    public SqlServerGrainStorage(
        string name,
        SqlServerGrainStorageOptions options,
        ILogger<SqlServerGrainStorage> logger)
    {
        _name = name;
        _options = options;
        _logger = logger;
    }

    public void Participate(ISiloLifecycle observer) =>
        observer.Subscribe(
            nameof(SqlServerGrainStorage),
            ServiceLifecycleStage.ApplicationServices,
            OnStartAsync);

    private async Task OnStartAsync(CancellationToken ct)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(ct);
        await using var createCmd = new SqlCommand(CreateTableSql, connection);
        await createCmd.ExecuteNonQueryAsync(ct);
        await SeedCountersAsync(connection, ct);
        _logger.LogInformation("SqlServerGrainStorage '{Name}' initialized.", _name);
    }

    private static async Task SeedCountersAsync(SqlConnection connection, CancellationToken ct)
    {
        foreach (var (id, name) in WellKnownCounterIds.All)
        {
            await using var cmd = new SqlCommand(SeedSql, connection);
            cmd.Parameters.AddWithValue("@Id",   id);
            cmd.Parameters.AddWithValue("@Name", name);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var id = GetGrainGuid(grainId);

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new SqlCommand(ReadSql, connection);
        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var counter = new Counter
            {
                Id    = reader.GetGuid(0),
                Name  = reader.GetString(1),
                Value = reader.IsDBNull(2) ? 0 : reader.GetInt32(2)
            };
            grainState.State        = (T)(object)counter;
            grainState.ETag         = Convert.ToHexString((byte[])reader[3]);
            grainState.RecordExists = true;
        }
        else
        {
            grainState.State        = (T)(object)new Counter { Id = id };
            grainState.ETag         = null;
            grainState.RecordExists = false;
        }
    }

    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainState.State is not Counter counter)
            throw new InvalidOperationException(
                $"{nameof(SqlServerGrainStorage)} only supports '{nameof(Counter)}' state.");

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync();

        byte[] newRowVersion;

        if (grainState.ETag is null)
        {
            await using var cmd = new SqlCommand(InsertSql, connection);
            cmd.Parameters.AddWithValue("@Id",    counter.Id == Guid.Empty ? GetGrainGuid(grainId) : counter.Id);
            cmd.Parameters.AddWithValue("@Name",  counter.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@Value", counter.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            newRowVersion = (byte[])reader[0];
        }
        else
        {
            await using var cmd = new SqlCommand(UpdateSql, connection);
            cmd.Parameters.AddWithValue("@Id",    counter.Id);
            cmd.Parameters.AddWithValue("@Name",  counter.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("@Value", counter.Value);
            cmd.Parameters.Add("@RowVersionExpected", SqlDbType.Binary, 8).Value =
                Convert.FromHexString(grainState.ETag);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InconsistentStateException(
                    $"RowVersion mismatch for grain {grainId} state '{stateName}'.",
                    grainState.ETag,
                    "(new)");

            newRowVersion = (byte[])reader[0];
        }

        grainState.ETag         = Convert.ToHexString(newRowVersion);
        grainState.RecordExists = true;
    }

    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var id = GetGrainGuid(grainId);

        await using var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new SqlCommand(DeleteSql, connection);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();

        grainState.State        = (T)(object)new Counter { Id = id };
        grainState.ETag         = null;
        grainState.RecordExists = false;
    }

    private static Guid GetGrainGuid(GrainId grainId) =>
        Guid.Parse(grainId.Key.ToString()!);
}
