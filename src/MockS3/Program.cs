using MockS3.Routing;
using MockS3.Storage;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<InMemoryS3Storage>();
builder.Services.AddSingleton<S3RequestRouter>();
var app = builder.Build();
app.Services.GetRequiredService<S3RequestRouter>().Register(app);
app.Run();

public partial class Program { }
