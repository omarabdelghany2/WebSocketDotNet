using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {        
        private static void selectQuestionsCategory(string roomId)
        {
            // Directly creating a list of questions for the room
            var roomQuestions = new List<Question>
            {
                new Question
                {
                    QuestionTitle = "What is the capital of France?",
                    Answers = new[] { "Paris", "London", "Berlin", "Madrid" },
                    CorrectAnswer = "Paris"
                },
                new Question
                {
                    QuestionTitle = "What is 2 + 2?",
                    Answers = new[] { "3", "4", "5", "6" },
                    CorrectAnswer = "4"
                },
                new Question
                {
                    QuestionTitle = "What is the largest planet in our Solar System?",
                    Answers = new[] { "Earth", "Mars", "Jupiter", "Venus" },
                    CorrectAnswer = "Jupiter"
                }
            };

            // Add the questions list directly to the dictionary for the room
            RoomToQuestions.TryAdd(roomId, roomQuestions);
        }
    }
}



//it will take list of strings that it will be The catrogries
// it will get request from nekla to take the questions
//it will add this questions to RoomToquestion map