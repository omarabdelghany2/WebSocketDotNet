using System.Text.Json;  // For JsonSerializer and JsonException
using System.Text;       // For Encoding
using System.Net.Http.Headers; // For MediaTypeHeaderValue
using System.Net.Http;
using SignalRGame.Models;
namespace SignalRGame.Services
{
    public class Mode4Service
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://localhost:8004/api";

        public Mode4Service(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(string passage, List<Question> questions)> GetParagraphAndQuestionsAsync(string token, int maxRetries = 3)
        {
            int attempts = 0;

            while (attempts < maxRetries)
            {
                attempts++;

                try
                {
                    Console.WriteLine($"[INFO] Attempting to fetch paragraph with questions (Attempt {attempts}/{maxRetries})");

                    // Get random paragraph
                    var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/paragraph/random/");
                    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    var response = await _httpClient.SendAsync(requestMessage);
                    var paragraphResponse = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Failed to get random paragraph. Status code: {response.StatusCode}");
                    }

                    // Deserialize paragraph
                    var paragraphData = System.Text.Json.JsonSerializer.Deserialize<ParagraphResponse>(paragraphResponse);
                    if (paragraphData == null)
                    {
                        throw new Exception("Failed to deserialize paragraph data");
                    }

                    Console.WriteLine($"[INFO] Fetched paragraph ID: {paragraphData.id}");

                    // Get questions for this paragraph
                    requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/paragraph/{paragraphData.id}/questions/");
                    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    response = await _httpClient.SendAsync(requestMessage);
                    var questionsResponse = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Failed to get questions. Status code: {response.StatusCode}");
                    }

                    // Deserialize questions
                    var questionsData = System.Text.Json.JsonSerializer.Deserialize<QuestionsResponse>(questionsResponse);
                    if (questionsData == null)
                    {
                        throw new Exception("Failed to deserialize questions data");
                    }

                    // Check if paragraph has questions
                    if (questionsData.results == null || questionsData.results.Count == 0)
                    {
                        Console.WriteLine($"[WARNING] Paragraph ID {paragraphData.id} has no questions. Retrying...");
                        continue; // Try again with a new paragraph
                    }

                    Console.WriteLine($"[SUCCESS] Found paragraph with {questionsData.results.Count} questions");

                    // Convert API questions to your game Question format
                    var gameQuestions = questionsData.results.Select(q => new Question
                    {
                        questionTitle = q.text,
                        answers = q.answers.Select(a => new Answer
                        {
                            answerText = a.text,
                            is_correct = a.is_correct
                        }).ToList()
                    }).ToList();

                    // Set correct answers and hide is_correct flags
                    foreach (var q in gameQuestions)
                    {
                        var correctAnswer = q.answers.FirstOrDefault(a => a.is_correct);
                        if (correctAnswer != null)
                        {
                            q.correctAnswer = correctAnswer.answerText;
                        }

                        foreach (var a in q.answers)
                        {
                            a.is_correct = false; // hide correctness
                        }
                    }

                    return (paragraphData.text, gameQuestions);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Error in GetParagraphAndQuestionsAsync (Attempt {attempts}): {ex.Message}");

                    // If it's the last attempt, throw the exception
                    if (attempts >= maxRetries)
                    {
                        throw new Exception($"Failed to fetch paragraph with questions after {maxRetries} attempts", ex);
                    }

                    // Wait a bit before retrying to avoid hammering the API
                    await Task.Delay(250);
                }
            }

            // This should never be reached, but just in case
            throw new Exception($"Failed to fetch paragraph with questions after {maxRetries} attempts");
        }

        private class ParagraphResponse
        {
            public int id { get; set; }
            public string text { get; set; } = string.Empty;
        }

        private class QuestionsResponse
        {
            public int count { get; set; }
            public string? next { get; set; }
            public string? previous { get; set; }
            public List<ApiQuestion> results { get; set; } = new List<ApiQuestion>();
        }

        private class ApiQuestion
        {
            public int id { get; set; }
            public string sub_category { get; set; } = string.Empty;
            public string text { get; set; } = string.Empty;
            public List<ApiAnswer> answers { get; set; } = new List<ApiAnswer>();
            public string difficulty { get; set; } = string.Empty;
            public string? image_url { get; set; }
            public int paragraph { get; set; }
        }

        private class ApiAnswer
        {
            public int id { get; set; }
            public string text { get; set; } = string.Empty;
            public bool is_correct { get; set; }
        }
    }
}