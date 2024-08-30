var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder.AddKeycloak("keycloak", 8081)
    .WithDataVolume();

var apiService = builder.AddProject<Projects.Idp_ApiService>("apiservice")
    .WithReference(keycloak);

builder.AddProject<Projects.Idp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak)
    .WithReference(apiService);

builder.Build().Run();
