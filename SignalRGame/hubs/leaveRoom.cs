using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;
using System.Text.Json.Serialization;
using SignalRGame.Services;
using System.Text.Json;

namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task<string> leaveRoom(leaveRoomRequest request)
        {
            string token = request.token;
            string roomId = request.roomId;

            string serverResponse = await _userProfileFromTokenService.GetUserProfileAsync(token);
            var profile = JsonSerializer.Deserialize<UserProfile>(serverResponse);
            int userId = profile?.id ?? 0;



            if (serverResponse == "unauthorized")
            {
                await Clients.Caller.SendAsync("refresh"); // 👈 channel refresh event
                return null;
            }
            if (serverResponse == "error")
            {
                await Clients.Caller.SendAsync("playerLeft", new
                {

                    roomId = roomId,
                    userId = 0,
                    profileName = "",
                    error = true,
                    errorMessage = "Error retrieving userId; something went wrong with the Token."
                });
                return "Error: Invalid Token";
            }

            // Check if the room exists
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("playerLeft", new
                {

                    roomId = roomId,
                    userId=userId,
                    profileName=profile?.profileName,
                    error = true,
                    errorMessage = "room doesnt Exist."
                });
                return "Error: Invalid Token";
            }



            // Check if the user  in the room
            if (!room.Participants.Any(p => p.userId == userId.ToString()))
            {
                await Clients.Caller.SendAsync("playerLeft", new
                {

                    roomId = roomId,
                    userId=userId,
                    profileName=profile?.profileName,
                    error = true,
                    errorMessage = "he isn't in the room to take him out ."
                });
                return "Error: not in the room";
            }
            //get the player
            var player = room.Participants.FirstOrDefault(p => p.userId == userId.ToString());
            if (player != null)
            {
                room.Participants.Remove(player);
            }

            //check if the user is the Host of the Room
            bool isHost = room.Host.userId == userId.ToString();


            if (isHost)
            {
                // Remove old host mapping
                UserRoomMapping.TryRemove(player.userId, out _);

                room.Host = null; // Remove host
                if (room.Participants.Count > 0)
                {
                    // Assign a new host from participants
                    room.Host = room.Participants.First();
                    UserRoomMapping[room.Host.userId] = roomId;

                    Console.WriteLine($"Host left; reassigned new host: {room.Host.userId}");

                    await Clients.Group(roomId).SendAsync("hostLeft", new
                    {
                        hostId = Convert.ToInt32(player.userId),
                        team = player.team,
                        newHostId = Convert.ToInt32(room.Host.userId)
                    });
                }
                else
                {
                    // No participants left, delete the room
                    Rooms.TryRemove(roomId, out _);
                    Console.WriteLine("Room deleted as the host and participants left.");
                    await Clients.Group(roomId).SendAsync("roomDeleted");
                }
            }


            else{

                    await Clients.Group(roomId).SendAsync("playerLeft" ,new{userId=Convert.ToInt32(userId),team=player.team}); 
                    ParticipantRoomMapping.TryRemove(userId.ToString(), out _);


            }

            return "OK";
        }
    }

    // Accept invitation request model
    public class leaveRoomRequest
    {
        public string token { get; set; }
        public string roomId { get; set; }

    }
}
