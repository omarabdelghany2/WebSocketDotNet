using System.Text.Json;  // For JsonSerializer and JsonException
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue


namespace SignalRGame.Services
{
    public class userIdFromProfileNameService
    {
        private readonly HttpClient _httpClient;

        public userIdFromProfileNameService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }



    public async Task<int?> GetUserIdFromProfileNameAsync(string token, string profile_name)
    {
        // Include profile_name as a query parameter in the URL
        var databaseServerUrl = $"http://localhost:8000/api/user/id/?profile_name={Uri.EscapeDataString(profile_name)}";

        // Prepare the request message with GET
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, databaseServerUrl);

        // Set the Authorization header to include the Bearer token
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Send the request
            var databaseResponse = await _httpClient.SendAsync(requestMessage);

            if (databaseResponse.IsSuccessStatusCode)
            {
                // Parse the response content as an integer
                var responseContent = await databaseResponse.Content.ReadAsStringAsync();
                if (int.TryParse(responseContent, out int userId))
                {
                    return userId;
                }
                else
                {
                    Console.WriteLine($"Unexpected response format: {responseContent}");
                    return null;
                }
            }
            else
            {
                // Capture and print the error response
                var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error from database: {errorContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occurred during the HTTP request
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return null;
        }
    }


    }
}