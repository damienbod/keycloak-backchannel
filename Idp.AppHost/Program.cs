using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var userName = builder.AddParameter("userName");
var passwordKeycloak = builder.AddParameter("passwordKeycloak", secret: true);
var passwordElastic = builder.AddParameter("passwordElastic", secret: true);

var keycloak = builder.AddKeycloakContainer("keycloak",
            userName: userName, password: passwordKeycloak, port: 8080)
    .WithArgs("--features=preview")
    // for more details regarding disable-trust-manager see https://www.keycloak.org/server/outgoinghttp#_client_configuration_command
    // IMPORTANT: use this command ONLY in local development environment!
    .WithArgs("--spi-connections-http-client-default-disable-trust-manager=true")
    .WithDataVolume()
    .RunWithHttpsDevCertificate(port: 8081);

var cache = builder.AddRedis("cache", 6379)
    .WithDataVolume();

var mvcpar = builder.AddProject<Projects.MvcPar>("mvcpar")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak)
    .WithReference(cache);

var mvcbackchanneltwo = builder.AddProject<Projects.MvcBackChannelTwo>("mvcbackchanneltwo")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak)
    .WithReference(cache);

builder.AddProject<Projects.AngularBff>("angularbff")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak);

var elasticsearch = builder.AddElasticsearch("elasticsearch", password: passwordElastic)
    .WithEnvironment("xpack.security.http.ssl.enabled", "true")
    .WithEnvironment("xpack.security.http.ssl.keystore.path", "http_ca.crt")
    .WithHttpsEndpoint(port: 9200, targetPort: 9200)
    // either [xpack.security.http.ssl.keystore.path],
    // or both [xpack.security.http.ssl.key] and [xpack.security.http.ssl.certificate]"
    .WithDataVolume();

builder.AddProject<Projects.RazorPagePar>("razorpagepar")
    .WithExternalHttpEndpoints()
    .WithReference(elasticsearch)
    .WithReference(keycloak);

builder.Build().Run();
