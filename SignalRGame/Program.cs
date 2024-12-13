


using SignalRGame.Hubs;    // Add the correct namespace for GameHub
using SignalRGame.Services; // Add the correct namespace for GameService

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register services (Replace ConfigureServices method with inline registrations)
builder.Services.AddSignalR();
builder.Services.AddSingleton<GameService>();  // Register GameService
builder.Services.AddSingleton<GameHub>();      // Register GameHub

var app = builder.Build();

// Enable serving static files (HTML, CSS, JS, images, etc.)
app.UseStaticFiles();

// Map the SignalR hub
app.MapHub<GameHub>("/gamehub");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.Run();

