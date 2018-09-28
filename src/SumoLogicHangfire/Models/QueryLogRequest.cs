using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SumoLogicHangfire.Models
{
    public class LogRequest
    {
        public const string StatusNotStarted = "NotStarted";
        public const string StatusGatheringLogs = "GatheringLogs";
        public const string StatusDone = "Done";
        public const string StatusCancelled = "Cancelled";

        public LogRequest()
        {
            Status = StatusNotStarted;
        }

        public Guid Id { get; set; }
        public string Query { get; set; }
        public string Status { get; set; }
    }
}
