var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(5050))
    .WithPgWeb()
    .WithDataVolume(isReadOnly: false);

var postgresdb = postgres.AddDatabase("postgresdb");

var apiService = builder
    .AddProject<Projects.Aspire_ApiService>("apiservice")
    .WithReference(postgresdb)
    .WaitFor(postgresdb)
    .WaitFor(postgres);

builder
    .AddProject<Projects.Aspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
