using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task addFriend(addFriendRequest request)
        {
            string Authorization =request.token;
            int friendId=request.friendId;

            string serverResponse = await _userProfileFromTokenService.GetUserProfileAsync(Authorization);
            var profile = JsonSerializer.Deserialize<UserProfile>(serverResponse);
            int userId = profile?.id ?? 0;
            int score=profile?.score ?? 0;

            if (userId!=0)
            {
                await Clients.Caller.SendAsync("addFriendRequst", new{userId = userId, error=true,errorMessage="Error retrieving userId; something went wrong with the Token."});
                
            }

            // Check if the invited user is connected (i.e., is in the login room)
            if (LoginRoomMapping.TryGetValue(friendId.ToString(), out var loginRoomConnectionId))
            {
                // Send an invitation to the invited user's login room (using their user ID or token)
                await Clients.Group(loginRoomConnectionId).SendAsync("receivedAddFriendRequest", new{userId=userId, profileName = profile?.profileName,score=score});
            }


        }
    }


    public class addFriendRequest
    {
        public string token { get; set; }
        public int friendId { get; set; }
    }
}
