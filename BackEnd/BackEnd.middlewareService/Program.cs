using BackEnd.middlewareService.Services;

using Microsoft.Extensions.FileProviders;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpClient(); // Register HttpClient
builder.Services.AddHttpClient<TokenValidator>();
builder.Services.AddHttpClient<FriendsService>();
builder.Services.AddHttpClient<leaderBoardSerivce>();
builder.Services.AddHttpClient<userProfileService>();
builder.Services.AddHttpClient<userScoreService>();
builder.Services.AddHttpClient<userIdFromTokenService>();
builder.Services.AddHttpClient<ForgetPsswordService>();
builder.Services.AddHttpClient<getSubCategoriesService>();
builder.Services.AddHttpClient<numberOfUsersFromTokenService>();
builder.Services.AddHttpClient<getMonthsSubscriptionsService>();
builder.Services.AddHttpClient<paypalDatabaseServices>();
builder.Services.AddHttpClient<numberOfSubscriptionsFromTokenService>();
builder.Services.AddHttpClient<insertQuestionsSerivce>();

















builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(System.Net.IPAddress.Any, 5038);
});

// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenAnyIP(443, o => o.UseHttps()); // This configures HTTPS on port 5050
// });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()  // Allows all origins
                  .AllowAnyHeader()  // Allows all headers
                  .AllowAnyMethod(); // Allows all HTTP methods (GET, POST, PUT, DELETE, etc.)
        });
});


var app = builder.Build();


// Enable serving static files
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unhandled Exception: {ex.Message}");
        throw;
    }
});
// app.Use(async (context, next) =>
// {
//     // Log incoming request headers and body
//     context.Request.EnableBuffering(); // Allow re-reading the request body
//     using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
//     {
//         var body = await reader.ReadToEndAsync();
//         Console.WriteLine($"Incoming Request: {context.Request.Method} {context.Request.Path}");
//         Console.WriteLine($"Headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
//         Console.WriteLine($"Body: {body}");
//         context.Request.Body.Position = 0; // Reset stream position for next middleware
//     }

//     await next();
// });


// app.Use(async (context, next) =>
// {
//     context.Request.EnableBuffering(); // Enable rewinding the request body
//     await next();
// });


// Serve the "Images" folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Images")),
    RequestPath = "/Images"
});

// Serve the "Avatars" folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Avatars")),
    RequestPath = "/Avatars"
});

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// app.UseHttpsRedirection();
// app.UseAuthorization();
// app.MapControllers();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}