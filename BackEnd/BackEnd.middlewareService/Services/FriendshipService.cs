using System.Text.Json;  // For JsonSerializer and JsonException
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue


namespace BackEnd.middlewareService.Services
{
    public class FriendsService
    {
        private readonly HttpClient _httpClient;

        public FriendsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


    public async Task<string> GetFriendsListAsync(string token)
    {
        var databaseServerUrl = "http://localhost:8000/api/user/friends/list/";

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


    public async Task<bool> addFriendAsync(string token, int userID)
    {
        var databaseServerUrl = "http://localhost:8000/api/user/friends/add/";

        // Prepare the request message with PATCH method
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, databaseServerUrl);

        // Prepare the body content (email in JSON format)
        var jsonPayload = JsonSerializer.Serialize(new { friend_id = userID });
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Set the content of the request
        requestMessage.Content = content;
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Send the request
            var databaseResponse = await _httpClient.SendAsync(requestMessage);

            if (databaseResponse.IsSuccessStatusCode)
            {
                // Read the response content as a string (if needed for debugging/logging purposes)
                var responseContent = await databaseResponse.Content.ReadAsStringAsync();
                return true;
            }
            else
            {
                // Log the error response
                var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error from database: {errorContent}");
                return false; // Return false for non-success status codes
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occurred during the HTTP request
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return false; // Return false if an exception occurs
        }
    }

    public async Task<bool> remFriendAsync(string token, int friend_id)
    {
        var databaseServerUrl = $"http://localhost:8000/api/user/friends/remove/?friend_id={friend_id}";


        // Prepare the request message with PATCH method
        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, databaseServerUrl);
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Send the request
            var databaseResponse = await _httpClient.SendAsync(requestMessage);

            if (databaseResponse.IsSuccessStatusCode)
            {
                // Read the response content as a string (if needed for debugging/logging purposes)
                var responseContent = await databaseResponse.Content.ReadAsStringAsync();
                return true;
            }
            else
            {
                // Log the error response
                var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error from database: {errorContent}");
                return false; // Return false for non-success status codes
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occurred during the HTTP request
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return false; // Return false if an exception occurs
        }
    }


    public async Task<bool> acceptFriendAsync(string token, int userID)
    {
        var databaseServerUrl = "http://localhost:8000/api/user/friends/accept/";

        // Prepare the request message with PATCH method
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, databaseServerUrl);

        // Prepare the body content (email in JSON format)
        var jsonPayload = JsonSerializer.Serialize(new { user_request_id = userID });
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Set the content of the request
        requestMessage.Content = content;
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Send the request
            var databaseResponse = await _httpClient.SendAsync(requestMessage);

            if (databaseResponse.IsSuccessStatusCode)
            {
                // Read the response content as a string (if needed for debugging/logging purposes)
                var responseContent = await databaseResponse.Content.ReadAsStringAsync();
                return true;
            }
            else
            {
                // Log the error response
                var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error from database: {errorContent}");
                return false; // Return false for non-success status codes
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occurred during the HTTP request
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return false; // Return false if an exception occurs
        }
    }

    public async Task<string> getPendingFriendRequestAsync(string token)
    {
        var databaseServerUrl = "http://localhost:8000/api/user/friends/pending/list/";

        // Prepare the request message with Get method
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, databaseServerUrl);

        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Send the request
            var databaseResponse = await _httpClient.SendAsync(requestMessage);

            if (databaseResponse.IsSuccessStatusCode)
            {
                // Read the response content as a string (if needed for debugging/logging purposes)
                return await databaseResponse.Content.ReadAsStringAsync();
            }
            else
            {
                // Log the error response
                var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error from database: {errorContent}");
                return errorContent; // Return false for non-success status codes
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occurred during the HTTP request
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return ex.Message; // Return false if an exception occurs
        }
    }



    public async Task<bool> declinePendingRequestAsync(string token, int user_request_idD)
    {
        // Directly append the integer to the URL
        var databaseServerUrl = $"http://localhost:8000/api/user/friends/pending/decline/?user_request_id={user_request_idD}";

        // Prepare the request message with DELETE method
        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, databaseServerUrl);

        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Send the request
            var databaseResponse = await _httpClient.SendAsync(requestMessage);

            if (databaseResponse.IsSuccessStatusCode)
            {
                // Read the response content as a string (if needed for debugging/logging purposes)
                return true;
            }
            else
            {
                // Log the error response
                var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error from database: {errorContent}");
                return false; // Return the error content for non-success status codes
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occurred during the HTTP request
            Console.WriteLine($"Exception occurred: {ex.Message}");
            return false;
        }
    }






    }





}
