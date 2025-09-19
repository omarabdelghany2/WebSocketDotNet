using SignalRGame.Models;
using SignalRGame.Services;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;

namespace SignalRGame.Hubs
{
    public partial class GameHub : Hub
    {
        // ---- START GUEST GAME ----
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

            // Add bots
            var bot1 = new Player { userId = "bot1", team = "Blue", profileName = "Bot 1", score = 100, isBot = true };
            var bot2 = new Player { userId = "bot2", team = "Red", profileName = "Bot 2", score = 200, isBot = true };
            var bot3 = new Player { userId = "bot3", team = "Red", profileName = "Bot 3", score = 300, isBot = true };

            room.Participants.Add(bot1);
            room.Participants.Add(bot2);
            room.Participants.Add(bot3);

            room.inGame = true;
            room.questionTime = 10; // Default guest question time

            foreach (var player in room.Participants)
            {
                player.inGame = true;
            }

            // Get default guest questions
            var questions = await _guestRoomService.GetGuestRoomQuestionsAsync();
            RoomToQuestions[roomId] = questions;

            // Countdown
            for (int j = 5; j >= 0; j--)
            {
                await Clients.Group(roomId).SendAsync("loadingPage", new { timer = j });
                await Task.Delay(1000);
            }

            await Clients.Group(roomId).SendAsync("gameStarted", new { error = false, errorMessage = "", questionsCount = questions.Count });

            // Start sending questions
            _ = Task.Run(() =>
                _gameService.SendingGuestQuestions("guest", roomId, RoomToQuestions, RoomToCurrentQuestion, Rooms, room.questionTime, UserRoomMapping));
        }
    }
}
