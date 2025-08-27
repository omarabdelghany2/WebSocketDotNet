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




        public async Task<bool> CreateCustomRoomAsync(string token)
        {
            var url = "http://localhost:8004/api/custom_room/create/";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
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

        public async Task<bool> DeleteCustomRoomAsync(string token)
        {
            var url = "http://localhost:8004/api/custom_room/delete/";

            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while deleting room: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckIfUserHasCustomRoomAsync(string token)
        {
            var url = "http://localhost:8004/api/custom_room/check/";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return result.Trim().ToLower() == "true";
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

        public async Task<string> GetCustomRoomQuestionsAsync(string token)
        {
            var url = "http://localhost:8004/api/custom_room/questions/";

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
                    Console.WriteLine($"Error fetching questions: {await response.Content.ReadAsStringAsync()}");
                    return "error";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return "error";
            }
        }



        public async Task<bool> SaveCustomRoomAsync(string token, List<Question> questions)
        {
            var url = "http://localhost:8004/api/custom_room/save/";

            var jsonPayload = JsonSerializer.Serialize(questions);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = content;

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine($"Error saving room: {await response.Content.ReadAsStringAsync()}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return false;
            }
        }





    }
}
