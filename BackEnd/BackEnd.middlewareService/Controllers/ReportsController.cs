using Microsoft.AspNetCore.Mvc;
using BackEnd.middlewareService.Services;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackEnd.middlewareService.Controllers
{
    public class Report
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("user")]
        public string? User { get; set; }

        [JsonPropertyName("game")]
        public string? Game { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "pending";
    }

    public class CreateReportRequest
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    public class ReportListResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("previous")]
        public string? Previous { get; set; }

        [JsonPropertyName("results")]
        public List<Report> Results { get; set; } = new List<Report>();
    }

    [Route("api/reports")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly ReportService _reportService;
        private readonly HttpClient _httpClient;

        public ReportsController(ReportService reportService, HttpClient httpClient)
        {
            _reportService = reportService;
            _httpClient = httpClient;
        }

        [HttpPost("question-create")]
        public async Task<IActionResult> CreateQuestionReport([FromBody] CreateReportRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (string.IsNullOrEmpty(token))
                    return Unauthorized(new { message = "Token is required" });

                Console.WriteLine($"[Question Report] Description received: {request.Description}");

                var jsonInput = JsonSerializer.Serialize(new
                {
                    description = request.Description,
                    type = "question_report"
                });

                var content = new StringContent(jsonInput, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var databaseUrl = "http://104.248.35.179:8004/api/reports/create/";
                var response = await _httpClient.PostAsync(databaseUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        message = "Question report created successfully",
                        response = JsonDocument.Parse(responseContent)
                    });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        message = "Question report creation failed",
                        error = responseContent
                    });
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid token" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating question report", error = ex.Message });
            }
        }

        [HttpPost("complaints-create")]
        public async Task<IActionResult> CreateComplaintReport([FromBody] CreateReportRequest request)
        {
            try
            {
                string token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (string.IsNullOrEmpty(token))
                    return Unauthorized(new { message = "Token is required" });

                Console.WriteLine($"[Complaint Report] Description received: {request.Description}");

                var jsonInput = JsonSerializer.Serialize(new
                {
                    description = request.Description,
                    type = "complaint_and_suggestion"
                });

                var content = new StringContent(jsonInput, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var databaseUrl = "http://104.248.35.179:8004/api/reports/create/";
                var response = await _httpClient.PostAsync(databaseUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        message = "Complaint report created successfully",
                        response = JsonDocument.Parse(responseContent)
                    });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        message = "Complaint report creation failed",
                        error = responseContent
                    });
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid token" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating complaint report", error = ex.Message });
            }
        }

        [HttpGet("list")]
        public async Task<ActionResult<ReportListResponse>> GetAllReports([FromQuery] int? page = null, [FromQuery] string type = null)
        {
            try
            {
                string token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (string.IsNullOrEmpty(token))
                    return Unauthorized(new { message = "Token is required" });

                var reports = await _reportService.GetAllReportsAsync(token, page, type);
                return Ok(reports);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid token" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching reports", error = ex.Message });
            }
        }
    }
}
