using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task<bool> removeFriend(removeFriendRequest request)
        {
            string authorization = request.token;
            
            // Get friend ID from profile name and token
            int? friendId = await _userIdFromProfileNameService.GetUserIdFromProfileNameAsync(authorization, request.profileName);

            // Guard clause: If friendId is 0, return false immediately
            if (friendId == 0)
            {
                await Clients.Caller.SendAsync("removeFriendRequestFailed", 
                    new { error = true, errorMessage = "Friend ID not found for the given profile name." });
                return false;
            }
            
            Console.WriteLine(friendId);

            // Get user profile from token
            string serverResponse = await _userProfileFromTokenService.GetUserProfileAsync(authorization);

            if (serverResponse == "unauthorized")
            {
                await Clients.Caller.SendAsync("refresh"); // 👈 channel refresh event
                return false;
            }
            var profile = JsonSerializer.Deserialize<UserProfile>(serverResponse);
            int userId = profile?.id ?? 0;
            int score = profile?.score ?? 0;

            // Check if userId is valid
            if (userId == 0)
            {
                await Clients.Caller.SendAsync("removeFriendRequestFailed", 
                    new { error = true, errorMessage = "Error retrieving user ID; something went wrong with the token." });
                return false;
            }

            // Check if the friend user is connected (i.e., in the login room)
            if (LoginRoomMapping.TryGetValue(friendId.ToString(), out var loginRoomConnectionId))
            {
                // Send a message  to the delted user's login room
                await Clients.Group(loginRoomConnectionId).SendAsync("receivedRemoveFriendRequest", 
                    new { userId = userId});
            }


            // Indicate success
            await Clients.Caller.SendAsync("removeFriendRequestSuccess", 
                new {error=false, message = " remove Friend request sent successfully." });
            return true;
        }
    }

    public class removeFriendRequest
    {
        public string token { get; set; }
        public string profileName { get; set; }
    }
}
