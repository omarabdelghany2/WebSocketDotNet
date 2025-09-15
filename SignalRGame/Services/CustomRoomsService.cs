using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using SignalRGame.Models;

namespace SignalRGame.Services
{
    public class CustomRoomsService
    {
        private readonly HttpClient _httpClient;

        public CustomRoomsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private class RoomsEnvelope
        {
            public int count { get; set; }
            public object next { get; set; }
            public object previous { get; set; }
            public List<RoomItem> results { get; set; }
        }

        private class RoomItem
        {
            public int id { get; set; }
            public int user { get; set; }
            public List<Question> questions { get; set; }
        }

        public async Task<List<Question>> GetQuestionsForRoomAsync(string token, int userId, int roomId)
        {
            var url = $"http://127.0.0.1:8004/api/custom-rooms/{userId}/";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

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

            var envelope = JsonSerializer.Deserialize<RoomsEnvelope>(json, options);
            var room = envelope?.results?.FirstOrDefault(r => r.id == roomId);
            return room?.questions ?? new List<Question>();
        }
    }
}


