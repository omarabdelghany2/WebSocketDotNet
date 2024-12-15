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
            var databaseServerUrl = "http://41.40.138.255:8000/api/user/auth/";

            // Prepare the GET request
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, databaseServerUrl);

            // Set the Authorization header to include the Bearer token
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Send the request
            var databaseResponse = await _httpClient.SendAsync(requestMessage);

            if (databaseResponse.IsSuccessStatusCode)
            {
                return await databaseResponse.Content.ReadAsStringAsync();
            }

            return "error";
        }




    }
}