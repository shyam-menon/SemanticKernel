using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SK_AgentsNRD.Plugins
{
    /// <summary>
    /// Plugin that contains Splunk log service functions
    /// </summary>
    public class SplunkPlugin
    {
        private readonly ILogger<SplunkPlugin> _logger;
        private readonly Dictionary<string, List<LogEntry>> _deviceLogs;

        public SplunkPlugin(ILogger<SplunkPlugin> logger)
        {
            _logger = logger;
            _deviceLogs = new Dictionary<string, List<LogEntry>>();
            
            // Initialize with some test logs
            InitializeTestLogs();
        }

        private void InitializeTestLogs()
        {
            // Device with expired credentials
            _deviceLogs["DEV001"] = new List<LogEntry>
            {
                new LogEntry
                {
                    DeviceId = "DEV001",
                    Timestamp = DateTime.Now.AddDays(-4).AddHours(1),
                    LogLevel = "ERROR",
                    Message = "Authentication failed: Credentials expired"
                },
                new LogEntry
                {
                    DeviceId = "DEV001",
                    Timestamp = DateTime.Now.AddDays(-4),
                    LogLevel = "INFO",
                    Message = "Last successful data collection"
                },
                new LogEntry
                {
                    DeviceId = "DEV001",
                    Timestamp = DateTime.Now.AddDays(-4).AddHours(-1),
                    LogLevel = "INFO",
                    Message = "Device check-in successful"
                }
            };

            // Device with corrupted credentials
            _deviceLogs["DEV002"] = new List<LogEntry>
            {
                new LogEntry
                {
                    DeviceId = "DEV002",
                    Timestamp = DateTime.Now.AddDays(-5).AddHours(2),
                    LogLevel = "ERROR",
                    Message = "Certificate validation error: Corrupted certificate"
                },
                new LogEntry
                {
                    DeviceId = "DEV002",
                    Timestamp = DateTime.Now.AddDays(-5),
                    LogLevel = "INFO",
                    Message = "Last successful data collection"
                },
                new LogEntry
                {
                    DeviceId = "DEV002",
                    Timestamp = DateTime.Now.AddDays(-5).AddHours(-2),
                    LogLevel = "WARNING",
                    Message = "Certificate approaching expiration"
                }
            };

            // Device with missing credentials
            _deviceLogs["DEV003"] = new List<LogEntry>
            {
                new LogEntry
                {
                    DeviceId = "DEV003",
                    Timestamp = DateTime.Now.AddDays(-3).AddHours(1),
                    LogLevel = "ERROR",
                    Message = "Credentials not found: Missing authentication data"
                },
                new LogEntry
                {
                    DeviceId = "DEV003",
                    Timestamp = DateTime.Now.AddDays(-3),
                    LogLevel = "INFO",
                    Message = "Last successful data collection"
                },
                new LogEntry
                {
                    DeviceId = "DEV003",
                    Timestamp = DateTime.Now.AddDays(-3).AddHours(-2),
                    LogLevel = "INFO",
                    Message = "System reboot completed"
                }
            };

            // Device with valid credentials but other issues
            _deviceLogs["DEV004"] = new List<LogEntry>
            {
                new LogEntry
                {
                    DeviceId = "DEV004",
                    Timestamp = DateTime.Now.AddDays(-3).AddHours(1),
                    LogLevel = "ERROR",
                    Message = "Network connectivity issue: Unable to reach server"
                },
                new LogEntry
                {
                    DeviceId = "DEV004",
                    Timestamp = DateTime.Now.AddDays(-3),
                    LogLevel = "INFO",
                    Message = "Last successful data collection"
                },
                new LogEntry
                {
                    DeviceId = "DEV004",
                    Timestamp = DateTime.Now.AddDays(-3).AddHours(-3),
                    LogLevel = "INFO",
                    Message = "Authentication successful"
                }
            };

            // Healthy device
            _deviceLogs["DEV005"] = new List<LogEntry>
            {
                new LogEntry
                {
                    DeviceId = "DEV005",
                    Timestamp = DateTime.Now.AddHours(-2),
                    LogLevel = "INFO",
                    Message = "Data collection completed successfully"
                },
                new LogEntry
                {
                    DeviceId = "DEV005",
                    Timestamp = DateTime.Now.AddHours(-12),
                    LogLevel = "INFO",
                    Message = "Data collection completed successfully"
                },
                new LogEntry
                {
                    DeviceId = "DEV005",
                    Timestamp = DateTime.Now.AddDays(-1),
                    LogLevel = "INFO",
                    Message = "Authentication successful"
                }
            };
        }

        [KernelFunction, Description("Retrieves logs for a device from Splunk")]
        public string GetDeviceLogs(string deviceId, int days = 7)
        {
            _logger.LogInformation($"Function called: GetDeviceLogs for device {deviceId}, days: {days}");
            
            if (!_deviceLogs.ContainsKey(deviceId))
            {
                var errorMessage = $"No logs found for device {deviceId}";
                _logger.LogWarning(errorMessage);
                return errorMessage;
            }

            var cutoffDate = DateTime.Now.AddDays(-days);
            var relevantLogs = _deviceLogs[deviceId]
                .Where(log => log.Timestamp >= cutoffDate)
                .OrderByDescending(log => log.Timestamp)
                .ToList();

            if (!relevantLogs.Any())
            {
                var noLogsMessage = $"No logs found for device {deviceId} in the past {days} days";
                _logger.LogInformation(noLogsMessage);
                return noLogsMessage;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Logs for device {deviceId} from the past {days} days:");
            sb.AppendLine();

            foreach (var log in relevantLogs)
            {
                sb.AppendLine($"[{log.Timestamp}] [{log.LogLevel}] {log.Message}");
            }

            var result = sb.ToString();
            _logger.LogInformation($"Retrieved {relevantLogs.Count} logs for device {deviceId}");
            return result;
        }

        [KernelFunction, Description("Verifies data collection for a device")]
        public bool VerifyDataCollection(string deviceId)
        {
            _logger.LogInformation($"Function called: VerifyDataCollection for device {deviceId}");
            
            if (!_deviceLogs.ContainsKey(deviceId))
            {
                _logger.LogWarning($"No logs found for device {deviceId}");
                return false;
            }

            // Check if there are any data collection logs in the past day
            var cutoffDate = DateTime.Now.AddDays(-1);
            var hasRecentData = _deviceLogs[deviceId]
                .Any(log => log.Timestamp >= cutoffDate && 
                           (log.Message.Contains("data collection") || 
                            log.Message.Contains("Data collection")));

            _logger.LogInformation($"Data collection verification for device {deviceId}: {hasRecentData}");
            return hasRecentData;
        }

        [KernelFunction, Description("Performs a manual data collection for a device")]
        public string PerformManualCollection(string deviceId)
        {
            _logger.LogInformation($"Function called: PerformManualCollection for device {deviceId}");
            
            if (!_deviceLogs.ContainsKey(deviceId))
            {
                _deviceLogs[deviceId] = new List<LogEntry>();
            }

            // Add a new log entry for the manual collection
            var newLog = new LogEntry
            {
                DeviceId = deviceId,
                Timestamp = DateTime.Now,
                LogLevel = "INFO",
                Message = "Manual data collection initiated"
            };
            
            _deviceLogs[deviceId].Add(newLog);

            // Simulate success or failure based on the device ID
            // For demonstration purposes, we'll make it succeed for all devices except DEV004
            if (deviceId == "DEV004")
            {
                var errorLog = new LogEntry
                {
                    DeviceId = deviceId,
                    Timestamp = DateTime.Now.AddSeconds(30),
                    LogLevel = "ERROR",
                    Message = "Manual data collection failed: Network connectivity issue"
                };
                
                _deviceLogs[deviceId].Add(errorLog);
                
                _logger.LogWarning($"Manual data collection failed for device {deviceId}");
                return $"Manual data collection failed for device {deviceId}. Error: Network connectivity issue";
            }
            else
            {
                var successLog = new LogEntry
                {
                    DeviceId = deviceId,
                    Timestamp = DateTime.Now.AddSeconds(30),
                    LogLevel = "INFO",
                    Message = "Manual data collection completed successfully"
                };
                
                _deviceLogs[deviceId].Add(successLog);
                
                _logger.LogInformation($"Manual data collection succeeded for device {deviceId}");
                return $"Manual data collection completed successfully for device {deviceId}";
            }
        }
    }

    /// <summary>
    /// Represents a log entry in the Splunk system
    /// </summary>
    public class LogEntry
    {
        public string DeviceId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string LogLevel { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
