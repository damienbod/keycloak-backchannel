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
    .WithLifetime(ContainerLifetime.Persistent)
    .RunKeycloakWithHttpsDevCertificate(port: 8081);

var cache = builder.AddRedis("cache", 6379)
    .WithDataVolume();

var mvcpar = builder.AddProject<Projects.MvcPar>("mvcpar")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak)
    .WithReference(cache)
    .WaitFor(keycloak)
    .WaitFor(cache);

var mvcbackchanneltwo = builder.AddProject<Projects.MvcBackChannelTwo>("mvcbackchanneltwo")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak)
    .WithReference(cache);

builder.AddProject<Projects.AngularBff>("angularbff")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WaitFor(cache);

builder.AddProject<Projects.RazorPagePar>("razorpagepar")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak)
    .WaitFor(keycloak);

var elasticsearch = builder.AddElasticsearch("elasticsearch", password: passwordElastic)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .RunElasticWithHttpsDevCertificate(port: 9200);

builder.AddProject<Projects.ElasticsearchAuditTrail>("elasticsearchaudittrail")
    .WithExternalHttpEndpoints()
    .WithReference(elasticsearch)
    .WaitFor(keycloak);

var dpopapi = builder.AddProject<Projects.DPoPApi>("dpopapi")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak);

builder.AddProject<Projects.DPoPClient>("dpopclient")
    .WithExternalHttpEndpoints()
    .WithReference(dpopapi)
    .WithReference(keycloak);

builder.Build().Run();
