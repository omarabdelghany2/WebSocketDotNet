using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;
using SignalRGame.Services;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
    
    
        public async Task StartGame(string token, string roomId, List<string> questionsCategories)
        {
            Console.WriteLine("StartGame method called.");
            Console.WriteLine(questionsCategories);
            await _GetQuestions.GetQuestionsResponseAsync("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ0b2tlbl90eXBlIjoiYWNjZXNzIiwiZXhwIjoxNzM0MjA0NjU4LCJpYXQiOjE3MzQyMDEwNTgsImp0aSI6ImFjYjAzOWM4MWYwNDRkMGFiZWRkMzJjMWNlOWM1NmRkIiwidXNlcl9pZCI6NCwiZW1haWwiOiJzc0BnbWFpbC5jb20ifQ.a__QQouAHFjm6bA8f3CaPjWB9l65b1T8Svso4TvtUyg", questionsCategories);
            
            if (!TokenToUserId.TryGetValue(token, out var userId))
            {
                await Clients.Caller.SendAsync("Error", "Invalid token.");
                return;
            }

            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("Error", "Room does not exist.");
                return;
            }

            if (room.Host.UserId != userId)
            {
                await Clients.Caller.SendAsync("Error", "Only the host can start the game.");
                return;
            }

            // Notify participants about their teams
            foreach (var player in room.Participants)
            {
                await Clients.Group(roomId).SendAsync("PlayerTeam", player.UserId, player.Team);
            }

            // Notify participants that the game has started
            await Clients.Group(roomId).SendAsync("GameStarted");

            // Run the question-sending process in the background
                    _ = Task.Run(() =>
                    _gameService.SendingQuestions(roomId, RoomToQuestions, RoomToCurrentQuestion));
        }
    }
}