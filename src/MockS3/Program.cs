using MockS3.Storage;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<InMemoryS3Storage>();
var app = builder.Build();
app.Run();
