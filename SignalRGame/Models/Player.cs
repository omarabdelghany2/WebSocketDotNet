namespace SignalRGame.Models
{
    public class Player
    {
        public string UserId { get; set; } = string.Empty;
        public string Team { get; set; } = "Unassigned"; // Team can be "Blue" or "Red"
        public int Score{ get; set;}= 0;
    }
}
