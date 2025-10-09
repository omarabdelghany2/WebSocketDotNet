using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;
using SignalRGame.Services;
using System.Text.Json;

using System.Text.Json.Serialization;
namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task startModeTwoGame(startGameModeTwoRequest request)
        {
            Console.WriteLine("StartGame method called.");
            string token = request.token;
            string roomId = request.roomId;



            //serilaize the questions here then add it to RoomToQuestions Dictionary
            string result = await _GetQuestions.GetQuestionsResponseModeTwoAsync(token);

            List<QuestionMillionaire> questions = JsonSerializer.Deserialize<List<QuestionMillionaire>>(result);


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

            RoomToQuestionsModeTwo[roomId]=questions;

            
            string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(token);
            if (userId == "error")
            {
                await Clients.Caller.SendAsync("gameModeTwoStarted", new{error =true, errorMessage="Invalid token."});
                
                return;
            }
            
            if (userId == "unauthorized")
            {
                await Clients.Caller.SendAsync("refresh"); // 👈 channel refresh event
                return;
            }

            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("gameModeTwoStarted", new { error = true, errorMessage = "Room does not exist." });

                return;
            }

            if (room.Host.userId != userId)
            {
                await Clients.Caller.SendAsync("gameModeTwoStarted", new{error =true, errorMessage="Only the host can start the game."});
                
                return;
            }
            // var blueTeamCount = room.Participants.Count(player => player.team == "Blue");
            // var redTeamCount = room.Participants.Count(player => player.team == "Red");

            //game started
            room.inGame=true;
            

            // if (blueTeamCount != redTeamCount)
            // {
            //     await Clients.Caller.SendAsync("gameModeTwoStarted", new{error =true,errorMessage="Teams must have an equal number of players to start the game."});
                
            //     return;
            // }

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
            await Clients.Group(roomId).SendAsync("gameModeTwoStarted",new{error =false,errorMessage="",questionsCount=questions.Count});


    

            // Run the question-sending process in the background
            _ = Task.Run(() =>
                _gameService.SendingQuestionsModeTwo(request.token,roomId, RoomToQuestionsModeTwo, RoomToCurrentQuestionModeTwo ,Rooms,UserRoomMapping));
        }
    }

    // Properly defining the startGameRequest class with access modifiers and a constructor
    public class startGameModeTwoRequest
    {
        public string token { get; set; }
        public string roomId { get; set; }

        // Constructor to initialize the properties
        public startGameModeTwoRequest(string token, string roomId)
        {
            this.token = token;
            this.roomId = roomId;
        }
    }
}
