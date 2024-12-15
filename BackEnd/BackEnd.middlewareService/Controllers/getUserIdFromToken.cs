using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BackEnd.middlewareService.Services;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers; // Added for MediaTypeHeaderValue


namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/")]
    public class getUserIdFromToken : ControllerBase
    {
        private readonly userIdFromTokenService _userIdFromTokenService;

        public getUserIdFromToken(userIdFromTokenService userid)
        {
            _userIdFromTokenService = userid;
        }
        [HttpGet("user-id")]
        public async Task<IActionResult> GetUserIdFromToken([FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }

            // Remove the "Bearer " prefix if it exists
            string token = Authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? Authorization.Substring("Bearer ".Length).Trim()
                : Authorization;

            // Call the GetUserIdFromTokenAsync function to fetch the userId
            string result = await _userIdFromTokenService.GetUserIdFromTokenAsync(token);

            if (result == "error")
            {
                return BadRequest("Error retrieving userId.");
            }

            // Attempt to parse the result into an integer
            if (int.TryParse(result, out int userId))
            {
                return Ok(new
                {
                    Message = "UserId fetched successfully",
                    Data = userId // Return the userId as an integer
                });
            }

            // If the result cannot be parsed to an integer, return a BadRequest response
            return BadRequest("Invalid userId format received.");
        }



    }
}
