using SignalRGame.Models;  // Import the namespace where Room and Player are defined
using Microsoft.AspNetCore.SignalR;


namespace SignalRGame.Hubs
{
    public partial class GameHub
    {        
        public string? GetUserIdFromToken(string token)
        {
            return TokenToUserId.TryGetValue(token, out var userId) ? userId : null;
        }
    }
}


//get request from neklo to take the user id and  the input is the token