using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var userName = builder.AddParameter("userName");
var password = builder.AddParameter("password", secret: true);

var keycloak = builder.AddKeycloakContainer("keycloak", userName: userName, password: password, port: 8080)
    .WithArgs("--features=preview")
    .WithDataVolume()
    .RunWithHttpsDevCertificate(port: 8081);

builder.AddProject<Projects.MvcHybridBackChannel>("MvcHybridBackChannel")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak);

builder.AddProject<Projects.MvcHybridBackChannelTwo>("MvcHybridBackChannelTwo")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak);

builder.Build().Run();
