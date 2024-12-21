using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub : Hub
    {
    

        // Your News method that will send the letter
        public async Task<string> news(string letter)
        {
            // Loop through the LoginRoomMapping (assuming it contains UserId to Room mapping)
            foreach (var entry in LoginRoomMapping)
            {
                recentNewsLetter=letter;
                string roomId = entry.Value; // Get UserId from the LoginRoomMapping
                await Clients.Group(roomId).SendAsync("news" ,new { news = letter });

            }

            // Return some result after sending the letters
            return "Letters sent to all users in the room.";
        }
    }
}
