using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {
        public async Task switchPrivacy(SwitchPrivacyOfRoomRequest request )
        {
            string token = request.token;
            string roomId = request.roomId;
            string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(token);
            if (userId == "error")
            {
                await Clients.Caller.SendAsync("SwitchedPrivacyOfRoom", new{roomId ="" , error =true ,errorMessage="Error retrieving userId; something went wrong with the Token."});
                return;
            }
            

            if (userId == "unauthorized")
            {
                await Clients.Caller.SendAsync("refresh"); // 👈 channel refresh event
                return;
            }

            // Check if the room exists
            if (!Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("SwitchedPrivacyOfRoom", new { roomId = "", error = true, errorMessage = "Room does not exist." });
                return;
            }

            // Find the player in the room's participants
            var player = room.Participants.FirstOrDefault(p => p.userId == userId);

            if (player == null)
            {
                await Clients.Caller.SendAsync("SwitchedPrivacyOfRoom", new{roomId ="" , error =true ,errorMessage="Player not found in the room."});
                return;
            }

            // Check if the player is the host
            if (room.Host == null || room.Host.userId != userId)
            {
                await Clients.Caller.SendAsync("SwitchedPrivacyOfRoom", new 
                {
                    roomId = room.RoomId, 
                    userId = userId, 
                    error = true, 
                    errorMessage = "Only the host can switch the room's privacy."
                });
                return;
            }



            // Switch the player's team
            if (room.publicRoom == false)
            {
                room.publicRoom = true;
            }
            else
            {
                room.publicRoom = false;
            }

            Console.WriteLine(room.publicRoom);

            // Notify the room that the player has switched teams
            await Clients.Group(roomId).SendAsync("SwitchedPrivacyOfRoom", new{publicRoom = room.publicRoom  ,error = false ,errorMessage=""});

        }




        public async Task getPublicGames(getPublicGamesRequest request)
        {
            string token = request.token;
            string userId = await _userIdFromTokenService.GetUserIdFromTokenAsync(token);
            if (userId == "error")
            {
                await Clients.Caller.SendAsync("publicGames", new{roomId ="" , error =true ,errorMessage="Error retrieving userId; something went wrong with the Token."});
                return;
            }

            if (userId == "unauthorized")
            {
                await Clients.Caller.SendAsync("refresh"); // 👈 channel refresh event
                return;
            }

                var publicRooms = Rooms.Values
                .Where(r => r.publicRoom && !r.inGame) // ✅ Filter by inGame == false
                .ToList();

            //return all rooms that are public to the caller
            await Clients.Caller.SendAsync("publicGames", new{
                
            rooms= publicRooms.Select(r => new {
                roomId=r.RoomId,
                hostName=r.Host.profileName,
                participantsCount = r.Participants.Count
            }),

            error = false,
            
            errorMessage=""});

        }


    }




    public class SwitchPrivacyOfRoomRequest
    {
        public string token { get; set; }
        public string roomId { get; set; }
    }


        public class getPublicGamesRequest
    {
        public string token { get; set; }
    }

}
        