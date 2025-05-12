using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SK_AgentsNRD
{
    /// <summary>
    /// Class to maintain device state for NRD (Non-Reporting Device) monitoring
    /// </summary>
    public class DeviceState
    {
        /// <summary>
        /// Unique identifier for the device
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether the device is currently reporting data
        /// </summary>
        public bool IsReporting { get; set; } = false;
        
        /// <summary>
        /// Last time the device reported data
        /// </summary>
        public DateTime LastReportTime { get; set; } = DateTime.Now.AddDays(-4); // Default to 4 days ago to simulate NRD
        
        /// <summary>
        /// Status of the device credentials (Valid, Expired, Corrupted, Missing)
        /// </summary>
        public string CredentialStatus { get; set; } = "Expired"; // Default to expired credentials
        
        /// <summary>
        /// Last error message reported by the device
        /// </summary>
        public string LastError { get; set; } = "Authentication failure";
        
        /// <summary>
        /// Whether data collection is enabled for the device
        /// </summary>
        public bool DataCollectionEnabled { get; set; } = true;
        
        /// <summary>
        /// Enrollment status in the management system (e.g., JamC)
        /// </summary>
        public bool IsEnrolled { get; set; } = true;
    }
}
