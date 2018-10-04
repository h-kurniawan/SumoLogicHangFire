using Newtonsoft.Json;

namespace SumoLogicHangfire.Models
{
    public class SearchJobResponse
    {
        public class HypermediaLink
        {
            [JsonProperty("href")]
            public string SearchLocation { get; set; }
        }

        public string Id { get; set; }
        public HypermediaLink Link { get; set; }
    }
}
