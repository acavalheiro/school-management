var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()       // persist data across Docker restarts
    .WithPgAdmin();         // pgAdmin UI at a random port

// First argument is the Aspire resource name — it becomes the connection string
// key, so it must stay "DefaultConnection" to match appsettings.json. The second
// is the actual PostgreSQL database name.
var atlDb = postgres.AddDatabase("DefaultConnection", "school-management");

builder.AddProject<Projects.Api>("api")
    .WithReference(atlDb)
    .WaitFor(atlDb);        // don't start API until Postgres is ready

builder.Build().Run();
