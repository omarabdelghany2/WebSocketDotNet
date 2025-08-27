using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using BackEnd.middlewareService.Models;

namespace BackEnd.middlewareService.Services
{
    public class StoreService
    {
        private readonly HttpClient _httpClient;

        public StoreService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Avatar>> GetAllAvatarsAsync(string token)
        {
            var url = "http://localhost:8004/api/store/avatars/";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Avatar>>(result, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Avatar>();
                }

                Console.WriteLine($"Error fetching avatars: {await response.Content.ReadAsStringAsync()}");
                return new List<Avatar>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception fetching avatars: {ex.Message}");
                return new List<Avatar>();
            }
        }

        public async Task<decimal> GetUserBalanceAsync(string token)
        {
            var url = "http://localhost:8004/api/store/balance/";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var balance = JsonSerializer.Deserialize<BalanceResponse>(result);
                    return balance?.Balance ?? 0;
                }

                Console.WriteLine($"Error getting balance: {await response.Content.ReadAsStringAsync()}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception getting balance: {ex.Message}");
                return 0;
            }
        }

        public async Task<decimal> GetAvatarPriceAsync(string token, int avatarId)
        {
            var url = $"http://localhost:8004/api/store/avatar-price/{avatarId}/";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var price = JsonSerializer.Deserialize<PriceResponse>(result);
                    return price?.Price ?? 0;
                }

                Console.WriteLine($"Error getting avatar price: {await response.Content.ReadAsStringAsync()}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception getting avatar price: {ex.Message}");
                return 0;
            }
        }

        public async Task<PurchaseResult> PurchaseAvatarAsync(string token, int avatarId)
        {
            var url = "http://localhost:8004/api/store/purchase/";
            var payload = JsonSerializer.Serialize(new { avatarId });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = content;

            try
            {
                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<PurchaseResult>(json);
                    result!.Success = true;
                    return result;
                }

                return new PurchaseResult
                {
                    Success = false,
                    ErrorMessage = json
                };
            }
            catch (Exception ex)
            {
                return new PurchaseResult
                {
                    Success = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

        public async Task<bool> ChangeAvatarAsync(string token, int avatarId)
        {
            var url = "http://localhost:8004/api/store/change-avatar/";
            var payload = JsonSerializer.Serialize(new { avatarId });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Put, url);
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

        public async Task<bool> AddBalanceFromPayPalAsync(string token, decimal amount)
        {
            var url = "http://localhost:8004/api/store/add-balance/";
            var payload = JsonSerializer.Serialize(new { amount });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = content;

            try
            {
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception adding balance: {ex.Message}");
                return false;
            }
        }
    }

    // Models (same file or separate)
    public class Avatar
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = "";
    }

    public class BalanceResponse
    {
        public decimal Balance { get; set; }
    }

    public class PriceResponse
    {
        public decimal Price { get; set; }
    }

    public class PurchaseResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public decimal NewBalance { get; set; }
    }
}
