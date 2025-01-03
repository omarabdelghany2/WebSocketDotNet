using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task onlineAndOfflineUsers()
        
        {
            

            // On the server, ensure you are calling onlineAndOfflineUsers


            int numberOfRooms = LoginRoomMapping.Count; // Get the total number of rooms
            await Clients.All.SendAsync("ReceiveOnlineAndOfflineUsers", new { numberOfUsers = numberOfRooms });


        }
    }


    public class dashbooardRequest{

        public string token { get; set; }

    }
}