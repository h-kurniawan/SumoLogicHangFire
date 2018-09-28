using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SumoLogicHangfire.Models
{
    public class TraceLog
    {
        public Guid Id { get; set; }
        public string Log { get; set; }
        public string ComponentName { get; set; }
        public bool HasException { get; set; }
        public string TraceId { get; set; }
        public Guid SearchJobId { get; set; }
    }
}
