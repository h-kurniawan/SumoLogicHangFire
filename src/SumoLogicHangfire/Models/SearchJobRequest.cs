namespace SumoLogicHangfire.Models
{
    public class SearchJobRequest
    {
        public string Query { get; set; }
        public long From { get; set; }
        public long To { get; set; }
    }
}
