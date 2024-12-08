using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public LoginController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] UserLoginInput input)
        {
            var databaseServerUrl = "http://your-database-server:port/login"; // Replace with your actual database server URL

            try
            {
                // Send login credentials to the database server
                var response = await _httpClient.PostAsJsonAsync(databaseServerUrl, input);

                if (response.IsSuccessStatusCode)
                {
                    // Parse the response from the database server
                    var serverResponse = await response.Content.ReadAsStringAsync();
                    var tokens = JsonSerializer.Deserialize<TokenResponse>(serverResponse);

                    // Return the tokens to the frontend
                    return Ok(new
                    {
                        Message = "Login successful",
                        AccessToken = tokens.AccessToken,
                        RefreshToken = tokens.RefreshToken
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

    // Input model for login
    public class UserLoginInput
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    // Token response model
    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
