var builder = DistributedApplication.CreateBuilder(args);

// Stable SA password so the persisted data volume survives Aspire restarts.
// Set via user secrets:  dotnet user-secrets set Parameters:sqlPassword "<strong password>"
var sqlPassword = builder.AddParameter("sqlPassword", secret: true);

var sql = builder.AddSqlServer("sql", password: sqlPassword)
    .WithDataVolume()                              // persist DB files in a named Docker volume
    .WithLifetime(ContainerLifetime.Persistent)    // keep the container running between Aspire app runs
    .AddDatabase("SIDatabase");

builder.AddProject<Projects.WebApp>("webapp")
    .WithReference(sql)
    .WaitFor(sql);

builder.Build().Run();
