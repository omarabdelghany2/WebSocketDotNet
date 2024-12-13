using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task<string> AcceptInvitation(string token, string roomId, string inviterId)
        {
            // Retrieve the user ID from the token
            if (!TokenToUserId.TryGetValue(token, out var userId))
            {
                await Clients.Caller.SendAsync("Error", "Invalid token.");
                return "Error: Invalid token";
            }

            // Check if the room exists
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("Error", "Room does not exist.");
                return "Error: Room does not exist";
            }

            // Check if the inviter is a participant or the host of the room
            var inviterIsValid = room.Host.UserId == inviterId || room.Participants.Any(p => p.UserId == inviterId);
            if (!inviterIsValid)
            {
                await Clients.Caller.SendAsync("Error", "Invalid inviter.");
                return "Error: Invalid inviter";
            }

            // Check if the user is already in the room
            if (room.Participants.Any(p => p.UserId == userId))
            {
                await Clients.Caller.SendAsync("Error", "You are already in the room.");
                return "Error: Already in the room";
            }

            // Assign the user to a team (blue if fewer blue players, red otherwise)
            string team = room.Participants.Count(p => p.Team == "Blue") < room.Participants.Count(p => p.Team == "Red") ? "Blue" : "Red";
            
            // Add the user to the room
            var newPlayer = new Player { UserId = userId, Team = team };
            room.Participants.Add(newPlayer);

            // Add the user to the SignalR group for the room
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // Notify the room that a new player joined
            await Clients.Group(roomId).SendAsync("PlayerJoined", userId, team);

            // Notify the inviter that the invitation was accepted
            if (UserIdToConnectionId.TryGetValue(inviterId, out var inviterConnectionId))
            {
                await Clients.Client(inviterConnectionId).SendAsync("InvitationAccepted", roomId, userId);
            }

            return "OK";
        }
    }
}

