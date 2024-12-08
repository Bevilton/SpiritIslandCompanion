using WebApp;

var builder = WebApplication.CreateBuilder(args);
var startup = new Startup(builder);

startup.ConfigureServices();

var app = builder.Build();

startup.Configure(app);

app.Run();