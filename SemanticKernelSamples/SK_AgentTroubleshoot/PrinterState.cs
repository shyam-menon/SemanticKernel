using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SK_AgentTroubleshoot
{// Class to maintain printer state
    public class PrinterState
    {
        public int PaperLevel { get; set; } = 45;
        public int TonerLevel { get; set; } = 30;
        public bool IsConnected { get; set; } = true;
        public string LastError { get; set; } = "None";
        public int QueuedJobs { get; set; } = 2;
    }
}
