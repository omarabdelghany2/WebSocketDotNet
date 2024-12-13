using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task InviteToRoom(string token, string roomId, string invitedUserId)
        {
            // Retrieve the inviter's user ID from the token
            if (!TokenToUserId.TryGetValue(token, out var inviterUserId))
            {
                await Clients.Caller.SendAsync("Error", "Invalid token.");
                return;
            }

            // Check if the room exists
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("Error", "Room does not exist.");
                return;
            }

            // Ensure the inviter is the host or a participant of the room
            if (room.Host.UserId != inviterUserId && !room.Participants.Any(p => p.UserId == inviterUserId))
            {
                await Clients.Caller.SendAsync("Error", "You are not a participant of this room.");
                return;
            }

            // Check if the invited user is connected (i.e., is in the login room)
            if (LoginRoomMapping.TryGetValue(invitedUserId, out var loginRoomConnectionId))
            {
                // Send an invitation to the invited user's login room (using their user ID or token)
                await Clients.Group(loginRoomConnectionId).SendAsync("RoomInvitation", roomId, inviterUserId);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "The invited user is not connected.");
                return;
            }

            // Notify the inviter that the invitation was sent successfully
            await Clients.Caller.SendAsync("InvitationSent", invitedUserId);
        }
    }
}
