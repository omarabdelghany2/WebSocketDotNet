using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BackEnd.middlewareService.Services;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers; // Added for MediaTypeHeaderValue
using System.Text.Json.Serialization;



namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/")]
    public class AddRemoveFriendsController : ControllerBase
    {
        private readonly FriendsService _friendsService;
        private readonly userIdFromTokenService _userIdFromTokenService;
        public AddRemoveFriendsController(FriendsService friendsService, userIdFromTokenService userIdFromToken)
        {
           _friendsService = friendsService;
           _userIdFromTokenService=userIdFromToken;
        }

        [HttpPost("add-friend")]
        public async Task<IActionResult> addFriend([FromHeader] string Authorization,[FromBody] friendDATA user )
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest(" refresh Token is required.");
            }
            var token = Authorization.Substring("Bearer ".Length).Trim();

            // Call the DataBase to get the userId from userName
            int? user_ID = await _userIdFromTokenService.GetUserIdFromProfileNameAsync(token, user.profile_name);
            if (!user_ID.HasValue)
            {
                return BadRequest(new { Message = "Invalid profile name or user not found." });
            }
            //call the Database to save the Pending Request of AddFRIEND
            bool result=await _friendsService.addFriendAsync(token,user_ID.Value);

            //call the Wedsocket to send that add friend INVITATION
            //now its a valid one 
            return Ok(new
                {
                    Message = "Friends request sent Succesfully",
                });

        }


        [HttpPost("remove-friend")]
        public async Task<IActionResult> removeFriend([FromHeader] string Authorization, [FromBody] friendDATA user )
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest(" refresh Token is required.");
            }
            var token = Authorization.Substring("Bearer ".Length).Trim();

            int? user_ID = await _userIdFromTokenService.GetUserIdFromProfileNameAsync(token, user.profile_name);
            if (!user_ID.HasValue)
            {
                return BadRequest(new { Message = "Invalid profile name or user not found." });
            }

            bool result = await _friendsService.remFriendAsync(token, user_ID.Value);


            if (result)
            {
                // Return 204 No Content for successful deletion
                return NoContent();
            }
            return NotFound(new { Message = "Friendship not found or could not be removed." });

        }

        [HttpPost("decline-Pending-friend")]
        public async Task<IActionResult> declinePendingRequest([FromHeader] string Authorization, [FromBody] friendDATA user )
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest(" refresh Token is required.");
            }
            var token = Authorization.Substring("Bearer ".Length).Trim();

            int? user_ID = await _userIdFromTokenService.GetUserIdFromProfileNameAsync(token, user.profile_name);
            if (!user_ID.HasValue)
            {
                return BadRequest(new { Message = "Invalid profile name or user not found." });
            }

            bool result = await _friendsService.declinePendingRequestAsync(token, user_ID.Value);


            if (result)
            {
                // Return 204 No Content for successful deletion
                return NoContent();
            }
            return NotFound(new { Message = "Pending Request not found or could not be removed." });

        }



        [HttpGet("get-pending-requests")]
        public async Task<IActionResult> removeFriend([FromHeader] string Authorization )
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest(" refresh Token is required.");
            }
            var token = Authorization.Substring("Bearer ".Length).Trim();


            //call the Database to Delete the friendship
            string result=await _friendsService.getPendingFriendRequestAsync(token);

            var friendsList = JsonSerializer.Deserialize<object>(result);

            return Ok(new
            {
                    Message = "Friends penging list fetched successfully",
                    Data = friendsList
            });

            return Ok("valid");

        }





        [HttpPost("accept-friend")]
        public async Task<IActionResult> acceptFriendAsync([FromHeader] string Authorization, [FromBody] friendDATA user )
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest(" refresh Token is required.");
            }
            var token = Authorization.Substring("Bearer ".Length).Trim();

            int? user_ID = await _userIdFromTokenService.GetUserIdFromProfileNameAsync(token, user.profile_name);
            if (!user_ID.HasValue)
            {
                return BadRequest(new { Message = "Invalid profile name or user not found." });
            }

            //call the Database to Delete the friendship
            bool result=await _friendsService.acceptFriendAsync(token,user_ID.Value);

                return Ok(new
                {
                    Message = "Friendship request accepted Succesfully",
                });

        }



        public class RefreshFromAccessRequest(){

            public string refreshToken{ get; set; }
        }

        private class TokenResponse
        {
            [JsonPropertyName("access_token")]  // Mapping snake_case to PascalCase
            public string AccessToken { get; set; }

            [JsonPropertyName("refresh_token")]  // Mapping snake_case to PascalCase
            public string RefreshToken { get; set; }
        }


        public class friendDATA
        { 
            public string? profile_name { get; set; }
        }

    }
}
