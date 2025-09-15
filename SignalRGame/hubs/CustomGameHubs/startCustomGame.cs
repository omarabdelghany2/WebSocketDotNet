using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using SignalRGame.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task startCustomGame(startCustomGameRequest request)
        {
            Console.WriteLine("StartCustomGame method called.");

            string token = request.token;
            string roomId = request.roomId;
            int questionTime = request.questionTime;
            int customRoomId = request.customRoomId;

            string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(token);
            if (userId == "error")
            {
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "Invalid token." });
                return;
            }

            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "Room does not exist." });
                return;
            }

            if (room.Host.userId != userId)
            {
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "Only the host can start the game." });
                return;
            }

            bool subscriptionResponse = await _isSubscribedService.isSubscribedAsync(token);
            if (!subscriptionResponse)
            {
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "The user is not subscribed." });
                return;
            }

            // Fetch questions from API by customRoomId
            var questions = await _customRoomsService.GetQuestionsForRoomAsync(token, Convert.ToInt32(userId), customRoomId);
            if (questions == null || questions.Count == 0)
            {
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "No questions available for this room." });
                return;
            }

            // Preprocess questions
            foreach (var question in questions)
            {
                var correctAnswer = question.answers.FirstOrDefault(a => a.is_correct);
                if (correctAnswer != null)
                {
                    question.correctAnswer = correctAnswer.answerText;
                }

                foreach (var answer in question.answers)
                {
                    answer.is_correct = false;
                }
            }

            RoomToQuestions[roomId] = questions;
            room.questionTime = questionTime;
            room.inGame = true;

            var blueTeamCount = room.Participants.Count(player => player.team == "Blue");
            var redTeamCount = room.Participants.Count(player => player.team == "Red");

            if (blueTeamCount != redTeamCount)
            {
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "Teams must have an equal number of players to start the game." });
                return;
            }

            foreach (var player in room.Participants)
            {
                player.inGame = true;
            }

            // Countdown
            for (int j = 5; j >= 0; j--)
            {
                await Clients.Group(roomId).SendAsync("loadingPage", new { timer = j });
                await Task.Delay(1000);
            }

            await Clients.Group(roomId).SendAsync("gameStarted", new { error = false, errorMessage = "", questionsCount = questions.Count });

            // Start sending questions
            _ = Task.Run(() =>
                _gameService.SendingCustomQuestions(token, roomId, RoomToQuestions, RoomToCurrentQuestion, Rooms, questionTime, UserRoomMapping));
        }

        public class startCustomGameRequest
        {
            public string token { get; set; }
            public string roomId { get; set; }
            public int questionTime { get; set; }
            public int customRoomId { get; set; }

            public startCustomGameRequest(string token, string roomId, int questionTime, int customRoomId)
            {
                this.token = token;
                this.roomId = roomId;
                this.questionTime = questionTime;
                this.customRoomId = customRoomId;
            }
        }
    }
}
