# OrleansSample1

OrleansSample1 is a small .NET 10 sample that shows how to use Microsoft Orleans with:

- an Orleans silo host
- an ASP.NET Core API that talks to Orleans as a client
- a custom SQL Server grain storage provider
- Azurite for Orleans cluster membership during local development

The sample exposes counters over HTTP. Each counter is an Orleans grain identified by a `Guid`. The current counter value is persisted in SQL Server, and the API forwards HTTP requests to the grain.

For deeper architecture notes, see [Documentation/README.md](Documentation/README.md).

## Projects

| Project | Purpose |
| --- | --- |
| `OrleansGrains` | Shared grain contracts, grain implementation, and counter model |
| `OrleansSilos` | Hosts the Orleans silo and registers storage providers |
| `OrleansCounterAPI` | ASP.NET Core Web API that connects to the Orleans cluster as a client |
| `OrleansClient` | Small console smoke test that calls the API |

## How The Application Works

1. `OrleansSilos` starts the Orleans silo.
2. The silo uses Azure Table Storage for cluster membership. In local development this points to Azurite through `UseDevelopmentStorage=true`.
3. `OrleansCounterAPI` connects to the same Orleans cluster and exposes HTTP endpoints.
4. `CounterController` resolves an `ICounterGrain` by `Guid` and calls `Increment()` or `GetValue()`.
5. `CounterGrain` persists its state through the custom SQL Server storage provider named `counterStore`.

## API Endpoints

By default the API runs on `http://localhost:5131`.

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/counter/{counterId}` | Returns the current value |
| `POST` | `/counter/{counterId}/increment` | Increments the counter and returns the new value |

Example:

```powershell
$counterId = '9ec79b44-967b-4684-87ab-1b3f919054a7'

Invoke-RestMethod -Method Post "http://localhost:5131/counter/$counterId/increment"
Invoke-RestMethod -Method Get "http://localhost:5131/counter/$counterId"
```

## Known Counter IDs

The silo seeds five well-known counters on startup:

| Name | Id |
| --- | --- |
| Counter One | `9ec79b44-967b-4684-87ab-1b3f919054a7` |
| Counter Two | `a0eff5e9-fa0d-471e-9100-c5f9335d6051` |
| Counter Three | `83519673-3e02-4aa2-98f6-e51207d242ef` |
| Counter Four | `1651f451-4519-4732-8029-3b360c0226b2` |
| Counter Five | `0268cc26-4ad4-4cae-8b9f-8379d3db2979` |

## Prerequisites

- .NET 10 SDK
- SQL Server LocalDB, or another reachable SQL Server instance
- Node.js, because `Start-Dev.ps1` starts Azurite through `npx`

Default local configuration:

- SQL connection string: `Server=(localdb)\\mssqllocaldb;Database=OrleansGrainState;Trusted_Connection=True;`
- Orleans clustering storage: `UseDevelopmentStorage=true`

If you want to use a different SQL Server instance, update `ConnectionStrings:SqlServer` in `OrleansSilos/appsettings.json`.

## How To Run

### Option 1: Command line

Open the repo root and run these in separate terminals.

1. Start Azurite:

```powershell
pwsh -File .\Start-Dev.ps1
```

2. Start the silo:

```powershell
dotnet run --project .\OrleansSilos\OrleansSilos.csproj
```

3. Start the API:

```powershell
dotnet run --project .\OrleansCounterAPI\OrleansCounterAPI.csproj
```

4. Optional smoke test:

```powershell
dotnet run --project .\OrleansClient\OrleansClient.csproj
```

The console client calls the API, increments `Counter One`, then fetches the current value again.

### Option 2: Visual Studio

Run `Start-Dev.ps1` first so Azurite is available, then start the solution using the Visual Studio launch profile referenced by the script.

## Required Table

The only application-specific relational table required by the code is `Counter`.

Important:

- You do not need to create this table manually for local development.
- `SqlServerGrainStorage` creates it automatically when the silo starts.
- The silo also seeds five rows for the well-known counter IDs if they do not already exist.

This is the schema created by the code:

```sql
CREATE TABLE Counter
(
    Id         UNIQUEIDENTIFIER NOT NULL,
    Name       NVARCHAR(255)    NOT NULL,
    Value      INT              NULL,
    RowVersion ROWVERSION       NOT NULL,
    CONSTRAINT PK_Counter PRIMARY KEY (Id)
);
```

Typical contents after first startup:

```sql
SELECT Id, Name, Value, RowVersion
FROM Counter
ORDER BY Name;
```

## Notes About Azure Table Storage

The solution also depends on Azure Table Storage for Orleans cluster membership and default grain storage. In development, `Start-Dev.ps1` starts Azurite for this. Those Orleans tables are managed by Orleans and Azurite; there is no extra hand-written table schema in this repository for them.