using System.Text.Json;  // For JsonSerializer and JsonException
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue


namespace BackEnd.middlewareService.Services
{
    public class userIdFromTokenService
    {
        private readonly HttpClient _httpClient;

        public userIdFromTokenService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


    public async Task<string> GetUserIdFromTokenAsync(string token)
    {
        var databaseServerUrl = "http://41.40.138.255:8000/api/user/id/";

        // Prepare the request message with GET
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, databaseServerUrl);

        // Set the Authorization header to include the Bearer token
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Prepare the body content (user_id in JSON format)

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
