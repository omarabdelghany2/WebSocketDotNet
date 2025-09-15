



using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using BackEnd.middlewareService.Services;

namespace BackEnd.middlewareService.Controllers
{
    [ApiController]
    [Route("api/user-profile")]
    public class userProfileController : ControllerBase
    {
        private readonly userProfileService _userProfileService;

        public userProfileController(userProfileService userprofile)
        {
            _userProfileService = userprofile;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserProfile([FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }

            var token = Authorization.Substring("Bearer ".Length).Trim();

            string result = await _userProfileService.GetUserProfileAsync(token);

            if (result == "error")
            {
                return BadRequest("Error retrieving user profile.");
            }

            try
            {
                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;

                // Extract score
                int score = root.GetProperty("score").GetInt32();

                // Rank logic: each rank has 3 parts of 100 points (except AI has 1000)
                string[] orderedRanks = new[] { "Abyssal", "ICY", "Stone", "Copper", "Bronze", "Iron", "Classical", "Modern", "Contemporary" };
                int pointsPerPart = 100;
                int partsPerRank = 3;
                int pointsPerRank = pointsPerPart * partsPerRank; // 300
                int totalBeforeAI = orderedRanks.Length * pointsPerRank; // 2700

                string rank;
                if (score < 0)
                {
                    rank = "NEWBIE";
                }
                else if (score < totalBeforeAI)
                {
                    int rankIndex = score / pointsPerRank;
                    int remainderInRank = score % pointsPerRank;
                    int partIndex = remainderInRank / pointsPerPart; // 0,1,2
                    rank = $"{orderedRanks[rankIndex]} {partIndex + 1}";
                }
                else if (score < totalBeforeAI + 1000)
                {
                    rank = "AI";
                }
                else
                {
                    // If above AI cap, stay at AI
                    rank = "AI";
                }

                // Deserialize profile into a dictionary so we can add rank
                var profile = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
                profile["rank"] = rank;

                return Ok(new
                {
                    Message = "Profile fetched successfully",
                    Data = profile
                });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { Message = "Error parsing profile JSON", Exception = ex.Message });
            }
        }




        [HttpGet("classic-mode-history")]
        public async Task<IActionResult> GetUserClassicModeHistory([FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }
            var token = Authorization.Substring("Bearer ".Length).Trim();

            // Call the GetUserProfileAsync function to fetch the friends list
            string result = await _userProfileService.GetUserClassicModeHistoryAsync(token);

            if (result == "error")
            {
                return BadRequest("Error retrieving friends list.");
            }

            try
            {
                // Parse the JSON response into a more readable format
                var profile = JsonSerializer.Deserialize<ClassicGame>(result);

                return Ok(new
                {
                    Message = "profile classic mode history fetched successfully",
                    Data = profile // Return the friends list as parsed JSON
                });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { Message = "Error parsing profile JSON", Exception = ex.Message });
            }
        }





        [HttpGet("millionaire-mode-history")]
        public async Task<IActionResult> GetUserMillionaireModeHistory([FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }
            var token = Authorization.Substring("Bearer ".Length).Trim();

            // Call the GetUserProfileAsync function to fetch the friends list
            string result = await _userProfileService.GetUserClassicModeHistoryAsync(token);

            if (result == "error")
            {
                return BadRequest("Error retrieving friends list.");
            }

            try
            {
                // Parse the JSON response into a more readable format
                var profile = JsonSerializer.Deserialize<MillionaireGame>(result);

                return Ok(new
                {
                    Message = "profile classic mode history fetched successfully",
                    Data = profile // Return the friends list as parsed JSON
                });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { Message = "Error parsing profile JSON", Exception = ex.Message });
            }
        }
 



        [HttpGet("combined-history")]
        public async Task<IActionResult> GetCombinedGameHistory([FromHeader] string Authorization)
        {
            if (string.IsNullOrEmpty(Authorization))
            {
                return BadRequest("Token is required.");
            }

            var token = Authorization.Substring("Bearer ".Length).Trim();

            var classicTask = _userProfileService.GetUserClassicModeHistoryAsync(token);
            var millionaireTask = _userProfileService.GetUserMillionaireModeHistoryAsync(token);

            await Task.WhenAll(classicTask, millionaireTask);

            if (classicTask.Result == "error" || millionaireTask.Result == "error")
            {
                return BadRequest("Error retrieving game history.");
            }

            try
            {
                var classicGames = JsonSerializer.Deserialize<ClassicGame>(classicTask.Result);
                var millionaireGames = JsonSerializer.Deserialize<MillionaireGame>(millionaireTask.Result);

                var mergedHistory = new List<MergedGameEntry>();

            if (classicGames?.Results != null)
            {
                mergedHistory.AddRange(classicGames.Results.Select(c => new MergedGameEntry
                {
                    gameMode = "Classic",
                    createdAt = c.CreatedAt.ToString("yyyy/MM/dd"),
                    data = new
                    {
                        id = c.Id,
                        winnerTeam = c.WinnerTeam?.Color.Equals("red", StringComparison.OrdinalIgnoreCase) == true ? "Red" :
                                    c.WinnerTeam?.Color.Equals("blue", StringComparison.OrdinalIgnoreCase) == true ? "Blue" : "",
                        members = c.Teams?.Sum(t => t.NumberOfMembers) ?? 0
                    }
                }));
            }

            if (millionaireGames?.Results != null)
            {
                mergedHistory.AddRange(millionaireGames.Results.Select(m => new MergedGameEntry
                {
                    gameMode = "Millionaire",
                    createdAt = m.CreatedAt.ToString("yyyy/MM/dd"),
                    data = new
                    {
                        id = m.Id,
                        winnerTeam = m.Score.ToString(),
                        members = 1
                    }
                }));
            }




                var ordered = mergedHistory.OrderByDescending(g => g.createdAt).ToList();

                return Ok(new
                {
                    Message = "Combined game history fetched successfully",
                    Data = ordered
                });
            }
            catch (JsonException ex)
            {
                return StatusCode(500, new { Message = "Error parsing JSON", Exception = ex.Message });
            }
        }

        public class MergedGameEntry
        {
            public string gameMode { get; set; } // "Classic" or "Millionaire"
            public string createdAt { get; set; } // ✅ Now matches the formatted string
            public object data { get; set; } // Contains WinnerTeam and Members
        }




        public class ClassicGame
        {
            [JsonPropertyName("count")]
            public int Count { get; set; }

            [JsonPropertyName("next")]
            public string Next { get; set; }

            [JsonPropertyName("previous")]
            public string Previous { get; set; }

            [JsonPropertyName("results")]
            public List<Result> Results { get; set; }
        }

        public class Result
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("host")]
            public string Host { get; set; }

            [JsonPropertyName("is_public")]
            public bool IsPublic { get; set; }

            [JsonPropertyName("created_at")]
            public DateTime CreatedAt { get; set; }

            [JsonPropertyName("sub_categories")]
            public List<string> SubCategories { get; set; }

            [JsonPropertyName("winner_team")]
            public Team WinnerTeam { get; set; }

            [JsonPropertyName("winner_score")]
            public int WinnerScore { get; set; }

            [JsonPropertyName("teams")]
            public List<Team> Teams { get; set; }
        }

        public class Team
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("color")]
            public string Color { get; set; }

            [JsonPropertyName("score")]
            public int Score { get; set; }

            [JsonPropertyName("number_of_members")]
            public int NumberOfMembers { get; set; }

            [JsonPropertyName("members")]
            public List<Member> Members { get; set; }
        }

        public class Member
        {
            [JsonPropertyName("user")]
            public User User { get; set; }
        }

        public class User
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("profile_name")]
            public string ProfileName { get; set; }

            [JsonPropertyName("score")]
            public int Score { get; set; }
        }
    
    
    


        public class MillionaireGame
        {
            [JsonPropertyName("count")]
            public int Count { get; set; }

            [JsonPropertyName("next")]
            public string Next { get; set; }

            [JsonPropertyName("previous")]
            public string Previous { get; set; }

            [JsonPropertyName("results")]
            public List<MillionaireGameResult> Results { get; set; }
        }

        public class MillionaireGameResult
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("created_at")]
            public DateTime CreatedAt { get; set; }

            [JsonPropertyName("score")]
            public int Score { get; set; }

            [JsonPropertyName("player")]
            public int Player { get; set; }
        }  
    
    }
}
