var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Idp_ApiService>("apiservice");

builder.AddProject<Projects.Idp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
