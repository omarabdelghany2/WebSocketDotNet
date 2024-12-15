using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BackEnd.middlewareService.Services;
using System.Text.Json;

namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FriendsController : ControllerBase
    {
        private readonly FriendsService _friendsService;

        public FriendsController(FriendsService friendsService)
        {
            _friendsService = friendsService;
        }

        [HttpPost] // No need to specify "friends" here
        public async Task<IActionResult> GetFriendsList([FromHeader] string Authorization, [FromBody] int userId)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }

            if (userId <= 0)
            {
                return BadRequest("Invalid userId.");
            }

            // Call the GetFriendsListAsync function to fetch the friends list
            string result = await _friendsService.GetFriendsListAsync(Authorization, userId);

            if (result == "error")
            {
                return BadRequest("Error retrieving friends list.");
            }

            try
            {
                // Parse the JSON response into a more readable format
                var friendsList = JsonSerializer.Deserialize<object>(result);

                return Ok(new
                {
                    Message = "Friends list fetched successfully",
                    Data = friendsList // Return the friends list as parsed JSON
                });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { Message = "Error parsing friends list JSON", Exception = ex.Message });
            }
        }
    }
}
