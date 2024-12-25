
using SignalRGame.Services;

using SignalRGame.Hubs;    // Add the correct namespace for GameHub


var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register services (Replace ConfigureServices method with inline registrations)
builder.Services.AddSignalR();
 // Register GameService
builder.Services.AddSingleton<GameHub>();      // Register GameHub
builder.Services.AddSingleton<getQuestionsService>();
builder.Services.AddSingleton<userIdFromTokenService>();
builder.Services.AddSingleton<userProfileFromTokenService>();
builder.Services.AddSingleton<GameService>(); 
builder.Services.AddSingleton<FriendsService>(); 
builder.Services.AddSingleton<GetFriendsByIdService>(); 

builder.Services.AddHttpClient(); // Register HttpClient



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.SetIsOriginAllowed(_ => true) // Allow any origin
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Required for SignalR
        });
});


var app = builder.Build();
app.UseCors("AllowAll");
app.Urls.Add("http://0.0.0.0:5274");


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

