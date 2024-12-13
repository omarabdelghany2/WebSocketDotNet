using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
    
    
        public async Task StartGame(string token, string roomId)
        {
            Console.WriteLine("StartGame method called.");
            
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