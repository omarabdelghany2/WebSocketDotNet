using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using BackEnd.middlewareService.Models;
using System.Text.Json.Serialization;

namespace BackEnd.middlewareService.Services
{
    public class StoreService
    {
        private readonly HttpClient _httpClient;

        public StoreService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // ---------- MODELS (for responses) ----------
        public class AvatarListResponse
        {
            public int Count { get; set; }
            public string? Next { get; set; }
            public string? Previous { get; set; }
            public List<Avatar> Results { get; set; } = new();
        }

        public class UserProfileResponse
        {
            public int Id { get; set; }
            public string Coins { get; set; } = "0";
        }


        public async Task<List<Avatar>> GetAvailableAvatarsAsync(string token)
        {
            var avatarsUrl = "http://localhost:8004/api/store/avatar/";
            var profileUrl = "http://localhost:8004/api/user/profile/";

            try
            {
                // 1. Get all avatars
                var avatarsRequest = new HttpRequestMessage(HttpMethod.Get, avatarsUrl);
                avatarsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var avatarsResponse = await _httpClient.SendAsync(avatarsRequest);
                avatarsResponse.EnsureSuccessStatusCode();

                var avatarsResult = await avatarsResponse.Content.ReadAsStringAsync();
                var avatarResponse = JsonSerializer.Deserialize<AvatarListResponse>(
                    avatarsResult,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                var allAvatars = avatarResponse?.Results ?? new List<Avatar>();

                // 2. Get user profile (owned avatars)
                var profileRequest = new HttpRequestMessage(HttpMethod.Get, profileUrl);
                profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var profileResponse = await _httpClient.SendAsync(profileRequest);
                profileResponse.EnsureSuccessStatusCode();

                var profileResult = await profileResponse.Content.ReadAsStringAsync();
                var userProfile = JsonSerializer.Deserialize<UserProfile>(
                    profileResult,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                var ownedIds = userProfile?.Avatar?.Select(a => a.Avatar).ToHashSet() ?? new HashSet<int>();


                // 3. Filter out owned avatars
                var availableAvatars = allAvatars.Where(a => !ownedIds.Contains(a.Id)).ToList();

                return availableAvatars;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception fetching avatars: {ex.Message}");
                return new List<Avatar>();
            }
        }
        public async Task<decimal> GetUserBalanceAsync(string token)
        {
            var url = "http://localhost:8004/api/user/profile/";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error getting user balance: {await response.Content.ReadAsStringAsync()}");
                    return 0;
                }

                var result = await response.Content.ReadAsStringAsync();
                var profile = JsonSerializer.Deserialize<UserProfileResponse>(result,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return decimal.TryParse(profile?.Coins, out var coins) ? coins : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception getting user balance: {ex.Message}");
                return 0;
            }
        }

        public async Task<decimal> GetAvatarPriceAsync(string token, int avatarId)
        {
            var url = $"http://localhost:8004/api/store/avatar/{avatarId}/";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error getting avatar price: {await response.Content.ReadAsStringAsync()}");
                    return 0;
                }

                var result = await response.Content.ReadAsStringAsync();
                var avatar = JsonSerializer.Deserialize<Avatar>(result,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return avatar?.Price ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception getting avatar price: {ex.Message}");
                return 0;
            }
        }

        public async Task<PurchaseResult> PurchaseAvatarAsync(string token, int avatarId)
        {
            try
            {
                // 1. Get user profile
                var profileUrl = "http://localhost:8004/api/user/profile/";
                var profileRequest = new HttpRequestMessage(HttpMethod.Get, profileUrl);
                profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var profileResponse = await _httpClient.SendAsync(profileRequest);
                if (!profileResponse.IsSuccessStatusCode)
                {
                    return new PurchaseResult { Success = false, ErrorMessage = "Failed to fetch user profile." };
                }

                var profileJson = await profileResponse.Content.ReadAsStringAsync();
                var profile = JsonSerializer.Deserialize<UserProfileResponse>(profileJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (profile == null || !decimal.TryParse(profile.Coins, out var coins))
                {
                    return new PurchaseResult { Success = false, ErrorMessage = "Invalid user profile." };
                }

                // 2. Get avatar price
                var avatarPrice = await GetAvatarPriceAsync(token, avatarId);

                // 3. Check balance
                if (coins < avatarPrice)
                {
                    return new PurchaseResult { Success = false, ErrorMessage = "Insufficient balance." };
                }

                var newBalance = coins - avatarPrice;

                // 4. Add avatar to user (your API spec expects avatar + is_default)
                var avatarAddUrl = $"http://localhost:8004/api/user/avatar/";
                var avatarAddPayload = JsonSerializer.Serialize(new { avatar = avatarId, is_default = false });
                var avatarAddContent = new StringContent(avatarAddPayload, Encoding.UTF8, "application/json");

                var avatarAddRequest = new HttpRequestMessage(HttpMethod.Post, avatarAddUrl);
                avatarAddRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                avatarAddRequest.Content = avatarAddContent;

                var avatarAddResponse = await _httpClient.SendAsync(avatarAddRequest);
                if (!avatarAddResponse.IsSuccessStatusCode)
                {
                    var errorBody = await avatarAddResponse.Content.ReadAsStringAsync();
                    return new PurchaseResult { Success = false, ErrorMessage = $"Failed to add avatar. Server said: {errorBody}" };
                }

                return new PurchaseResult
                {
                    Success = true,
                    NewBalance = newBalance
                };
            }
            catch (Exception ex)
            {
                return new PurchaseResult { Success = false, ErrorMessage = $"Exception: {ex.Message}" };
            }
        }

        public async Task<bool> ChangeAvatarAsync(string token, int avatarId)
        {
            var url = "http://localhost:8004/api/user/edit/profile/";

            // ✅ match API expected payload
            var payload = JsonSerializer.Serialize(new { default_avatar_id = avatarId });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Patch, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = content;

            try
            {
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception changing avatar: {ex.Message}");
                return false;
            }
        }

        //TODO
        public async Task<bool> AddBalanceFromPayPalAsync(string token, decimal amount)
        {
            try
            {
                // 1. Get user profile
                var profileUrl = "http://localhost:8004/api/user/profile/";
                var profileRequest = new HttpRequestMessage(HttpMethod.Get, profileUrl);
                profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var profileResponse = await _httpClient.SendAsync(profileRequest);
                if (!profileResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine("Failed to fetch user profile.");
                    return false;
                }

                var profileJson = await profileResponse.Content.ReadAsStringAsync();
                var profile = JsonSerializer.Deserialize<UserProfileResponse>(profileJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (profile == null || !decimal.TryParse(profile.Coins, out var currentCoins))
                {
                    Console.WriteLine("Invalid profile response.");
                    return false;
                }

                int userId = profile.Id;

                // 2. Calculate new balance
                var newBalance = currentCoins + amount;

                // 3. Update coins
                var coinsUrl = $"http://localhost:8004/api/user/coins/{userId}/";
                var payload = JsonSerializer.Serialize(new { coins = newBalance.ToString("F2") });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                var coinsRequest = new HttpRequestMessage(HttpMethod.Put, coinsUrl);
                coinsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                coinsRequest.Content = content;

                var coinsResponse = await _httpClient.SendAsync(coinsRequest);
                return coinsResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception adding balance: {ex.Message}");
                return false;
            }
        }
    }

    // ---------- MODELS ----------
    public class UserAvatar
    {
        public int Id { get; set; }              // relation id
        public int Avatar { get; set; }          // store avatar id
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public decimal Price { get; set; }
        public bool Is_Default { get; set; }
    }

public class UserProfile
{
    public int Id { get; set; }
    public string First_Name { get; set; } = "";
    public string Last_Name { get; set; } = "";
    public string Profile_Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Country { get; set; } = "";
    public int Score { get; set; }
    public decimal Balance { get; set; }
    public bool Is_Staff { get; set; }
    public bool Is_Subscribed { get; set; }
    public string Coins { get; set; } = "0.00";
    public string Default_Avatar { get; set; } = "";
    public int? Default_Avatar_Id { get; set; }


    public List<UserAvatar> Avatar { get; set; } = new();
}

    public class PurchaseResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public decimal NewBalance { get; set; }
    }
}
