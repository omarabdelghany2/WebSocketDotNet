using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using SignalRGame.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json.Serialization;
using System.Text.Json;


using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json.Serialization;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
    public async Task createModeTwoRoom(string Authorization)
    {
        string serverResponse = await _userProfileFromTokenService.GetUserProfileAsync(Authorization);
        var profile = JsonSerializer.Deserialize<UserProfile>(serverResponse);
        int userId = profile?.id ?? 0; // Default to 0 if profile.id is null

        if (serverResponse == "error")
        {
            await Clients.Caller.SendAsync("roomCreatedModeTwo", new { roomId = "",error = true, errorMessage = "Error retrieving userId; something went wrong with the Token." });
            Console.WriteLine("didnt get the user id");
        }



        //check if the user is Subscribed first


            bool subscriptionResponce = await _isSubscribedService.isSubscribedAsync(Authorization);

            if(subscriptionResponce!=true){
            // await Clients.Caller.SendAsync("roomCreated", new { roomId = "", team = "", error = true, errorMessage = "The user is not subscribed" });
            Console.WriteLine("subscribedd notttt ");
            return;
            }





        // Check if the user already has a room
        if (UserRoomMapping.ContainsKey(userId.ToString()))
        {
            await Clients.Caller.SendAsync("roomCreatedModeTwo", new { roomId = "",error = true, errorMessage = "The Host already has a Room" });
            Console.WriteLine("already hasaroom");
            return;
        }

        // Generate a unique room ID
        var roomId = Guid.NewGuid().ToString();

        // Create the room with the user as the host (assigned to blue team by default)
        var room = new Room
        {
            RoomId = roomId,
            Host = new Player
            {
                userId = userId.ToString(),  // Use userId as string directly
                team = "Blue",
                profileName = profile?.profileName,
                score = profile?.score ?? 0  // Default to 0 if profileScore is null
            },
            Participants = new List<Player>
            {
                new Player
                {
                    userId = userId.ToString(),  // Use userId as string directly
                    team = "Blue",
                    profileName = profile?.profileName,
                    score = profile?.score ?? 0  // Default to 0 if profileScore is null
                }
            },
            publicRoom=true  //make it private room as default
        };

        // Console.WriteLine("here i want to detectttttttttttttttttttttttttt");
        // Console.WriteLine(profile.score);
            foreach (var participant in room.Participants)
            {
                Console.WriteLine($"UserId: {participant.userId}, Team: {participant.team}, Score: {participant.score}");
            }

        // Save the room
        Rooms[roomId] = room;
        UserRoomMapping[userId.ToString()] = roomId;

        // Add the connection to the SignalR group
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        // Notify the caller that the room has been created
        Console.WriteLine(roomId);
        await Clients.Caller.SendAsync("roomCreatedModeTwo", new { roomId = roomId, error = false, errorMessage = "" });
    }

    }


}