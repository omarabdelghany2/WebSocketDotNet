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



        public async Task updateNews(newsRequest request)
        
        {
                globalNews=request.news;

                Console.WriteLine("yes we entered here and we got the following");

                Console.WriteLine(globalNews);
                foreach (var entry in LoginRoomMapping)
                {
                    string roomId = entry.Value; // Get UserId from the LoginRoomMapping
                    await Clients.Group(roomId).SendAsync("news" ,new { news = globalNews });

                }

        }

        



        
    }


    // public class onlineAndOfflineUsersRequest{

    //     public string token { get; set; }

    // }


        public class newsRequest{

        public string news { get; set; }

    }
}