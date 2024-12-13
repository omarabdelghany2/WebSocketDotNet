var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register services (Replace ConfigureServices method with inline registrations)
builder.Services.AddSignalR();
builder.Services.AddSingleton<GameService>();


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

