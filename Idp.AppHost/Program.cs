using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var adminUsername = builder.AddParameter("adminUsername");
var adminPassword = builder.AddParameter("adminPassword", secret: true);

var keycloak = builder.AddKeycloak("keycloak", 8081)
    .WithEnvironment("admin", adminUsername)
    .WithEnvironment("admin", adminPassword)
    .WithDataVolume();

var apiService = builder.AddProject<Projects.Idp_ApiService>("apiservice")
    .WithReference(keycloak);

builder.AddProject<Projects.Idp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak)
    .WithReference(apiService);

builder.Build().Run();
