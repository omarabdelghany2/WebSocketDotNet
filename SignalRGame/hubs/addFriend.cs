using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task<bool> AddFriend(addFriendRequest request)
        {
            string authorization = request.token;
            
            // Get friend ID from profile name and token
            int? friendId = await _userIdFromProfileNameService.GetUserIdFromProfileNameAsync(authorization, request.profileName);

            // Guard clause: If friendId is 0, return false immediately
            if (friendId == 0)
            {
                await Clients.Caller.SendAsync("addFriendRequestFailed", 
                    new { error = true, errorMessage = "Friend ID not found for the given profile name." });
                return false;
            }
            
            Console.WriteLine(friendId);

            // Get user profile from token
            string serverResponse = await _userProfileFromTokenService.GetUserProfileAsync(authorization);
            var profile = JsonSerializer.Deserialize<UserProfile>(serverResponse);
            int userId = profile?.id ?? 0;
            int score = profile?.score ?? 0;

            // Check if userId is valid
            if (userId == 0)
            {
                await Clients.Caller.SendAsync("addFriendRequestFailed", 
                    new { error = true, errorMessage = "Error retrieving user ID; something went wrong with the token." });
                return false;
            }
            
            // Build a temporary Player object from the inviter's profile
            var inviterPlayer = new Player
            {
                userId = userId.ToString(),
                profileName = profile?.profileName ?? "",
                profileScore = profile?.score ?? 0,
                team = "Blue" // or whichever team is appropriate
            };



            // Check if the invited user is connected (i.e., in the login room)
            if (LoginRoomMapping.TryGetValue(friendId.ToString(), out var loginRoomConnectionId))
            {
                // Send an invitation to the invited user's login room
                await Clients.Group(loginRoomConnectionId).SendAsync("receivedAddFriendRequest",
                    new { userId = userId, profileName = profile?.profileName, score = score,  rank = inviterPlayer.rank, });
            }
            else
            {
                await Clients.Caller.SendAsync("addFriendRequestFailed",
                    new { error = true, errorMessage = "The invited user is not connected." });
                return false;
            }

            // Indicate success
            await Clients.Caller.SendAsync("addFriendRequestSuccess", 
                new { userId = friendId, message = "Friend request sent successfully." });
            return true;
        }
    }

    public class addFriendRequest
    {
        public string token { get; set; }
        public string profileName { get; set; }
    }
}
