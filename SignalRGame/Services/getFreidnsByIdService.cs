using System.Text.Json;  // For JsonSerializer and JsonException
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue

namespace SignalRGame.Services
{
    public class GetFriendsByIdService
    {
        private readonly HttpClient _httpClient;

        public GetFriendsByIdService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetFriendsByIdAsync(int userId)
        {
            // Include userId in the URL as a path variable
            var databaseServerUrl = $"http://localhost:8000/api/user/friends/list/{userId}";

            // Prepare the request message with GET
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, databaseServerUrl);

            try
            {
                // Send the request
                var databaseResponse = await _httpClient.SendAsync(requestMessage);

                if (databaseResponse.IsSuccessStatusCode)
                {
                    // Return the response content (e.g., a list of friends in JSON format)
                    return await databaseResponse.Content.ReadAsStringAsync();
                }
                else
                {
                    // Capture and print the error response
                    var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error from database: {errorContent}");
                    return $"Error: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occurred during the HTTP request
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return $"Exception: {ex.Message}";
            }
        }
    }
}
