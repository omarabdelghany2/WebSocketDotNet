using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using BackEnd.middlewareService.Models;




namespace BackEnd.middlewareService.Services

{
    public class CustomRoomService
    {
        private readonly HttpClient _httpClient;

        public CustomRoomService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


 

        public async Task<bool> CreateCustomRoomAsync(int userId, string token)
        {
            var url = $"http://localhost:8004/api/custom-rooms/{userId}/";

            var requestBody = new
            {
                questions = new string[] { }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while creating room: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteCustomRoomAsync(int userId, int roomId, string token)
        {
            var url = $"http://localhost:8004/api/custom-rooms/{userId}/{roomId}/";

            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error deleting room: {await response.Content.ReadAsStringAsync()}");
                }
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while deleting room: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckIfUserHasCustomRoomAsync(int userId, string token)
        {
            var url = $"http://localhost:8004/api/custom-rooms/{userId}/";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("count", out var countElement))
                    {
                        return countElement.GetInt32() > 0;
                    }

                    return false; // if no "count" property, assume no room
                }
                else
                {
                    Console.WriteLine($"Error checking room: {await response.Content.ReadAsStringAsync()}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetCustomRoomDetailsAsync(int userId, string token)
        {
            var url = $"http://localhost:8004/api/custom-rooms/{userId}/";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine($"Error fetching room details: {await response.Content.ReadAsStringAsync()}");
                    return "error";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return "error";
            }
        }


        public async Task<bool> AddQuestionsToCustomRoomAsync(int userId, int roomId, string token, List<Question> questions)
        {
            var url = $"http://localhost:8004/api/custom-rooms/{userId}/{roomId}/";

            var requestBody = new { questions };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error adding questions: {await response.Content.ReadAsStringAsync()}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while adding questions: {ex.Message}");
                return false;
            }
        }




    }
}
