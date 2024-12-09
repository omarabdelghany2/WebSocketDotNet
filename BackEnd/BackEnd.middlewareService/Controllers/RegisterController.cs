using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;


namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public RegisterController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost]
        public async Task<IActionResult> register([FromBody] UserRegistrationInput input)
        {
            // Log the received input
            Console.WriteLine($"Received Input: {JsonSerializer.Serialize(input)}");

            // Database server URL
            var databaseServerUrl = "http://192.168.1.74:8000/api/register/";

            try
            {
                // Log the request body being sent to the database
                var jsonInput = JsonSerializer.Serialize(input);
                Console.WriteLine($"Sending to database: {jsonInput}");

                // Manually create the content to ensure it's serialized correctly
                var content = new StringContent(jsonInput, Encoding.UTF8, "application/json");

                // Alternatively, you could set the content type header explicitly:
                // content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                // Send input to the database server
                var databaseResponse = await _httpClient.PostAsync(databaseServerUrl, content);

                if (databaseResponse.IsSuccessStatusCode)
                {
                    // Read the database server's response
                    var result = await databaseResponse.Content.ReadAsStringAsync();
                    return Ok(new { Message = "Registration successful", Details = result });
                }
                else
                {
                    // Capture and return the error response
                    var error = await databaseResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Database server error: {error}");
                    return StatusCode((int)databaseResponse.StatusCode, new { Message = "Database server error", Error = error });
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP communication errors
                return StatusCode(500, new { Message = "Error communicating with the database server", Exception = ex.Message });
            }
        }
    }

    public class UserRegistrationInput
    {
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("profile_name")]
        public string ProfileName { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }
    }
}
