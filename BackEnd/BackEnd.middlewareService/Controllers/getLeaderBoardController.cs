using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BackEnd.middlewareService.Services;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers; // Added for MediaTypeHeaderValue


namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/leaderboard")]
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
            if (string.IsNullOrEmpty(Authorization) || !Authorization.StartsWith("Bearer "))
            {
                return BadRequest("Token is required in the Authorization header.");
            }

            var token = Authorization.Substring("Bearer ".Length).Trim();

            string result = await _leaderBoardService.GetLeaderBoardAsync(token);

            if (result == "error")
            {
                return BadRequest("Error retrieving leaderboard list.");
            }

            try
            {
                var leaderBoardList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(result);

                string[] orderedRanks = new[] { "Abyssal", "ICY", "Stone", "Copper", "Bronze", "Iron", "Classical", "Modern", "Contemporary" };
                int pointsPerPart = 100;
                int partsPerRank = 3;
                int pointsPerRank = pointsPerPart * partsPerRank; // 300
                int totalBeforeAI = orderedRanks.Length * pointsPerRank; // 2700

                foreach (var player in leaderBoardList)
                {
                    if (player.ContainsKey("score") && int.TryParse(player["score"].ToString(), out int score))
                    {
                        string rank;
                        if (score < 0)
                        {
                            rank = "NEWBIE";
                        }
                        else if (score < totalBeforeAI)
                        {
                            int rankIndex = score / pointsPerRank;
                            int remainderInRank = score % pointsPerRank;
                            int partIndex = remainderInRank / pointsPerPart; // 0..2
                            rank = $"{orderedRanks[rankIndex]} {partIndex + 1}";
                        }
                        else if (score < totalBeforeAI + 1000)
                        {
                            rank = "AI";
                        }
                        else
                        {
                            rank = "AI";
                        }

                        player["rank"] = rank;
                    }
                }

                return Ok(new
                {
                    Message = "Leaderboard list fetched successfully",
                    Data = leaderBoardList
                });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { Message = "Error parsing leaderboard JSON", Exception = ex.Message });
            }
        }


    }
}
