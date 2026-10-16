var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Afc_Api>("api")
    .WithHttpEndpoint(port: 5181);
builder.AddProject<Projects.Afc_Web_Host>("web")
    .WithHttpEndpoint(port: 5182)
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
