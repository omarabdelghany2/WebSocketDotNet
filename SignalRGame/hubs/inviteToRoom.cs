using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;



namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task inviteToRoom(inviteToRoomRequest request)
        {
            string Authorization =request.token;
            string roomId=request.roomId;
            int invitedUserId=request.invitedUserId;

   
            string serverResponse = await _userProfileFromTokenService.GetUserProfileAsync(Authorization);
            var profile = JsonSerializer.Deserialize<UserProfile>(serverResponse);
            int userId = profile?.id ?? 0;
            string profileName=profile?.profileName;

            if (serverResponse == "error")
            {
                await Clients.Caller.SendAsync("invitationSent", new{invitedUserId = invitedUserId, error=true,errorMessage="Error retrieving profile; something went wrong with the Token."});
                
            }

            if (serverResponse == "unauthorized")
            {
                await Clients.Caller.SendAsync("refresh"); // 👈 channel refresh event
                return;
            }

            //check if the user is Subscribed first
            bool subscriptionResponce = await _isSubscribedService.isSubscribedAsync(Authorization);

            if(subscriptionResponce!=true){
            // await Clients.Caller.SendAsync("roomCreated", new { roomId = "", team = "", error = true, errorMessage = "The user is not subscribed" });
            return;
            }

            
            // Check if the room exists
            if (!Rooms.TryGetValue(roomId, out var room)) 
            {
                
                await Clients.Caller.SendAsync("invitationSent", new{invitedUserId = invitedUserId, error=true,errorMessage="Room does not exist."});
                return;
            }

            // Ensure the inviter is the host or a participant of the room
            if (Convert.ToInt32(room.Host.userId) != userId && !room.Participants.Any(p => Convert.ToInt32(p.userId) == userId))
            {
                await Clients.Caller.SendAsync("invitationSent", new{invitedUserId = invitedUserId, error=true,errorMessage="You are not a participant of this room."});
                return;
            }


            var inviterPlayer = new Player
            {
                userId = userId.ToString(),
                profileName = profile?.profileName ?? "",
                profileScore = profile?.score ?? 0,
                team = "Blue" // or whichever team is appropriate
            };
            

            // Check if the invited user is connected (i.e., is in the login room)
            if (LoginRoomMapping.TryGetValue(invitedUserId.ToString(), out var loginRoomConnectionId))
            {
                // Send an invitation to the invited user's login room (using their user ID or token)
                await Clients.Group(loginRoomConnectionId).SendAsync("roomInvitation", new
                {
                    roomId = roomId,
                    inviterUserId = userId,
                    profileName = profileName,
                    rank = inviterPlayer.rank,
                    mode = room.Mode // 👈 include Mode from the Room model
                });
            }
            else
            {

                await Clients.Caller.SendAsync("invitationSent", new { invitedUserId = invitedUserId, error = true, errorMessage = "The invited user is not connected." });
                return;
            }

                // Notify the inviter that the invitation was sent successfully
                await Clients.Caller.SendAsync("invitationSent", new{invitedUserId = invitedUserId, error=false,errorMessage=""});
        }
    }


    public class inviteToRoomRequest
    {
        public string token { get; set; }
        public string roomId { get; set; }

        public int invitedUserId { get; set; }
    }
}
