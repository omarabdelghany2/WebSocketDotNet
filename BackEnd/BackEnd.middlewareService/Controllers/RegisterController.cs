using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Register([FromBody] UserRegistrationInput input)
        {
            var databaseServerUrl = "http://your-database-server:port/register";

            try
            {
                var response = await _httpClient.PostAsJsonAsync(databaseServerUrl, input);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return Ok(new { Message = "Registration successful", Details = result });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { Message = "Database server error", Error = error });
                }
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new { Message = "Error communicating with the database server", Exception = ex.Message });
            }
        }
    }

    public class UserRegistrationInput
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
    }
}
