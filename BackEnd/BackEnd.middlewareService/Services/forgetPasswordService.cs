using System.Text.Json;  // For JsonSerializer and JsonException
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue


namespace BackEnd.middlewareService.Services
{
    public class ForgetPsswordService
    {
        private readonly HttpClient _httpClient;

        public ForgetPsswordService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        public async Task<bool> ValidateEmail(string Email)
        {
            var databaseServerUrl = $"http://192.168.1.74:8000/api/user/password/reset/?email={Uri.EscapeDataString(Email)}";
            

            // Prepare the request message with POST
            
            try
            {
                // Send the request
                var databaseResponse = await _httpClient.GetAsync(databaseServerUrl);

                if (databaseResponse.IsSuccessStatusCode)
                {
                    // Read the response content as a string
                    var responseContent = await databaseResponse.Content.ReadAsStringAsync();
                    return(true);
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



        public async Task<bool> SaveCode(string Email,string VerifyCode)
        {
            var databaseServerUrl = "http://192.168.1.74:8000/api/user/password/verification/store/";

            // Prepare the request message with POST
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, databaseServerUrl);

            // Prepare the body content (email in JSON format)
            var jsonPayload = JsonSerializer.Serialize(new { email = Email ,verification_code = VerifyCode });
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Set the content of the request
            requestMessage.Content = content;

            try
            {
                // Send the request
                var databaseResponse = await _httpClient.SendAsync(requestMessage);

                if (databaseResponse.IsSuccessStatusCode)
                {
                    // Read the response content as a string
                    var responseContent = await databaseResponse.Content.ReadAsStringAsync();
                    return(true);
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


        public async Task<string> GetCode(string Email)
        {
            // Prepare the database server URL with the email as a query parameter
            var databaseServerUrl = $"http://192.168.1.74:8000/api/user/password/verification/?email={Uri.EscapeDataString(Email)}";

            try
            {
                // Send the GET request
                var databaseResponse = await _httpClient.GetAsync(databaseServerUrl);

                if (databaseResponse.IsSuccessStatusCode)
                {
                    // Read the response content as a string
                    var responseContent = await databaseResponse.Content.ReadAsStringAsync();

                    // Parse the JSON response to extract the code
                    var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (responseJson.TryGetProperty("verification_code", out var codeProperty))
                    {
                        return codeProperty.GetString(); // Return the extracted code
                    }
                    else
                    {
                        Console.WriteLine("Code property not found in response.");
                        return null; // Return null if "code" property is missing
                    }
                }
                else
                {
                    // Log the error response
                    var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error from database: {errorContent}");
                    return null; // Return null for non-success status codes
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occurred during the HTTP request
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return null; // Return null if an exception occurs
            }
        }



    public async Task<bool> ResetPassword(string Email, string newPassword, string verifyCode)
    {
        var databaseServerUrl = "http://192.168.1.74:8000/api/user/password/reset/confirm/";

        // Prepare the request message with PATCH method
        var requestMessage = new HttpRequestMessage(HttpMethod.Patch, databaseServerUrl);

        // Prepare the body content (email in JSON format)
        var jsonPayload = JsonSerializer.Serialize(new { email = Email, verification_code = verifyCode, new_password = newPassword });
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Set the content of the request
        requestMessage.Content = content;

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




    }
}

