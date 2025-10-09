using SignalRGame.Models;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SignalRGame.Hubs
{
    public partial class GameHub : Hub
    {
        public async Task startMode4Game(StartMode4GameRequest request)
        {
            string token = request.token;
            string roomId = request.roomId;

            // 🔹 Get userId from token
            string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(token);
            if (userId == "error")
            {
                Console.WriteLine("down in userId");
                        await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "Invalid token." });
                        return;
            }
            


            if (userId == "unauthorized")
            {
                await Clients.Caller.SendAsync("refresh"); // 👈 channel refresh event
                return;
            }

            // 🔹 Validate room
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                Console.WriteLine("down in room");
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "Room does not exist." });
                return;
            }

            // 🔹 Check if caller is the host
            if (room.Host.userId != userId)
            {
		Console.WriteLine("down in host");
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "Only the host can start the game." });
                return;
            }


            // // 🔹 Check participants count
            // if (room.Participants == null || room.Participants.Count < 2)
            // {
            //     await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "At least 2 players are required to start the game." });
            //     return;
            // }
            if (room.inGame)
            {
                await Clients.Caller.SendAsync("gameStarted", new { error = true, errorMessage = "Game already started." });
                return;
            }

            // 🔹 Set room & players into game
            room.inGame = true;
            room.questionTime = 15; // Mode4 default question time

            foreach (var player in room.Participants)
            {
                player.inGame = true;
            }

            // Get paragraph and questions from the service
            var (comprehensionPassage, questions) = await _mode4Service.GetParagraphAndQuestionsAsync(token);

            // 🔹 Save to dictionary
            RoomToQuestions[roomId] = questions;

            // Countdown
            for (int j = 5; j >= 0; j--)
            {
                Console.WriteLine($"[SEND] loadingPage => {{ timer = {j} }}");
                await Clients.Group(roomId).SendAsync("loadingPage", new { timer = j });
                await Task.Delay(1000);
            }

            // 🔹 Changed event name from "gameStarted" → "mode4GameStarted"
            await Clients.Group(roomId).SendAsync("mode4GameStarted", new
            {
                error = false,
                errorMessage = "",
                questionsCount = questions.Count,
                passage = comprehensionPassage
            });

            // 🔹 Start sending questions in background
            // 🔹 Start sending questions in background
            _ = Task.Run(() =>
                _gameService.SendingQuestionsMode4(
                    token,
                    roomId,
                    RoomToQuestions,
                    RoomToCurrentQuestion,
                    Rooms,
                    room.questionTime,
                    UserRoomMapping
                ));



        }
    }

    public class StartMode4GameRequest
    {
        public string token { get; set; }
        public string roomId { get; set; }

        public StartMode4GameRequest(string token, string roomId)
        {
            this.token = token;
            this.roomId = roomId;
        }
    }
}
