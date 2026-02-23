using MockS3.Routing;
using MockS3.Storage;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<InMemoryS3Storage>();
var app = builder.Build();
S3RequestRouter.Register(app);
app.Run();
