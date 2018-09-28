using System;
using System.Runtime.Serialization;

namespace SumoLogicHangfire.Models
{
    public class MineLogRequest
    {
        [DataMember(Name = "query")]
        public string Query { get; set; }

        [DataMember(Name = "from")]
        public DateTimeOffset From { get; set; }

        [DataMember(Name = "to")]
        public DateTimeOffset To { get; set; }
    }
}
