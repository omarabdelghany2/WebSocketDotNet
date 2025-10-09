using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using BackEnd.middlewareService.Controllers;

namespace BackEnd.middlewareService.Services
{
    public class ReportService
    {
        private readonly HttpClient _httpClient;

        public ReportService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Report> CreateReportAsync(string token, string description)
        {
            var databaseServerUrl = "http://localhost:8004/api/reports/create/";

            // Prepare the request message with POST
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, databaseServerUrl);

            // Set the Authorization header to include the Bearer token
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Prepare the request body
            var requestBody = new { description = description };
            var json = JsonSerializer.Serialize(requestBody);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                // Send the request
                var databaseResponse = await _httpClient.SendAsync(requestMessage);

                if (databaseResponse.IsSuccessStatusCode)
                {
                    // Parse and return the response
                    var responseContent = await databaseResponse.Content.ReadAsStringAsync();
                    var report = JsonSerializer.Deserialize<Report>(responseContent)
                        ?? throw new Exception("Failed to deserialize report response");
                    return report;
                }
                else
                {
                    // Capture and print the error response
                    var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error from database: {errorContent}");
                    throw new Exception($"Failed to create report: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occurred during the HTTP request
                Console.WriteLine($"Exception occurred: {ex.Message}");
                throw;
            }
        }

        public async Task<ReportListResponse> GetAllReportsAsync(string token)
        {
            var databaseServerUrl = "http://localhost:8004/api/reports/list/";

            // Prepare the request message with GET
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, databaseServerUrl);

            // Set the Authorization header to include the Bearer token
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                // Send the request
                var databaseResponse = await _httpClient.SendAsync(requestMessage);

                if (databaseResponse.IsSuccessStatusCode)
                {
                    // Parse and return the response
                    var responseContent = await databaseResponse.Content.ReadAsStringAsync();
                    var reportList = JsonSerializer.Deserialize<ReportListResponse>(responseContent)
                        ?? throw new Exception("Failed to deserialize report list response");
                    return reportList;
                }
                else
                {
                    // Capture and print the error response
                    var errorContent = await databaseResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error from database: {errorContent}");
                    throw new Exception($"Failed to get reports: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occurred during the HTTP request
                Console.WriteLine($"Exception occurred: {ex.Message}");
                throw;
            }
        }
    }
}