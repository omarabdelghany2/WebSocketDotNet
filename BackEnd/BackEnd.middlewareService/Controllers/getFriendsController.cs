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

            string result = await _friendsService.GetFriendsListAsync(token);

            if (result == "error" || result.StartsWith("Exception"))
            {
                return BadRequest("Error retrieving friends list.");
            }

            try
            {
                var friendsList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(result);

                string[] orderedRanks = new[] { "Abyssal", "ICY", "Stone", "Copper", "Bronze", "Iron", "Classical", "Modern", "Contemporary" };
                int pointsPerPart = 100;
                int partsPerRank = 3;
                int pointsPerRank = pointsPerPart * partsPerRank; // 300
                int totalBeforeAI = orderedRanks.Length * pointsPerRank; // 2700

                foreach (var friend in friendsList)
                {
                    if (friend.ContainsKey("friend_score") &&
                        int.TryParse(friend["friend_score"].ToString(), out int score))
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

                        friend["rank"] = rank;
                    }
                }

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
