using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

using System.Text.Json.Serialization;

namespace SignalRGame.Models
{

public class Question
{
    [JsonPropertyName("sub_category")] // Mapping snake_case to PascalCase
    public string subCategory { get; set; } // Ensure this is an int for numeric friend_id

    [JsonPropertyName("text")] // Mapping snake_case to PascalCase
    public string questionTitle { get; set; }

    [JsonPropertyName("answers")] // Mapping snake_case to PascalCase
    public List<Answer> answers { get; set; } = new List<Answer>(); // Updated to be a list of Answer objects

    public string correctAnswer { get; set; } = string.Empty; // The correct answer
}

public class Answer
{
    [JsonPropertyName("text")]
    public string answerText { get; set; } = string.Empty;
    public bool is_correct { get; set; } = false;

}

}