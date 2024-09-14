var builder = DistributedApplication.CreateBuilder(args);

var userName = builder.AddParameter("userName");
var passwordKeycloak = builder.AddParameter("passwordKeycloak", secret: true);
var passwordElastic = builder.AddParameter("passwordElastic", secret: true);

var keycloak = builder.AddKeycloakContainer("keycloak",
            userName: userName, password: passwordKeycloak, port: 8080)
    .WithArgs("--features=preview")
    // for more details regarding disable-trust-manager
    // see https://www.keycloak.org/server/outgoinghttp#_client_configuration_command
    // IMPORTANT: use this command ONLY in local development environment!
    .WithArgs("--spi-connections-http-client-default-disable-trust-manager=true")
    .WithDataVolume()
    .RunKeycloakWithHttpsDevCertificate(port: 8081);

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

builder.AddProject<Projects.RazorPagePar>("razorpagepar")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak);

var elasticsearch = builder.AddElasticsearch("elasticsearch", password: passwordElastic)
    .WithDataVolume()
    .RunElasticWithHttpsDevCertificate(port: 9200);

builder.AddProject<Projects.ElasticsearchAuditTrail>("elasticsearchaudittrail")
    .WithExternalHttpEndpoints()
    .WithReference(elasticsearch);

builder.Build().Run();
