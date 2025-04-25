using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

using System.Text.Json.Serialization;

namespace SignalRGame.Hubs
{
    public partial class GameHub : Hub
    {
    
    // Your notifyFriends method to notify each friend in the list
    public async Task<string> NotifyFriends(string Authorization)
    {
        Console.WriteLine("entered Notify");

        string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(Authorization);
        if (userId == "error")
        {
            await Clients.Caller.SendAsync("Error", "Error retrieving userId; something went wrong with the Token.");
            return null;
        }

        Console.WriteLine(userId);

        // Fetch the friends list from the service
        var friendsListJson = await _FriendsService.GetFriendsListAsync(Authorization);

        if (string.IsNullOrEmpty(friendsListJson))
        {
            return "No friends found.";
        }

        // Log the raw response for debugging purposes

        // Deserialize the JSON response into a list of Friend objects
        try
        {
            List<Friend> friendsList = JsonSerializer.Deserialize<List<Friend>>(friendsListJson);
            
            if (friendsList == null || !friendsList.Any())
            {
                return "No friends found.";
            }

            // Loop through each friend in the list
            foreach (var friend in friendsList)
            {
                int friendId = friend.friendId;  // Now friendId is an int
                string profileName = friend.profileName;  // Get the profile name
                int friendScore = friend.friendScore;  // Parse friendScore if it's a string

                Console.WriteLine($"Friend ID: {friendId}, Profile Name: {profileName}, Score: {friendScore}");

                // Check if the friend is logged in (by checking LoginRoomMapping)
                if (LoginRoomMapping.ContainsKey(friendId.ToString()))
                {
                    // Get the roomId from the LoginRoomMapping using the friend's ID
                    string roomId = LoginRoomMapping[friendId.ToString()];
                    Console.WriteLine($"Friend {profileName} is online, roomId: {roomId}");

                    // Send a message to the room (you can customize the message)
                    await Clients.Group(roomId).SendAsync("connectionStatus", new { userId = userId, status = true });
                }
            }

            return "Messages sent to all online friends.";
        }
        catch (JsonException ex)
        {
            // Handle JSON deserialization error
            Console.WriteLine($"Error deserializing friends list: {ex.Message}");
            return "Error: Failed to deserialize friends list.";
        }
    }


    public class Friend
    {
        [JsonPropertyName("friend_id")]  // Mapping snake_case to PascalCase
        public int friendId { get; set; }  // Ensure this is an int for numeric friend_id

        [JsonPropertyName("profile_name")]  // Mapping snake_case to PascalCase
        public string profileName { get; set; }

        [JsonPropertyName("friend_score")]  // Mapping snake_case to PascalCase
        public int friendScore { get; set; }  // Change this from string to int
    }

    }
}
