using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var userName = builder.AddParameter("userName");
var password = builder.AddParameter("password", secret: true);

var keycloak = builder.AddKeycloakContainer("keycloak", userName: userName, password: password, port: 8081)
    .WithArgs("--features=preview")
    .WithDataVolume();

builder.AddProject<Projects.MvcHybridBackChannel>("MvcHybridBackChannel")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak);

builder.Build().Run();
