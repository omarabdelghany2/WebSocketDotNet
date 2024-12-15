using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BackEnd.middlewareService.Services;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers; // Added for MediaTypeHeaderValue


namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class userProfileController : ControllerBase
    {
        private readonly userProfileService _userProfileService;

        public userProfileController(userProfileService userprofile)
        {
            _userProfileService = userprofile;
        }

        [HttpGet("userProfile")]
        public async Task<IActionResult> GetUserProfile([FromHeader] string Authorization, [FromQuery] int userId)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }

            if (userId <= 0)
            {
                return BadRequest("Invalid userId.");
            }

            // Call the GetUserProfileAsync function to fetch the friends list
            string result = await _userProfileService.GetUserProfileAsync(Authorization, userId);

            if (result == "error")
            {
                return BadRequest("Error retrieving friends list.");
            }

            try
            {
                // Parse the JSON response into a more readable format
                var profile = JsonSerializer.Deserialize<object>(result);

                return Ok(new
                {
                    Message = "profile fetched successfully",
                    Data = profile // Return the friends list as parsed JSON
                });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { Message = "Error parsing profile JSON", Exception = ex.Message });
            }
        }
    }
}