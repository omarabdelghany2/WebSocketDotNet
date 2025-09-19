using System.Net.Http;
using System.Text.Json;
using SignalRGame.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SignalRGame.Services
{
    public class GuestRoomService
    {
        private readonly HttpClient _httpClient;

        public GuestRoomService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Question>> GetGuestRoomQuestionsAsync()
        {
            var url = "http://127.0.0.1:8004/api/guest-room/";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // 🚫 Guest mode does not use Authorization
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new List<Question>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // API returns a raw list of questions
            var questions = JsonSerializer.Deserialize<List<Question>>(json, options);

            return questions ?? new List<Question>();
        }
    }
}
