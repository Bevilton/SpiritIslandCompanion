var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .AddDatabase("SIDatabase");

builder.AddProject<Projects.WebApp>("webapp")
    .WithReference(sql)
    .WaitFor(sql);

builder.Build().Run();
