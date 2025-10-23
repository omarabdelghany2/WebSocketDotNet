using System.Text.Json;  // For JsonSerializer and JsonException
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue
using System.Text.Json.Serialization;



namespace BackEnd.middlewareService.Services
{
    public class userProfileService
    {
        private readonly HttpClient _httpClient;

        public userProfileService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        public async Task<string> GetUserProfileAsync(string token)
        {
            var databaseServerUrl = "http://localhost:8004/api/user/profile/";

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










        public async Task<string> GetUserClassicModeHistoryAsync(string token)
        {
            var databaseServerUrl = "http://localhost:8004/api/game/";

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
                    return await databaseResponse.Content.ReadAsStringAsync();
                }
                else
                {
                    var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error from database: {errorContent}");
                    return $"Error: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return $"Exception: {ex.Message}";
            }
        }

        public async Task<string> GetUserCustomGameHistoryAsync(string token)
        {
            var databaseServerUrl = "http://localhost:8004/api/custom-rooms/game/";

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






        public async Task<string> GetUserMillionaireModeHistoryAsync(string token)
        {
            var databaseServerUrl = "http://localhost:8004/api/millionaire/game/";

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



        public async Task<string> GetUserParagraphGameHistoryAsync(string token)
        {
            var databaseServerUrl = "http://localhost:8004/api/paragraph/game/";

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, databaseServerUrl);
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            try
            {
                var databaseResponse = await _httpClient.SendAsync(requestMessage);

                if (databaseResponse.IsSuccessStatusCode)
                {
                    return await databaseResponse.Content.ReadAsStringAsync();
                }
                else
                {
                    var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error from database: {errorContent}");
                    return $"Error: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return $"Exception: {ex.Message}";
            }
        }

        public async Task<bool> ChangeProfileNameAsync(string token, string newProfileName)
        {
            var url = "http://localhost:8004/api/user/edit/profile/";
            var payload = JsonSerializer.Serialize(new { profile_name = newProfileName });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Patch, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            request.Content = content;
            try
            {
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception changing profile name: {ex.Message}");
                return false;
            }
        }
    }
}
