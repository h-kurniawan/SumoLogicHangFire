using Newtonsoft.Json;

namespace SumoLogicHangfire.Models
{
    public class LogMessageMap
    {
        [JsonProperty("_raw")]
        public string RawMessage { get; set; }
    }
}
