using SignalRGame.Models;
using SignalRGame.Services;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;




using System.Text.Json;

using System.Text.Json.Serialization;

namespace SignalRGame.Hubs
{
    public partial class GameHub : Hub
    {
        
        public async Task startGuestGame(string roomId)
        {
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "Room does not exist." });
                return;
            }

            if (room.inGame)
            {
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "Game already started." });
                return;
            }

            room.inGame = true;
            room.questionTime = 10; // Default guest question time

            foreach (var player in room.Participants)
            {
                player.inGame = true;
            }

            // 🔹 Get questions from service
            List<Question> questions = await _guestRoomService.GetGuestRoomQuestionsAsync();

            // 🔹 Ensure correctAnswer is set & strip is_correct
            foreach (var q in questions)
            {
                var correctAnswer = q.answers.FirstOrDefault(a => a.is_correct);
                if (correctAnswer != null)
                {
                    q.correctAnswer = correctAnswer.answerText;
                }

                foreach (var a in q.answers)
                {
                    a.is_correct = false; // hide correctness info
                }
            }

            // 🔹 Save to dictionary
            RoomToQuestions[roomId] = questions;

            // Countdown
            for (int j = 5; j >= 0; j--)
            {
                await Clients.Group(roomId).SendAsync("loadingPage", new { timer = j });
                await Task.Delay(1000);
            }

            await Clients.Group(roomId).SendAsync("gameStarted", new
            {
                error = false,
                errorMessage = "",
                questionsCount = questions.Count
            });

            // Start sending questions in the background
            _ = Task.Run(() =>
                _gameService.SendingGuestQuestions("guest", roomId, RoomToQuestions, RoomToCurrentQuestion, Rooms, room.questionTime, UserRoomMapping));
        }






public class QuestionApiResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("next")]
    public string Next { get; set; }

    [JsonPropertyName("previous")]
    public string Previous { get; set; }

    [JsonPropertyName("results")]
    public List<Question> Results { get; set; }
}


    }
}
