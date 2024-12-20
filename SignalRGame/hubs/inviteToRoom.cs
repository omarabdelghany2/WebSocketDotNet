using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task InviteToRoom(string Authorization, string roomId, string invitedUserId)
        {
            string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(Authorization);
            if (userId == "error")
            {
                await Clients.Caller.SendAsync("invitationSent", new{invitedUserId = Convert.ToInt32(invitedUserId), error=true,errorMessage="Error retrieving userId; something went wrong with the Token."});
                
            }

            // Check if the room exists
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                
                await Clients.Caller.SendAsync("invitationSent", new{invitedUserId = Convert.ToInt32(invitedUserId), error=true,errorMessage="Room does not exist."});
                return;
            }

            // Ensure the inviter is the host or a participant of the room
            if (room.Host.UserId != userId && !room.Participants.Any(p => p.UserId == userId))
            {
                await Clients.Caller.SendAsync("invitationSent", new{invitedUserId = Convert.ToInt32(invitedUserId), error=true,errorMessage="You are not a participant of this room."});
                return;
            }

            // Check if the invited user is connected (i.e., is in the login room)
            if (LoginRoomMapping.TryGetValue(invitedUserId, out var loginRoomConnectionId))
            {
                // Send an invitation to the invited user's login room (using their user ID or token)
                await Clients.Group(loginRoomConnectionId).SendAsync("roomInvitation", new{roomId=roomId, inviterUserId=Convert.ToInt32(invitedUserId)});
            }
            else
            {
                
                await Clients.Caller.SendAsync("invitationSent", new{invitedUserId = Convert.ToInt32(invitedUserId),error=true,errorMessage="The invited user is not connected."});
                return;
            }

            // Notify the inviter that the invitation was sent successfully
            await Clients.Caller.SendAsync("invitationSent", new{invitedUserId = Convert.ToInt32(invitedUserId), error=false,errorMessage=""});
        }
    }
}
