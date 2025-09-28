namespace SignalRGame.Models
{
    public class Room
    {
        public string RoomId { get; set; } = string.Empty;
        public Player Host { get; set; } = new Player(); // Host is a Player with a team
        public List<Player> Participants { get; set; } = new List<Player>(); // List of Players (each with a team)
        public int blueTeamScore=0;
        public int redTeamScore=0;
        public int blueTeamRoundScore=0;
        public int redTeamRoundScore=0;

        public int questionTime=0;
                // ✅ Add Mode
        public string Mode { get; set; } = "default";

        public int answersCount=0;
        public bool publicRoom=false;
        public bool inGame=false;
    }
}