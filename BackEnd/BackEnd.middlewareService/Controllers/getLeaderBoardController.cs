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
    public class leaderBoardController : ControllerBase
    {
        private readonly leaderBoardSerivce _leaderBoardService;

        public leaderBoardController(leaderBoardSerivce leaderBoard)
        {
            _leaderBoardService = leaderBoard;
        }

        [HttpGet]
        public async Task<IActionResult> GetLeaderBoard([FromHeader] string Authorization)
        {
            // Ensure the Authorization header is present and has the correct format
            if (string.IsNullOrEmpty(Authorization) || !Authorization.StartsWith("Bearer "))
            {
                return BadRequest("Token is required in the Authorization header.");
            }
            

            // Extract the token from the Authorization header
            var token = Authorization.Substring("Bearer ".Length).Trim();

            // Call the GetLeaderBoardAsync function to fetch the leaderboard list
            string result = await _leaderBoardService.GetLeaderBoardAsync(token);

            if (result == "error")
            {
                return BadRequest("Error retrieving leaderboard list.");
            }

            try
            {
                // Parse the JSON response into a more readable format
                var leaderBoardList = JsonSerializer.Deserialize<object>(result);

                return Ok(new
                {
                    Message = "Leaderboard list fetched successfully",
                    Data = leaderBoardList // Return the leaderboard list as parsed JSON
                });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { Message = "Error parsing leaderboard JSON", Exception = ex.Message });
            }
        }

    }
}
