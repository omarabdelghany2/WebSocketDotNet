using System.Text;
using System.Text.Json;

//now this function will be called 

namespace BackEnd.middlewareService.Services{
    public class TokenValidator
    {
        private readonly HttpClient _httpClient;

        public TokenValidator(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<string> ValidateTokenAsync(string token)
        {
            var databaseServerUrl = "http://localhost:8000/api/user/auth/";

            try
            {
                // Prepare the GET request
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, databaseServerUrl);

                // Set the Authorization header to include the Bearer token
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // Send the request
                var databaseResponse = await _httpClient.SendAsync(requestMessage);

                // Check if the response was successful
                if (databaseResponse.IsSuccessStatusCode)
                {
                    // Return the content of the response
                    return await databaseResponse.Content.ReadAsStringAsync();
                }

                // Handle non-success responses, return the status code and error message
                return $"Error: {databaseResponse.StatusCode}, {await databaseResponse.Content.ReadAsStringAsync()}";
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                return $"Exception: {ex.Message}";
            }
}


        public async Task<string> GetAccessTokenByRefresh(string refreshToken)
        {
            var databaseServerUrl = "http://localhost:8000/api/token/refresh/";

            try
            {

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, databaseServerUrl);
                // Prepare the body content (email in JSON format)
                var jsonPayload = JsonSerializer.Serialize(new { refresh = refreshToken, });
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Set the content of the request
                requestMessage.Content = content;


                // Send the request
                var databaseResponse = await _httpClient.SendAsync(requestMessage);

                if (databaseResponse.IsSuccessStatusCode)
                {
                    return await databaseResponse.Content.ReadAsStringAsync();
                }
                else
                {
                    // Log the error details
                    var errorDetails = await databaseResponse.Content.ReadAsStringAsync();
                    Console.Error.WriteLine($"Error refreshing access token. Status code: {databaseResponse.StatusCode}. Response: {errorDetails}");

                    // Return an error message
                    return $"Error: Unable to refresh access token. Status code: {databaseResponse.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                // Log the exception details
                Console.Error.WriteLine($"An error occurred while refreshing the access token: {ex.Message}");

                // Return a generic error message
                return "Error: An unexpected error occurred while refreshing the access token.";
            }
        }







    }
}