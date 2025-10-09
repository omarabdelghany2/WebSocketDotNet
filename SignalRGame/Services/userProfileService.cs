using System.Net.Http;
using System.Threading.Tasks;

namespace SignalRGame.Services
{
    public class UserProfileService
    {
        private readonly HttpClient _httpClient;

        public UserProfileService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Get user profile info
        /// </summary>
        public async Task<string> GetUserProfileAsync(string token)
        {
            return await SendGetRequestAsync("http://localhost:8004/api/user/profile/", token);
        }

        /// <summary>
        /// Get Classic mode history
        /// </summary>
        public async Task<string> GetUserClassicModeHistoryAsync(string token)
        {
            return await SendGetRequestAsync("http://localhost:8004/api/game/", token);
        }

        /// <summary>
        /// Get Millionaire mode history
        /// </summary>
        public async Task<string> GetUserMillionaireModeHistoryAsync(string token)
        {
            return await SendGetRequestAsync("http://localhost:8004/api/millionaire/game/", token);
        }

        // 🔒 Helper for GET requests with Bearer token
        private async Task<string> SendGetRequestAsync(string url, string token)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[UserProfileService] Error from API: {errorContent}");
                    return $"Error: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserProfileService] Exception: {ex.Message}");
                return $"Exception: {ex.Message}";
            }
        }


        
    }
}
