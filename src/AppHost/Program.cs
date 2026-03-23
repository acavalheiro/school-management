var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()       // persist data across Docker restarts
    .WithPgAdmin();         // pgAdmin UI at a random port

var atlDb = postgres.AddDatabase("DefaultConnection");

builder.AddProject<Projects.Api>("api")
    .WithReference(atlDb)
    .WaitFor(atlDb);        // don't start API until Postgres is ready

builder.Build().Run();
