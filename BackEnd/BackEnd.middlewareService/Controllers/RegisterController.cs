using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DnsClient;
using System.Linq;

namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/register")]
    public class registerController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public registerController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost]
        public async Task<IActionResult> register([FromBody] UserRegistrationInput input)
        {
            // Log the received input
            Console.WriteLine($"Received Input: {JsonSerializer.Serialize(input)}");

            // Validate email format and supported domains
            var emailPattern = @"^[a-zA-Z0-9._%+-]+@(gmail\.com|(outlook|hotmail|live)\.[a-z.]+|office365\.com)$";

            if (string.IsNullOrWhiteSpace(input.Email) || 
                !Regex.IsMatch(input.Email, emailPattern, RegexOptions.IgnoreCase))
            {
                return BadRequest(new { Message = "Invalid email. Please provide a valid Gmail, Outlook, Hotmail, Live, or Office365 address." });
            }

            var databaseServerUrl = "http://localhost:8004/api/user/auth/register/";

            try
            {
                var jsonInput = JsonSerializer.Serialize(input);
                var content = new StringContent(jsonInput, Encoding.UTF8, "application/json");

                var databaseResponse = await _httpClient.PostAsync(databaseServerUrl, content);

                if (databaseResponse.IsSuccessStatusCode)
                {
                    var result = await databaseResponse.Content.ReadAsStringAsync();
                    return Ok(new { Message = "Registration successful", Details = result });
                }
                else
                {
                    var error = await databaseResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Database server error: {error}");
                    return StatusCode((int)databaseResponse.StatusCode, new { Message = "Database server error", Error = error });
                }
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new { Message = "Error communicating with the database server", Exception = ex.Message });
            }
        }

            // MX record checker for domain
    private async Task<bool> DomainHasMxRecords(string domain)
    {
            try
            {
                var lookup = new LookupClient();
                var result = await lookup.QueryAsync(domain, QueryType.MX);
                return result.Answers.MxRecords().Any();
            }
            catch
            {
                return false;
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
