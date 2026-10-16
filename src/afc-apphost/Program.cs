var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Afc_Api>("api")
    .WithEndpoint("http", endpoint => endpoint.Port = 5181);
builder.AddProject<Projects.Afc_Web_Host>("web")
    .WithEndpoint("http", endpoint => endpoint.Port = 5182)
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
