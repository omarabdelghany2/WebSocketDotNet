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
            var url = "http://127.0.0.1:8004/api/core/guest-mode-questions/";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

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

            // Deserialize into wrapper, then pull "results"
            var apiResponse = JsonSerializer.Deserialize<QuestionApiResponse>(json, options);

            return apiResponse?.Results ?? new List<Question>();
        }


        public class QuestionApiResponse
            {
                public int Count { get; set; }
                public string Next { get; set; }
                public string Previous { get; set; }
                public List<Question> Results { get; set; }
            }


    }
}
