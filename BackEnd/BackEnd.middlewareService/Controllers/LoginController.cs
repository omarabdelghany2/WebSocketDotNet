using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using BackEnd.middlewareService.Services;


using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using BackEnd.middlewareService.Services;

namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/login")]
    public class loginController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly TokenValidator _tokenValidator;  //this is how we use TokenValidator 

        public loginController(HttpClient httpClient,   TokenValidator tokenValidator)
        {
            _httpClient = httpClient;
            _tokenValidator = tokenValidator;             //this is how we use TokenValidator 
        }

        [HttpPost]
        public async Task<IActionResult> login([FromBody] UserLoginInput input)
        {
            // Database server URL
            var databaseServerUrl = "http://localhost:8000/api/user/auth/login/"; // Replace with your actual database server URL

            try
            {
                // Log the received input
                Console.WriteLine($"Received Input: {JsonSerializer.Serialize(input)}");

                // Serialize the input to JSON
                var jsonInput = JsonSerializer.Serialize(input);
                Console.WriteLine($"Sending to database: {jsonInput}");

                // Manually create the content to ensure it's serialized correctly
                var content = new StringContent(jsonInput, Encoding.UTF8, "application/json");

                // Send login credentials to the database server
                var response = await _httpClient.PostAsync(databaseServerUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    // Parse the response from the database server
                    var serverResponse = await response.Content.ReadAsStringAsync();
                    var tokens = JsonSerializer.Deserialize<TokenResponse>(serverResponse);

                    // Return the tokens to the frontend
                    return Ok(new
                    {
                        Message = "Login successful",
                        AccessToken = tokens?.AccessToken,
                        RefreshToken = tokens?.RefreshToken,
                        ExpirationDate=tokens?.ExpirationDate
                    });
                }
                else
                {
                    // Handle login failure
                    var error = await response.Content.ReadAsStringAsync();
                    return Unauthorized(new { Message = "Login failed", Error = error });
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP errors
                return StatusCode(500, new { Message = "Error communicating with the database server", Exception = ex.Message });
            }
        }

    }

    // Input model for login with JsonPropertyName attributes
    public class UserLoginInput
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }
    }

        // Token response model
    public class TokenResponse
    {
        [JsonPropertyName("access_token")]  // Mapping snake_case to PascalCase
        public string AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]  // Mapping snake_case to PascalCase
        public string RefreshToken { get; set; }

        [JsonPropertyName("expiration_date")]  // Mapping snake_case to PascalCase
        public string ExpirationDate { get; set; }
    }
}
