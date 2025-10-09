
using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;
using System.Text.Json.Serialization;
using SignalRGame.Services;
using System.Text.Json;
namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task<string> reconnectGame(reconnectGameRequest request)
        {
            string token = request.token;
            string roomId = request.roomId;
            bool choice =request.choice;

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
                await Clients.Caller.SendAsync("joinedRoom", new
                {
                    roomId = roomId,
                    error = true,
                    errorMessage = "Error retrieving userId; something went wrong with the Token."
                });
                return "Error: Invalid Token";
            }

            //check if the user is Subscribed first
            bool subscriptionResponce = await _isSubscribedService.isSubscribedAsync(token);

            if (subscriptionResponce != true)
            {
                return "User is not subscribed";  // Return a meaningful string here.
            }

            if(choice){
                
                // Check if the room exists
                if (!Rooms.TryGetValue(roomId, out var room))
                {
                    await Clients.Caller.SendAsync("joinedRoom", new
                    {
                        roomId = roomId,
                        error = true,
                        errorMessage = "Room does not exist."
                    });
                    return "Error: Room does not exist";
                }

                // // Send to all group members except the caller
                // await Clients.GroupExcept(roomId, Context.ConnectionId).SendAsync("playerJoined", new 
                // {
                //     userId = Convert.ToInt32(userId),
                //     team = team,
                //     profileName = profile?.profileName,
                //     score = profile?.score
                // });

                // Add the user to the SignalR group for the room
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

                await Clients.Caller.SendAsync("reconnectedGame", new
                {
                    red = room.Participants
                        .Where(p => p.team == "Red")
                        .ToDictionary(p => Convert.ToInt32(p.userId), p => new
                        {
                            userId = Convert.ToInt32(p.userId),
                            profileName = p.profileName,  // Assuming `p.Score` exists for the player's score
                            rank = p?.rank,
                            isHost = p.userId == room.Host.userId,// Checking if the participant is the host
                            score = p.score,
                            gameScore = p.gameScore,
                            isMe = p.userId == userId.ToString() // Check if this player is the caller
                        }),
                    blue = room.Participants
                        .Where(p => p.team == "Blue")
                        .ToDictionary(p => Convert.ToInt32(p.userId), p => new
                        {
                            userId = Convert.ToInt32(p.userId),
                            profileName = p.profileName,  // Assuming `p.Score` exists for the player's score
                            rank = p?.rank,
                            isHost = p.userId == room.Host.userId,// Checking if the participant is the host
                            score = p.score,
                            gameScore = p.gameScore,
                            isMe = p.userId == userId.ToString() // Check if this player is the caller
                        }),
                    roomId = roomId,
                    blueScore = room.blueTeamScore,
                    redScore = room.redTeamScore,
                    error = false,
                    errorMessage = "",
                    gameTeam = room.Participants.FirstOrDefault(p => p.userId == userId.ToString())?.team ?? "Unassigned",
                    mode = room.Mode


                });
                return "joined Room succesfully";


            }
            else{





                // Check if the user has an assigned room
                if (UserRoomMapping.TryRemove(userId.ToString(), out var userRoom))
                {
                    Console.WriteLine($"Removed user {userId} from UserRoomMapping (Room: {userRoom})");
                }
                
                if (ParticipantRoomMapping.TryRemove(userId.ToString(), out var participantRoom))
                {
                    Console.WriteLine($"Removed user {userId} from ParticipantRoomMapping (Room: {participantRoom})");
                }
                
                //invoke afk player  link with nekalwy url
                bool serverResponse2 = await _afkPlayerService.sendAfkPlayerToDataBase(token);

                if(serverResponse2==true){
                    Console.WriteLine("the player is noted as afk");
                    return "the player is noted as afk";
                }
                else{
                    Console.WriteLine("the player didnt noted as afk");
                    return "the player didnt noted as afk";
                }
            }


        }
    }


    public class reconnectGameRequest
    {
        public string token { get; set; }
        public string roomId { get; set; }

        public bool choice{get;set;}
    }
}
