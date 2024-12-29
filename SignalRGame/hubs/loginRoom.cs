using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;
using SignalRGame.Services;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task loginRoom(string Authorization)
        {
            // Check if the Token is empty or something
            if (string.IsNullOrEmpty(Authorization))
            {
                await Clients.Caller.SendAsync("Error", "Token is required.");
                return;
            }

            // Call the GetUserIdFromTokenAsync function to fetch the userId
            string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(Authorization);
            if (userId == "error")
            {
                await Clients.Caller.SendAsync("Error", "Error retrieving userId; something went wrong with the Token.");
                return;
            }

            // Update or add the user to UserIdToConnectionId map
            UserIdToConnectionId[userId] = Context.ConnectionId;
            Console.WriteLine(Context.ConnectionId);

            string roomId;

            // Check if the user already has a login room
            if (LoginRoomMapping.TryGetValue(userId, out var existingRoomId))
            {
                roomId = existingRoomId;

                // Update the SignalR group membership
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, existingRoomId);
                await Groups.AddToGroupAsync(Context.ConnectionId, existingRoomId);

                // Notify the caller that the room was updated
                await Clients.Caller.SendAsync("loginRoom", new { roomId = roomId });
                await Clients.Caller.SendAsync("news", new { news = "recentNewsLetter recentNewsLetter recentNewsLetter recentNewsLetter" });
                List<FriendStatus> onlineFriends = await fetchOnlineFriends(Authorization, userId);
                if (onlineFriends != null && onlineFriends.Any())
                {
                    // If there are online friends, send the list to the caller
                    await Clients.Caller.SendAsync("onlineFriends", new { onlineFriends = onlineFriends });
                }
                else
                {
                    // If there are no online friends or the list is null, send an empty list
                    await Clients.Caller.SendAsync("onlineFriends", new { onlineFriends = new List<FriendStatus>() });
                }
            }
            else
            {
                // Generate a new login room ID
                roomId = $"login-{Guid.NewGuid()}";

                // Create the login room with only the host
                Player host = new Player { userId = userId };

                var room = new Room
                {
                    RoomId = roomId,
                    Host = host
                };

                LoginRooms[roomId] = room; // Save the room in the global Rooms dictionary
                LoginRoomMapping[userId] = roomId;

                // Add the user to the SignalR group
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

                // Notify the caller that the room was created
                await Clients.Caller.SendAsync("loginRoom", new { roomId = roomId });
                await Clients.Caller.SendAsync("news", new { news = "recentNewsLetter recentNewsLetter recentNewsLetter recentNewsLetter" });

                // Fetch the online friends
                List<FriendStatus> onlineFriends = await fetchOnlineFriends(Authorization, userId);
                if (onlineFriends != null && onlineFriends.Any())
                {
                    // If there are online friends, send the list to the caller
                    await Clients.Caller.SendAsync("onlineFriends", onlineFriends );
                }
                else
                {
                    // If there are no online friends or the list is null, send an empty list
                    await Clients.Caller.SendAsync("onlineFriends", "" );
                }
            }
        }

        private async Task<List<FriendStatus>> fetchOnlineFriends(string Authorization, string userId)
        {
            // Fetch the friends list from the service
            var friendsListJson = await _FriendsService.GetFriendsListAsync(Authorization);

            if (string.IsNullOrEmpty(friendsListJson))
            {
                return null;
            }

            // Log the raw response for debugging purposes
            Console.WriteLine($"Received friendsListJson: {friendsListJson}");

            // Deserialize the JSON response into a list of Friend objects
            try
            {
                List<Friend> friendsList = JsonSerializer.Deserialize<List<Friend>>(friendsListJson);

                if (friendsList == null || !friendsList.Any())
                {
                    return new List<FriendStatus>();  // Return an empty list if no friends are found
                }

                // Loop through each friend in the list
                List<FriendStatus> onlineFriends = new List<FriendStatus>();
                foreach (var friend in friendsList)
                {
                    Console.WriteLine($"Friend ID: {friend.friendId}, Profile Name: {friend.profileName}, Score: {friend.friendScore}");

                    // Check if the friend is logged in (by checking LoginRoomMapping)
                    if (LoginRoomMapping.ContainsKey(friend.friendId.ToString()))
                    {
                        onlineFriends.Add(new FriendStatus
                        {
                            friendId = friend.friendId,
                            profileName = friend.profileName
                        });
                    }
                }
                return onlineFriends;
            }
            catch (JsonException ex)
            {
                // Handle JSON deserialization error
                Console.WriteLine($"Error deserializing friends list: {ex.Message}");
                return null;
            }
        }

        // Define the Friend model


        // Define the FriendStatus model
        public class FriendStatus
        {
            public int friendId { get; set; }
            public string profileName { get; set; }
        }
    }
}
