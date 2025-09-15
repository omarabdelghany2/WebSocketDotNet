using System.Text.Json;  // For JsonSerializer and JsonException
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue
using System.Net.Http;

namespace SignalRGame.Services
{
    public class isSubscribedService
    {
        private readonly HttpClient _httpClient;

        public isSubscribedService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> isSubscribedAsync(string token)
        {
            var databaseServerUrl = "http://localhost:8005/api/subscription/status/";

            try
            {
                // Prepare the GET request
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, databaseServerUrl);

                // Set the Authorization header to include the Bearer token
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Send the request
                var databaseResponse = await _httpClient.SendAsync(requestMessage);

                // Check if the response was successful
                if (databaseResponse.IsSuccessStatusCode)
                {
                    // Return true if the response is successful
                    return true;
                }
                else
                {
                    // Return false if the response failed
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                return false;
            }
        }
    }
}
