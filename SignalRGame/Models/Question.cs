namespace SignalRGame.Models
{
    public class Question
    {
        public string QuestionTitle { get; set; } = string.Empty; // The question text
        public string[] Answers { get; set; } = Array.Empty<string>(); // Array of 4 possible answers
        public string CorrectAnswer { get; set; } = string.Empty; // The correct answer
    }

}