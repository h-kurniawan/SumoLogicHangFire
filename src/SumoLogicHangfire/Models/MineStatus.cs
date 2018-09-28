using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SumoLogicHangfire.Models
{
    public class MineStatus
    {
        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("message_count")]
        public int MessageCount { get; set; }
    }
}
