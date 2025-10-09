namespace SignalRGame.Models
{
    public class Player
    {
        private static readonly string[] orderedRanks = 
        { 
            "Abyssal", "ICY", "Stone", "Copper", "Bronze", 
            "Iron", "Classical", "Modern", "Contemporary" 
        };

        private const int pointsPerPart = 100;
        private const int partsPerRank = 3;
        private const int pointsPerRank = pointsPerPart * partsPerRank; // 300
        private static readonly int totalBeforeAI = orderedRanks.Length * pointsPerRank; // 2700

        public string userId { get; set; } = string.Empty;
        public string profileName { get; set; } = string.Empty;
        public string team { get; set; } = "Unassigned"; // Team can be "Blue" or "Red"
        public int score { get; set; } = 0;
        public int gameScore { get; set; } = 0;
        public int profileScore { get; set; } = 0;
        public bool answered { get; set; } = false;
        public bool inGame { get; set; } = false;
        public bool isBot { get; set; } = false;  
        public bool answereCorrectModeTwo { get; set; } = false;

        // Computed property
        public string rank
        {
            get
            {

                if (score < 0)
                {
                    return "NEWBIE";
                }
                else if (score < totalBeforeAI)
                {
                    int rankIndex = score / pointsPerRank;
                    int remainderInRank = score % pointsPerRank;
                    int partIndex = remainderInRank / pointsPerPart; // 0..2
                    return $"{orderedRanks[rankIndex]} {partIndex + 1}";
                }
                else if (score < totalBeforeAI + 1000)
                {
                    return "AI";
                }
                else
                {
                    return "AI";
                }
            }
        }
    }
}
