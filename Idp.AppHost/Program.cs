var builder = DistributedApplication.CreateBuilder(args);

var userName = builder.AddParameter("userName");
var password = builder.AddParameter("password", secret: true);

var keycloak = builder.AddKeycloakContainer("keycloak",
            userName: userName, password: password, port: 8080)
    .WithArgs("--features=preview")
    // for more details regarding disable-trust-manager see https://www.keycloak.org/server/outgoinghttp#_client_configuration_command
    // IMPORTANT: use this command ONLY in local development environment!
    .WithArgs("--spi-connections-http-client-default-disable-trust-manager=true")
    .WithDataVolume()
    .RunWithHttpsDevCertificate(port: 8081);

builder.AddProject<Projects.RazorPagePar>("razorpagepar")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak);

var mvcpar = builder.AddProject<Projects.MvcPar>("mvcpar")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak);

builder.AddProject<Projects.AngularBff>("angularbff")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak);

var mvcbackchanneltwo = builder.AddProject<Projects.MvcBackChannelTwo>("mvcbackchanneltwo")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak);

builder.Build().Run();
