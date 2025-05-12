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
    /// Plugin that contains JamC device management system functions
    /// </summary>
    public class JamCPlugin
    {
        private readonly ILogger<JamCPlugin> _logger;
        private Dictionary<string, DeviceState> _devices;

        public JamCPlugin(ILogger<JamCPlugin> logger)
        {
            _logger = logger;
            _devices = new Dictionary<string, DeviceState>();
            
            // Initialize with some test devices
            InitializeTestDevices();
        }

        private void InitializeTestDevices()
        {
            // Device with expired credentials
            _devices["DEV001"] = new DeviceState
            {
                DeviceId = "DEV001",
                IsReporting = false,
                LastReportTime = DateTime.Now.AddDays(-4),
                CredentialStatus = "Expired",
                LastError = "Authentication failure",
                DataCollectionEnabled = true,
                IsEnrolled = true
            };

            // Device with corrupted credentials
            _devices["DEV002"] = new DeviceState
            {
                DeviceId = "DEV002",
                IsReporting = false,
                LastReportTime = DateTime.Now.AddDays(-5),
                CredentialStatus = "Corrupted",
                LastError = "Certificate validation error",
                DataCollectionEnabled = true,
                IsEnrolled = true
            };

            // Device with missing credentials
            _devices["DEV003"] = new DeviceState
            {
                DeviceId = "DEV003",
                IsReporting = false,
                LastReportTime = DateTime.Now.AddDays(-3),
                CredentialStatus = "Missing",
                LastError = "Credentials not found",
                DataCollectionEnabled = true,
                IsEnrolled = true
            };

            // Device with valid credentials but other issues
            _devices["DEV004"] = new DeviceState
            {
                DeviceId = "DEV004",
                IsReporting = false,
                LastReportTime = DateTime.Now.AddDays(-3),
                CredentialStatus = "Valid",
                LastError = "Network connectivity issue",
                DataCollectionEnabled = true,
                IsEnrolled = true
            };

            // Healthy device
            _devices["DEV005"] = new DeviceState
            {
                DeviceId = "DEV005",
                IsReporting = true,
                LastReportTime = DateTime.Now.AddHours(-2),
                CredentialStatus = "Valid",
                LastError = "None",
                DataCollectionEnabled = true,
                IsEnrolled = true
            };
        }

        [KernelFunction, Description("Gets the current status of a device in JamC")]
        public string GetDeviceStatus(string deviceId)
        {
            _logger.LogInformation($"Function called: GetDeviceStatus for device {deviceId}");
            
            if (!_devices.ContainsKey(deviceId))
            {
                var errorMessage = $"Device {deviceId} not found in JamC system";
                _logger.LogWarning(errorMessage);
                return errorMessage;
            }

            var device = _devices[deviceId];
            var status = $"Device Status in JamC:\n" +
                       $"Device ID: {device.DeviceId}\n" +
                       $"Reporting Status: {(device.IsReporting ? "Active" : "Inactive")}\n" +
                       $"Last Report Time: {device.LastReportTime}\n" +
                       $"Credential Status: {device.CredentialStatus}\n" +
                       $"Last Error: {device.LastError}\n" +
                       $"Data Collection: {(device.DataCollectionEnabled ? "Enabled" : "Disabled")}\n" +
                       $"Enrollment Status: {(device.IsEnrolled ? "Enrolled" : "Not Enrolled")}";

            _logger.LogInformation($"Status retrieved for device {deviceId}");
            return status;
        }

        [KernelFunction, Description("Checks device credential status in JamC")]
        public string CheckCredentials(string deviceId)
        {
            _logger.LogInformation($"Function called: CheckCredentials for device {deviceId}");
            
            // Check if credentials have been updated by the CredentialPlugin
            if (CredentialPlugin.DeviceCredentialStatus.TryGetValue(deviceId, out var updatedStatus))
            {
                // Update the device state to reflect the current credential status
                if (_devices.ContainsKey(deviceId))
                {
                    _devices[deviceId].CredentialStatus = updatedStatus;
                    _logger.LogInformation($"Device {deviceId} credential status updated to: {updatedStatus}");
                }
                
                _logger.LogInformation($"Credential check completed for device {deviceId}: {updatedStatus}");
                return updatedStatus;
            }
            
            if (!_devices.ContainsKey(deviceId))
            {
                var errorMessage = $"Device {deviceId} not found in JamC system";
                _logger.LogWarning(errorMessage);
                return errorMessage;
            }

            var device = _devices[deviceId];
            var status = $"Credential Status for Device {deviceId}:\n" +
               $"Status: {device.CredentialStatus}\n";

            switch (device.CredentialStatus)
            {
                case "Expired":
                    status += "The device credentials have expired and need to be renewed.";
                    break;
                case "Corrupted":
                    status += "The device credentials are corrupted and need to be regenerated.";
                    break;
                case "Missing":
                    status += "The device credentials are missing and need to be created.";
                    break;
                case "Valid":
                    status += "The device credentials are valid and working correctly.";
                    break;
                default:
                    status += "Unknown credential status.";
                    break;
            }

            _logger.LogInformation($"Credential check completed for device {deviceId}: {device.CredentialStatus}");
            return status;
        }

        [KernelFunction, Description("Updates device credentials in JamC")]
        public string UpdateCredentials(string deviceId)
        {
            _logger.LogInformation($"Function called: UpdateCredentials for device {deviceId}");
            
            if (!_devices.ContainsKey(deviceId))
            {
                var errorMessage = $"Device {deviceId} not found in JamC system";
                _logger.LogWarning(errorMessage);
                return errorMessage;
            }

            var device = _devices[deviceId];
            var previousStatus = device.CredentialStatus;
            
            // Update the credentials
            device.CredentialStatus = "Valid";
            device.LastError = "None";
            
            // Update the shared credential status in CredentialPlugin
            CredentialPlugin.DeviceCredentialStatus[deviceId] = "Valid";
            _logger.LogInformation($"Updated shared credential status for device {deviceId} to Valid");
            
            var result = $"Credentials updated for Device {deviceId}:\n" +
                       $"Previous Status: {previousStatus}\n" +
                       $"Current Status: {device.CredentialStatus}\n" +
                       $"New credentials have been generated and configured on the device.";

            _logger.LogInformation($"Credentials updated for device {deviceId}");
            return result;
        }

        [KernelFunction, Description("Enrolls a device in JamC")]
        public string EnrollDevice(string deviceId)
        {
            _logger.LogInformation($"Function called: EnrollDevice for device {deviceId}");
            
            if (!_devices.ContainsKey(deviceId))
            {
                // Create a new device if it doesn't exist
                _devices[deviceId] = new DeviceState
                {
                    DeviceId = deviceId,
                    IsReporting = false,
                    LastReportTime = DateTime.Now,
                    CredentialStatus = "Valid",
                    LastError = "None",
                    DataCollectionEnabled = true,
                    IsEnrolled = true
                };
                
                _logger.LogInformation($"Device {deviceId} created and enrolled in JamC");
                return $"Device {deviceId} has been created and enrolled in JamC.";
            }
            else
            {
                var device = _devices[deviceId];
                
                if (device.IsEnrolled)
                {
                    _logger.LogInformation($"Device {deviceId} is already enrolled in JamC");
                    return $"Device {deviceId} is already enrolled in JamC.";
                }
                
                device.IsEnrolled = true;
                _logger.LogInformation($"Device {deviceId} has been enrolled in JamC");
                return $"Device {deviceId} has been enrolled in JamC.";
            }
        }
    }
}
