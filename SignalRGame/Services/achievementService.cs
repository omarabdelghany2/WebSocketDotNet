using Microsoft.AspNetCore.SignalR;
using SignalRGame.Hubs;
using SignalRGame.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SignalRGame.Services
{
    public class AchievementService
    {
        private readonly IHubContext<GameHub> _hubContext;
        private readonly UserProfileService _profileService;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // ✅ Unified achievement rules (Classic + Millionaire)
        private static readonly List<Achievement> AllAchievements = new()
        {
            // Classic
            new Achievement { Name = "Classic First Game", IconUrl = "/images/achievements/classic_first.png" },
            new Achievement { Name = "Classic Win 10", IconUrl = "/images/achievements/classic_win_10.png" },
            new Achievement { Name = "Classic Win 30", IconUrl = "/images/achievements/classic_win_30.png" },
            new Achievement { Name = "Classic Win 60", IconUrl = "/images/achievements/classic_win_60.png" },
            new Achievement { Name = "Classic Win 90", IconUrl = "/images/achievements/classic_win_90.png" },
            new Achievement { Name = "Classic Win 3 Consecutive", IconUrl = "/images/achievements/classic_win_3.png" },
            new Achievement { Name = "Classic Win 5 Consecutive", IconUrl = "/images/achievements/classic_win_5.png" },
            new Achievement { Name = "Classic Win 7 Consecutive", IconUrl = "/images/achievements/classic_win_7.png" },

            // Millionaire
            new Achievement { Name = "Millionaire First Game", IconUrl = "/images/achievements/millionaire_first.png" },
            new Achievement { Name = "Millionaire Win 10", IconUrl = "/images/achievements/millionaire_win_10.png" },
            new Achievement { Name = "Millionaire Win 30", IconUrl = "/images/achievements/millionaire_win_30.png" },
            new Achievement { Name = "Millionaire Win 60", IconUrl = "/images/achievements/millionaire_win_60.png" },
            new Achievement { Name = "Millionaire Win 90", IconUrl = "/images/achievements/millionaire_win_90.png" },
            new Achievement { Name = "Millionaire Win 3 Consecutive", IconUrl = "/images/achievements/millionaire_win_3.png" },
            new Achievement { Name = "Millionaire Win 5 Consecutive", IconUrl = "/images/achievements/millionaire_win_5.png" },
            new Achievement { Name = "Millionaire Win 7 Consecutive", IconUrl = "/images/achievements/millionaire_win_7.png" }
        };

        public AchievementService(UserProfileService profileService, IHubContext<GameHub> hubContext, HttpClient httpClient)
        {
            _profileService = profileService;
            _hubContext = hubContext;
            _httpClient = httpClient;
        }

        public async Task DetectAchievementsForRoom(string roomId)
        {
            Console.WriteLine($"[Achievements] DetectAchievementsForRoom called for room {roomId}");

            if (!GameHub.Rooms.TryGetValue(roomId, out var room))
            {
                Console.WriteLine($"[Achievements] No room found with ID {roomId}");
                return;
            }

            foreach (var player in room.Participants)
            {
                try
                {
                    var token = GameHub.TokenToUserId.FirstOrDefault(x => x.Value == player.userId).Key;
                    if (string.IsNullOrEmpty(token)) continue;

                    // Get owned achievements
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    var existingResp = await _httpClient.GetAsync("http://localhost:8004/api/achievements/user-achievements/");
                    var existingJson = await existingResp.Content.ReadAsStringAsync();
                    var existingApiResult = JsonSerializer.Deserialize<ApiResult<UserAchievement>>(existingJson, _jsonOptions);
                    var existingAchievements = existingApiResult?.Results ?? new List<UserAchievement>();

                    var ownedNames = existingAchievements
                        .Select(e => e.Name.Trim().ToLowerInvariant())
                        .ToHashSet();

                    // Get histories
                    var classicJson = await _profileService.GetUserClassicModeHistoryAsync(token);
                    var millionaireJson = await _profileService.GetUserMillionaireModeHistoryAsync(token);

                    var classicApiResult = JsonSerializer.Deserialize<ApiResult<ClassicGameHistory>>(classicJson, _jsonOptions);
                    var classicHistory = classicApiResult?.Results ?? new List<ClassicGameHistory>();

                    var millionaireApiResult = JsonSerializer.Deserialize<ApiResult<MillionaireGameHistory>>(millionaireJson, _jsonOptions);
                    var millionaireHistory = millionaireApiResult?.Results ?? new List<MillionaireGameHistory>();

                    // Resolve achievements
                    var unlocked = new List<string>();
                    unlocked.AddRange(CheckWins("Classic", classicHistory.Select(h => h.PlayerWon).ToList()));
                    unlocked.AddRange(CheckWins("Millionaire", millionaireHistory.Select(h => h.PlayerWon).ToList()));

                    var newAchievements = AllAchievements
                        .Where(a => unlocked.Contains(a.Name) && !ownedNames.Contains(a.Name.Trim().ToLowerInvariant()))
                        .ToList();

                    if (newAchievements.Any() && GameHub.UserIdToConnectionId.TryGetValue(player.userId, out var connId))
                    {
                        await _hubContext.Clients.Client(connId).SendAsync("achievementsUnlocked", new { achievements = newAchievements });

                        foreach (var ach in newAchievements)
                        {
                            var body = JsonSerializer.Serialize(new { name = ach.Name, description = "Unlocked achievement" });
                            var content = new StringContent(body, Encoding.UTF8, "application/json");
                            await _httpClient.PostAsync("http://localhost:8004/api/achievements/user-achievements/", content);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Achievements] Error for {player.userId}: {ex.Message}");
                }
            }
        }

        // ✅ New universal check for wins + consecutive wins
        private static List<string> CheckWins(string mode, List<bool> gameResults)
        {
            var unlocked = new List<string>();
            if (gameResults.Count == 0) return unlocked;

            // First game
            unlocked.Add($"{mode} First Game");

            // Total wins
            int totalWins = gameResults.Count(r => r);
            if (totalWins >= 10) unlocked.Add($"{mode} Win 10");
            if (totalWins >= 30) unlocked.Add($"{mode} Win 30");
            if (totalWins >= 60) unlocked.Add($"{mode} Win 60");
            if (totalWins >= 90) unlocked.Add($"{mode} Win 90");

            // Consecutive wins
            int currentStreak = 0;
            int maxStreak = 0;
            foreach (var win in gameResults)
            {
                if (win)
                {
                    currentStreak++;
                    maxStreak = Math.Max(maxStreak, currentStreak);
                }
                else
                {
                    currentStreak = 0;
                }
            }

            if (maxStreak >= 3) unlocked.Add($"{mode} Win 3 Consecutive");
            if (maxStreak >= 5) unlocked.Add($"{mode} Win 5 Consecutive");
            if (maxStreak >= 7) unlocked.Add($"{mode} Win 7 Consecutive");

            return unlocked.Distinct().ToList();
        }

        // Models
        public class Achievement { public string Name { get; set; } public string IconUrl { get; set; } }
        public class UserAchievement { public string Name { get; set; } public string Description { get; set; } public string IconUrl { get; set; } }

        public class ClassicGameHistory
        {
            [JsonPropertyName("id")] public int GameId { get; set; }
            [JsonPropertyName("winner_team")] public Team WinnerTeam { get; set; }
            [JsonPropertyName("teams")] public List<Team> Teams { get; set; } = new();
            public string PlayerTeam { get; set; }
            public bool PlayerWon => WinnerTeam != null && WinnerTeam.Members.Any(m => m.User.Id == PlayerId);
            public int PlayerId { get; set; }
        }

        public class MillionaireGameHistory
        {
            [JsonPropertyName("id")] public int GameId { get; set; }
            [JsonPropertyName("player")] public int PlayerId { get; set; }
            [JsonPropertyName("winner")] public bool PlayerWon { get; set; } // ✅ assumes API returns whether player won
        }

        public class Team { public int Id { get; set; } public string Color { get; set; } public List<Member> Members { get; set; } = new(); }
        public class Member { public User User { get; set; } }
        public class User { public int Id { get; set; } public string ProfileName { get; set; } public int Score { get; set; } }

        public class ApiResult<T>
        {
            public int Count { get; set; }
            public string? Next { get; set; }
            public string? Previous { get; set; }
            public List<T> Results { get; set; } = new();
        }
    }
}
