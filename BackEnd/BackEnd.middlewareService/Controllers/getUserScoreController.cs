using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BackEnd.middlewareService.Services;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers; // Added for MediaTypeHeaderValue


namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/user-score")]
    public class userScoreController : ControllerBase
    {
        private readonly userScoreService _userScoreService;

        public userScoreController(userScoreService userScore)
        {
            _userScoreService = userScore;
        }

        [HttpPost]
        public async Task<IActionResult> GetUserScore([FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }
            // Remove the "Bearer " prefix if it exists
            string token = Authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? Authorization.Substring("Bearer ".Length).Trim()
                : Authorization;


            // Call the GetUserProfileAsync function to fetch the friends list
            string result = await _userScoreService.GetUserScoreAsync(token);

            if (result == "error")
            {
                return BadRequest("Error retrieving friends list.");
            }

            try
            {
                // Parse the JSON response into a more readable format
                var Score = JsonSerializer.Deserialize<object>(result);

                return Ok(new
                {
                    Message = "Score fetched successfully",
                    Data = Score // Return the friends list as parsed JSON
                });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { Message = "Error parsing profile JSON", Exception = ex.Message });
            }
        }
    }
}
