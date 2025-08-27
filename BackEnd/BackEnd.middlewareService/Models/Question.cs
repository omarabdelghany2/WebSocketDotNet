using System.Text.Json.Serialization;
namespace BackEnd.middlewareService.Models
{

    public class Question
    {
        [JsonPropertyName("sub_category")]
        public string subCategory { get; set; }

        [JsonPropertyName("text")]
        public string questionTitle { get; set; }

        [JsonPropertyName("answers")]
        public List<Answer> answers { get; set; } = new List<Answer>();

        [JsonPropertyName("correctAnswer")]
        public string correctAnswer { get; set; } = string.Empty;
    }

    public class Answer
    {
        [JsonPropertyName("text")]
        public string answerText { get; set; } = string.Empty;

        [JsonPropertyName("is_correct")]
        public bool is_correct { get; set; } = false;
    }
}
