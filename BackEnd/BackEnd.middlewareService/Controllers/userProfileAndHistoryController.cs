



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

                // Rank logic
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
                    rank = "AI";
                }

                // Deserialize profile into dictionary
                var profile = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
                profile["rank"] = rank;

                // Extract owned achievements from API response
                var ownedAchievements = root.GetProperty("achievements")
                    .EnumerateArray()
                    .Select(a => a.GetProperty("name").GetString()?.Trim().ToLowerInvariant())
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToHashSet();

            
                // Predefined achievements (now with description)
                var allAchievements = new List<object>
                {
                    new { name = "First Classic Match", iconUrl = "/images/achievements/first_classic.png", description = "Play your very first Classic mode match." },
                    new { name = "Classic Winner x10", iconUrl = "/images/achievements/classic_winner_10.png", description = "Win 10 Classic matches." },
                    new { name = "Classic Winner x30", iconUrl = "/images/achievements/classic_winner_30.png", description = "Win 30 Classic matches." },
                    new { name = "Classic Winner x60", iconUrl = "/images/achievements/classic_winner_60.png", description = "Win 60 Classic matches." },
                    new { name = "Classic Winner x90", iconUrl = "/images/achievements/classic_winner_90.png", description = "Win 90 Classic matches." },
                    new { name = "Classic Winner 3 Consecutive", iconUrl = "/images/achievements/classic_winner_3.png", description = "Win 3 Classic matches in a row." },
                    new { name = "Classic Winner 5 Consecutive", iconUrl = "/images/achievements/classic_winner_5.png", description = "Win 5 Classic matches in a row." },
                    new { name = "Classic Winner 7 Consecutive", iconUrl = "/images/achievements/classic_winner_7.png", description = "Win 7 Classic matches in a row." },
                    new { name = "First Millionaire Match", iconUrl = "/images/achievements/first_millionaire.png", description = "Play your very first Millionaire mode match." },
                    new { name = "Millionaire Winner x10", iconUrl = "/images/achievements/millionaire_winner_10.png", description = "Win 10 Millionaire matches." },
                    new { name = "Millionaire Winner x30", iconUrl = "/images/achievements/millionaire_winner_30.png", description = "Win 30 Millionaire matches." },
                    new { name = "Millionaire Winner x60", iconUrl = "/images/achievements/millionaire_winner_60.png", description = "Win 60 Millionaire matches." },
                    new { name = "Millionaire Winner x90", iconUrl = "/images/achievements/millionaire_winner_90.png", description = "Win 90 Millionaire matches." },
                    new { name = "Millionaire Winner 3 Consecutive", iconUrl = "/images/achievements/millionaire_winner_3.png", description = "Win 3 Millionaire matches in a row." },
                    new { name = "Millionaire Winner 5 Consecutive", iconUrl = "/images/achievements/millionaire_winner_5.png", description = "Win 5 Millionaire matches in a row." },
                    new { name = "Millionaire Winner 7 Consecutive", iconUrl = "/images/achievements/millionaire_winner_7.png", description = "Win 7 Millionaire matches in a row." }
                };

                // Merge with ownership
                var achievementsWithOwnership = allAchievements
                    .Select(a => new
                    {
                        name = (string)a.GetType().GetProperty("name").GetValue(a),
                        iconUrl = (string)a.GetType().GetProperty("iconUrl").GetValue(a),
                        description = (string)a.GetType().GetProperty("description").GetValue(a),
                        owned = ownedAchievements.Contains(
                            ((string)a.GetType().GetProperty("name").GetValue(a)).Trim().ToLowerInvariant()
                        )
                    })
                    .ToList();

                // ✅ Overwrite "achievements" in profile
                profile["achievements"] = achievementsWithOwnership;

                return Ok(new
                {
                    Message = "Profile fetched successfully",
                    Data = profile
                });


            }
            catch (JsonException ex)
            {
                return BadRequest($"Invalid JSON format: {ex.Message}");
            }
        }



        // [HttpGet("classic-mode-history")]
        // public async Task<IActionResult> GetUserClassicModeHistory([FromHeader] string Authorization)
        // {
        //     if (string.IsNullOrEmpty(Authorization))
        //     {
        //         return BadRequest("Token is required.");
        //     }
        //     var token = Authorization.Substring("Bearer ".Length).Trim();

        //     // Call the GetUserProfileAsync function to fetch the friends list
        //     string result = await _userProfileService.GetUserClassicModeHistoryAsync(token);

        //     if (result == "error")
        //     {
        //         return BadRequest("Error retrieving friends list.");
        //     }

        //     try
        //     {
        //         // Parse the JSON response into a more readable format
        //         var profile = JsonSerializer.Deserialize<ClassicGame>(result);

        //         return Ok(new
        //         {
        //             Message = "profile classic mode history fetched successfully",
        //             Data = profile // Return the friends list as parsed JSON
        //         });
        //     }
        //     catch (JsonException ex)
        //     {
        //         return StatusCode(500, new { Message = "Error parsing profile JSON", Exception = ex.Message });
        //     }
        // }





        // [HttpGet("millionaire-mode-history")]
        // public async Task<IActionResult> GetUserMillionaireModeHistory([FromHeader] string Authorization)
        // {
        //     if (string.IsNullOrEmpty(Authorization))
        //     {
        //         return BadRequest("Token is required.");
        //     }
        //     var token = Authorization.Substring("Bearer ".Length).Trim();

        //     // Call the GetUserProfileAsync function to fetch the friends list
        //     string result = await _userProfileService.GetUserClassicModeHistoryAsync(token);

        //     if (result == "error")
        //     {
        //         return BadRequest("Error retrieving friends list.");
        //     }

        //     try
        //     {
        //         // Parse the JSON response into a more readable format
        //         var profile = JsonSerializer.Deserialize<MillionaireGame>(result);

        //         return Ok(new
        //         {
        //             Message = "profile classic mode history fetched successfully",
        //             Data = profile // Return the friends list as parsed JSON
        //         });
        //     }
        //     catch (JsonException ex)
        //     {
        //         return StatusCode(500, new { Message = "Error parsing profile JSON", Exception = ex.Message });
        //     }
        // }
 




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


        // public async Task<IActionResult> GetCombinedHistory([FromHeader] string Authorization)
        // {
        //     if (string.IsNullOrEmpty(Authorization))
        //     {
        //         return BadRequest("Token is required.");
        //     }
        //     var token = Authorization.Substring("Bearer ".Length).Trim();

        //     try
        //     {
        //         // Get all histories in parallel
        //         var classicTask = _userProfileService.GetUserClassicModeHistoryAsync(token);
        //         var millionaireTask = _userProfileService.GetUserMillionaireModeHistoryAsync(token);
        //         var customTask = _userProfileService.GetUserCustomGameHistoryAsync(token);

        //         await Task.WhenAll(classicTask, millionaireTask, customTask);

        //         var classicGames = JsonSerializer.Deserialize<ClassicGame>(await classicTask);
        //         var millionaireGames = JsonSerializer.Deserialize<MillionaireGame>(await millionaireTask);
        //         var customGames = JsonSerializer.Deserialize<ClassicGame>(await customTask); // Using same model as classic games

        //         // Combine all games into one response
        //         var combinedGames = new List<object>();

        //         // Add classic games
        //         foreach (var game in classicGames.Results)
        //         {
        //             combinedGames.Add(new
        //             {
        //                 type = "classic",
        //                 game
        //             });
        //         }

        //         // Add millionaire games
        //         foreach (var game in millionaireGames.Results)
        //         {
        //             combinedGames.Add(new
        //             {
        //                 type = "millionaire",
        //                 game
        //             });
        //         }

        //         // Add custom games
        //         foreach (var game in customGames.Results)
        //         {
        //             combinedGames.Add(new
        //             {
        //                 type = "custom",
        //                 game
        //             });
        //         }

        //         // Sort all games by creation date, newest first
        //         var sortedGames = combinedGames.OrderByDescending(g =>
        //         {
        //             if (g.GetType().GetProperty("game").GetValue(g) is Result r)
        //                 return r.CreatedAt;
        //             if (g.GetType().GetProperty("game").GetValue(g) is MillionaireGameResult m)
        //                 return m.CreatedAt;
        //             return DateTime.MinValue;
        //         }).ToList();

        //         return Ok(new
        //         {
        //             Message = "Combined game history fetched successfully",
        //             Data = new
        //             {
        //                 count = sortedGames.Count,
        //                 next = (string)null,
        //                 previous = (string)null,
        //                 results = sortedGames
        //             }
        //         });
        //     }
        //     catch (JsonException ex)
        //     {
        //         return StatusCode(500, new { Message = "Error parsing game history", Exception = ex.Message });
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, new { Message = "Error fetching game history", Exception = ex.Message });
        //     }
        // }
        



        public class ParagraphGame
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("next")]
    public string Next { get; set; }

    [JsonPropertyName("previous")]
    public string Previous { get; set; }

    [JsonPropertyName("results")]
    public List<ParagraphGameResult> Results { get; set; }
}

public class ParagraphGameResult
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("host")]
    public int Host { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("host_name")]
    public string HostName { get; set; }

    [JsonPropertyName("players")]
    public List<ParagraphPlayer> Players { get; set; }
}

public class ParagraphPlayer
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("player")]
    public int Player { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("player_name")]
    public string PlayerName { get; set; }
}




    [HttpGet("combined-history")]
    public async Task<IActionResult> GetCombinedHistory([FromHeader] string Authorization)
    {
        if (string.IsNullOrEmpty(Authorization))
            return BadRequest("Token is required.");

        var token = Authorization.Substring("Bearer ".Length).Trim();

        try
        {
            // Fetch all histories in parallel (✅ added paragraph)
            var classicTask = _userProfileService.GetUserClassicModeHistoryAsync(token);
            var millionaireTask = _userProfileService.GetUserMillionaireModeHistoryAsync(token);
            var customTask = _userProfileService.GetUserCustomGameHistoryAsync(token);
            var paragraphTask = _userProfileService.GetUserParagraphGameHistoryAsync(token);

            await Task.WhenAll(classicTask, millionaireTask, customTask, paragraphTask);

            // Deserialize responses
            var classicGames = JsonSerializer.Deserialize<ClassicGame>(await classicTask);
            var millionaireGames = JsonSerializer.Deserialize<MillionaireGame>(await millionaireTask);
            var customGames = JsonSerializer.Deserialize<ClassicGame>(await customTask);
            var paragraphGames = JsonSerializer.Deserialize<ParagraphGame>(await paragraphTask);

            // Combine all games into a single list
            var combinedGames = new List<object>();

            if (classicGames?.Results != null)
            {
                combinedGames.AddRange(classicGames.Results.Select(game => new
                {
                    type = "classic",
                    game
                }));
            }

            if (millionaireGames?.Results != null)
            {
                combinedGames.AddRange(millionaireGames.Results.Select(game => new
                {
                    type = "millionaire",
                    game
                }));
            }

            if (customGames?.Results != null)
            {
                combinedGames.AddRange(customGames.Results.Select(game => new
                {
                    type = "custom",
                    game
                }));
            }

            if (paragraphGames?.Results != null)
            {
                combinedGames.AddRange(paragraphGames.Results.Select(game => new
                {
                    type = "paragraph",
                    game
                }));
            }

            // Sort all games by creation date, newest first
            var sortedGames = combinedGames.OrderByDescending(g =>
            {
                var game = g.GetType().GetProperty("game")?.GetValue(g);
                return game switch
                {
                    Result r => r.CreatedAt,
                    MillionaireGameResult m => m.CreatedAt,
                    ParagraphGameResult p => p.CreatedAt,
                    _ => DateTime.MinValue
                };
            }).ToList();

            return Ok(new
            {
                Message = "Combined game history fetched successfully",
                Data = sortedGames
            });
        }
        catch (JsonException ex)
        {
            return StatusCode(500, new { Message = "Error parsing game history", Exception = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error fetching game history", Exception = ex.Message });
        }
    }






    }
}
