using System.Collections.Generic;

namespace SumoLogicHangfire.Models
{
    public class LogMiningState
    {
        public int TotalMessageCount { get; set; }
        public int CurrentMessageCount { get; set; }
        public List<string> LogMessages { get; set; } = new List<string>();
    }
}
