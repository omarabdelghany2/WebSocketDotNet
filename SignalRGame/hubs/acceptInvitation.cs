using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;
using System.Text.Json.Serialization;
using SignalRGame.Services;
using System.Text.Json;

namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task<string> acceptInvitation(acceptInvitationRequest request)
        {
            string token = request.token;
            string roomId = request.roomId;
            int inviterUserId = request.inviterUserId;

            string serverResponse = await _userProfileFromTokenService.GetUserProfileAsync(token);
            var profile = JsonSerializer.Deserialize<UserProfile>(serverResponse);
            int userId = profile?.id ?? 0;

            if (serverResponse == "error")
            {
                await Clients.Caller.SendAsync("invitationAccepted", new
                {
                    inviterUserId = inviterUserId,
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

            // Check if the room exists
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("invitationAccepted", new
                {
                    inviterUserId = inviterUserId,
                    roomId = roomId,
                    room = (dynamic)null,
                    error = true,
                    errorMessage = "Room does not exist."
                });
                return "Error: Room does not exist";
            }

            // Check if the inviter is a participant or the host of the room
            var inviterIsValid = room.Host.userId == inviterUserId.ToString() || room.Participants.Any(p => p.userId == inviterUserId.ToString());
            if (!inviterIsValid)
            {
                await Clients.Caller.SendAsync("Error", "Invalid inviter.");
                await Clients.Caller.SendAsync("invitationAccepted", new
                {
                    inviterUserId = inviterUserId,
                    roomId = roomId,
                    room = (dynamic)null,
                    error = true,
                    errorMessage = "Invalid inviter"
                });
                return "Error: Invalid inviter";
            }

            // Check if the user is already in the room
            if (room.Participants.Any(p => p.userId == userId.ToString()))
            {
                await Clients.Caller.SendAsync("invitationAccepted", new
                {
                    inviterUserId = inviterUserId,
                    roomId = roomId,
                    room = (dynamic)null,
                    error = true,
                    errorMessage = "You are already in the room."
                });
                return "Error: Already in the room";
            }

            // Assign the user to a team (blue if fewer blue players, red otherwise)
            string team = room.Participants.Count(p => p.team == "Blue") < room.Participants.Count(p => p.team == "Red") ? "Blue" : "Red";
            
            // Add the user to the room
            var newPlayer = new Player { userId = userId.ToString(), team = team,profileName=profile?.profileName,score=profile?.score ?? 0 };
            room.Participants.Add(newPlayer);

            ParticipantRoomMapping[userId.ToString()] = roomId;

            // Send to all group members except the caller
            await Clients.GroupExcept(roomId, Context.ConnectionId).SendAsync("playerJoined", new 
            {
                userId = Convert.ToInt32(userId),
                team = team,
                profileName = profile?.profileName,
                score = profile?.score
            });


            // Add the user to the SignalR group for the room
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // Send response with the blue and red teams
            await Clients.Caller.SendAsync("invitationAccepted", new
            {
                red = room.Participants
                    .Where(p => p.team == "Red")
                    .ToDictionary(p => Convert.ToInt32(p.userId), p => new
                    {
                        userId = Convert.ToInt32(p.userId),
                        profileName = p.profileName,  // Assuming `p.Score` exists for the player's score
                        isHost = p.userId == room.Host.userId,// Checking if the participant is the host
                        score=p.score,
                        isMe = p.userId == userId.ToString() // Check if this player is the caller
                    }),
                blue = room.Participants
                    .Where(p => p.team == "Blue")
                    .ToDictionary(p => Convert.ToInt32(p.userId), p => new
                    {
                        userId = Convert.ToInt32(p.userId),
                        profileName = p.profileName,  // Assuming `p.Score` exists for the player's score
                        isHost = p.userId == room.Host.userId,// Checking if the participant is the host
                        score=p.score,
                        isMe = p.userId == userId.ToString() // Check if this player is the caller
                    }),
                roomId=roomId,    
                error = false,
                errorMessage = ""
            });

            

            return "OK";
        }
    }

    // Accept invitation request model
    public class acceptInvitationRequest
    {
        public string token { get; set; }
        public string roomId { get; set; }
        public int inviterUserId { get; set; }
    }
}
