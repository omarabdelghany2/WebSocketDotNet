using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;
using SignalRGame.Services;
using System.Text.Json;

using System.Text.Json.Serialization;
namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task startGame(startGameRequest request)
        {
            Console.WriteLine("StartGame method called.");
            string token = request.token;
            string roomId = request.roomId;
            List<string> subCategories = request.subCategories;

            string result = await _GetQuestions.GetQuestionsResponseAsync(token, subCategories);




            //serilaize the questions here then add it to RoomToQuestions Dictionary

            List<Question> questions = JsonSerializer.Deserialize<List<Question>>(result);


            //check if the user is Subscribed first
            bool subscriptionResponce = await _isSubscribedService.isSubscribedAsync(token);

            if(subscriptionResponce!=true){
            // await Clients.Caller.SendAsync("roomCreated", new { roomId = "", team = "", error = true, errorMessage = "The user is not subscribed" });
            return;
            }

            if (questions != null)
            {
                foreach (var question in questions)
                {
                    var correctAnswer = question.answers.FirstOrDefault(a => a.is_correct);
                    if (correctAnswer != null)
                    {
                        question.correctAnswer = correctAnswer.answerText;
                    }

                    // Remove is_correct from all answers
                    foreach (var answer in question.answers)
                    {
                        answer.is_correct = false;
                    }
                }
            }


            //add the question to the RoomToQuestions Dictionary

             RoomToQuestions[roomId]=questions;

            
            string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(token);
            if (userId == "error")
            {
                await Clients.Caller.SendAsync("gameStarted", new{error =true, errorMessage="Invalid token."});
                
                return;
            }

            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("gameStarted", new{error =true, errorMessage="Room does not exist."});
                
                return;
            }

            if (room.Host.userId != userId)
            {
                await Clients.Caller.SendAsync("gameStarted", new{error =true, errorMessage="Only the host can start the game."});
                
                return;
            }
            var blueTeamCount = room.Participants.Count(player => player.team == "Blue");
            var redTeamCount = room.Participants.Count(player => player.team == "Red");
            room.questionTime=request.questionTime;
            

            if (blueTeamCount != redTeamCount)
            {
                await Clients.Caller.SendAsync("gameStarted", new{error =true,errorMessage="Teams must have an equal number of players to start the game."});
                
                return;
            }

            // Set inGame variable to true for all players
            foreach (var player in room.Participants)
            {
                player.inGame = true;
            }



            //sending the countdown Timer in the loading page
            for (int j = 5; j >= 0; j--)
            {
                await Clients.Group(roomId).SendAsync("loadingPage",new{timer=j});
                await Task.Delay(1000); // Wait for 1 second
            }
            // Notify participants that the game has started
            await Clients.Group(roomId).SendAsync("gameStarted",new{error =false,errorMessage="",questionsCount=questions.Count});


    

            // Run the question-sending process in the background
            _ = Task.Run(() =>
                _gameService.SendingQuestions(request.token,roomId, RoomToQuestions, RoomToCurrentQuestion ,Rooms ,request.subCategories,request.questionTime));
        }
    }

    // Properly defining the startGameRequest class with access modifiers and a constructor
    public class startGameRequest
    {
        public string token { get; set; }
        public string roomId { get; set; }
        public List<string> subCategories { get; set; }

        public int questionTime{get;set;}

        // Constructor to initialize the properties
        public startGameRequest(string token, string roomId, List<string> subCategories,int questionTime)
        {
            this.token = token;
            this.roomId = roomId;
            this.subCategories = subCategories;
            this.questionTime=questionTime;
        }
    }
}
