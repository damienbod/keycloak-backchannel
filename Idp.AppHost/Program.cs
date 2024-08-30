using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var userName = builder.AddParameter("userName");
var password = builder.AddParameter("password", secret: true);

var keycloak = builder.AddKeycloakContainer("keycloak", userName: userName, password: password, port: 8081)
    .WithDataVolume();

var apiService = builder.AddProject<Projects.Idp_ApiService>("apiservice")
    .WithReference(keycloak);

builder.AddProject<Projects.Idp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak)
    .WithReference(apiService);

builder.Build().Run();
