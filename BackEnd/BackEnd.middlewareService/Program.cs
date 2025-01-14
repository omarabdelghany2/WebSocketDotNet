using BackEnd.middlewareService.Services;


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










builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(System.Net.IPAddress.Any, 5038);
});


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