using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task acceptAddFriend(acceptAddFriendRequest request)
        {
            string Authorization =request.token;
            int? friendId = await _userIdFromProfileNameService.GetUserIdFromProfileNameAsync(Authorization, request.profileName);
            // Guard clause: If friendId is 0, return false immediately
            if (friendId == 0)
            {
                await Clients.Caller.SendAsync("acceptedFriendRequest", 
                    new { userId=0,profileName="",error = true, errorMessage = "Friend ID not found for the given profile name." });
            }

            string serverResponse = await _userProfileFromTokenService.GetUserProfileAsync(Authorization);
            var profile = JsonSerializer.Deserialize<UserProfile>(serverResponse);
            int userId = profile?.id ?? 0;
            int score=profile?.score ?? 0;

            // if (userId!=0)
            // {
            //     await Clients.Caller.SendAsync("acceptedFriendRequest", new{friendId = friendId, error=true,errorMessage="Error retrieving userId; something went wrong with the Token."});
                
            // }

            // Check if the invited user is connected (i.e., is in the login room)


                                    // Fetch the online friends
            List<FriendStatus> onlineFriends = await fetchOnlineFriends(Authorization, userId.ToString());
            if (onlineFriends != null && onlineFriends.Any())
            {
                // If there are online friends, send the list to the caller
                await Clients.Caller.SendAsync("onlineFriends", onlineFriends );
                //also send this list to the accepted friend
            }
            else
            {
                // If there are no online friends or the list is null, send an empty list
                await Clients.Caller.SendAsync("onlineFriends", "" );
            }



            if (LoginRoomMapping.TryGetValue(friendId.ToString(), out var loginRoomConnectionId))
            {
                // Send an invitation to the invited user's login room (using their user ID or token)

                await Clients.Group(loginRoomConnectionId).SendAsync("acceptedFriendRequest", new{userId=userId, profileName = profile?.profileName,score=score,error = false, errorMessage=""});

            }







        }
    }


    public class acceptAddFriendRequest
    {
        public string token { get; set; }
        public string profileName { get; set; }
    }
}
