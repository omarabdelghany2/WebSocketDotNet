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
    public class authenticationController : ControllerBase
    {
        private readonly TokenValidator  _TokenValidator;
        public authenticationController(TokenValidator tokenvalid)
        {
           _TokenValidator =tokenvalid;
        }

        [HttpGet("token-valid")]
        public async Task<IActionResult> checkTokenValidity([FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest(" refresh Token is required.");
            }
            var token = Authorization.Substring("Bearer ".Length).Trim();

            // Call the GetUserProfileAsync function to fetch the friends list
            string result = await _TokenValidator.ValidateTokenAsync(token);

            if (result == "error")
            {
                 return Unauthorized("You are not authorized to get the accessToken list.");
            }
            //now its a valid one 
            return Ok("valid");

        }


        [HttpPost("refresh-token")]
        public async Task<IActionResult> GetAccessTokenByRefresh([FromBody] RefreshFromAccessRequest request)
        {
            if (string.IsNullOrEmpty(request.refreshToken))
            {
                return BadRequest(" refresh Token is required.");
            }

            string result = await _TokenValidator.ValidateTokenAsync(request.refreshToken);

            if (result == "error")
            {
                 return Unauthorized("You are not authorized to get the accessToken list.");
            }

            string resultFrom=await _TokenValidator.GetAccessTokenByRefresh(request.refreshToken);

            try
            {
                var tokens = JsonSerializer.Deserialize<TokenResponse>(resultFrom);
                

                return Ok(new
                {
                    Message = "Tokens Are Got Succefully",
                    AccessToken = tokens?.access,
                    RefreshToken = tokens?.refresh
                });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { Message = "Error parsing Token JSON", Exception = ex.Message });
            }

        }





        public class RefreshFromAccessRequest(){

            public string refreshToken{ get; set; }
        }

        private class TokenResponse
        {
            
            public string access { get; set; }

            public string refresh { get; set; }
        }

    }
}
