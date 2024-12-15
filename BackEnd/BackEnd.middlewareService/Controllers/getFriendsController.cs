using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BackEnd.middlewareService.Services;
using System.Text.Json;

namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/friends")]
    public class friendsController : ControllerBase
    {
        private readonly FriendsService _friendsService;

        public friendsController(FriendsService friendsService)
        {
            _friendsService = friendsService;
        }

        [HttpGet] // No need to specify "friends" here
        public async Task<IActionResult> GetFriendsList([FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }

            var token = Authorization.Substring("Bearer ".Length).Trim();

            // Call the GetFriendsListAsync function with the userId from the request
            string result = await _friendsService.GetFriendsListAsync(token);

            if (result == "error")
            {
                return BadRequest("Error retrieving friends list.");
            }

            try
            {
                var friendsList = JsonSerializer.Deserialize<object>(result);

                return Ok(new
                {
                    Message = "Friends list fetched successfully",
                    Data = friendsList
                });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { Message = "Error parsing friends list JSON", Exception = ex.Message });
            }
        }


        public class UserRequest
        {
            public int UserId { get; set; }
        }
}
}
