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
    /// Plugin that contains credential management functions
    /// </summary>
    public class CredentialPlugin
    {
        private readonly ILogger<CredentialPlugin> _logger;
        private readonly Dictionary<string, DeviceCredential> _credentials;

        public CredentialPlugin(ILogger<CredentialPlugin> logger)
        {
            _logger = logger;
            _credentials = new Dictionary<string, DeviceCredential>();
            
            // Initialize with some test credentials
            InitializeTestCredentials();
        }

        private void InitializeTestCredentials()
        {
            // Device with expired credentials
            _credentials["DEV001"] = new DeviceCredential
            {
                DeviceId = "DEV001",
                Status = "Expired",
                ExpirationDate = DateTime.Now.AddDays(-10),
                LastRotated = DateTime.Now.AddDays(-100),
                Type = "Certificate"
            };

            // Device with corrupted credentials
            _credentials["DEV002"] = new DeviceCredential
            {
                DeviceId = "DEV002",
                Status = "Corrupted",
                ExpirationDate = DateTime.Now.AddDays(80),
                LastRotated = DateTime.Now.AddDays(-20),
                Type = "Certificate"
            };

            // Device with missing credentials
            _credentials["DEV003"] = new DeviceCredential
            {
                DeviceId = "DEV003",
                Status = "Missing",
                ExpirationDate = null,
                LastRotated = null,
                Type = "Certificate"
            };

            // Device with valid credentials but other issues
            _credentials["DEV004"] = new DeviceCredential
            {
                DeviceId = "DEV004",
                Status = "Valid",
                ExpirationDate = DateTime.Now.AddDays(180),
                LastRotated = DateTime.Now.AddDays(-30),
                Type = "Certificate"
            };

            // Healthy device
            _credentials["DEV005"] = new DeviceCredential
            {
                DeviceId = "DEV005",
                Status = "Valid",
                ExpirationDate = DateTime.Now.AddDays(270),
                LastRotated = DateTime.Now.AddDays(-10),
                Type = "Certificate"
            };
        }

        [KernelFunction, Description("Gets the status of device credentials")]
        public string GetCredentialStatus(string deviceId)
        {
            _logger.LogInformation($"Function called: GetCredentialStatus for device {deviceId}");
            
            if (!_credentials.ContainsKey(deviceId))
            {
                var errorMessage = $"No credentials found for device {deviceId}";
                _logger.LogWarning(errorMessage);
                return errorMessage;
            }

            var credential = _credentials[deviceId];
            var sb = new StringBuilder();
            
            sb.AppendLine($"Credential Status for Device {deviceId}:");
            sb.AppendLine($"Status: {credential.Status}");
            sb.AppendLine($"Type: {credential.Type}");
            
            if (credential.ExpirationDate.HasValue)
            {
                sb.AppendLine($"Expiration Date: {credential.ExpirationDate.Value.ToShortDateString()}");
                
                if (credential.ExpirationDate.Value < DateTime.Now)
                {
                    sb.AppendLine($"Expired: Yes (Expired {(DateTime.Now - credential.ExpirationDate.Value).Days} days ago)");
                }
                else
                {
                    sb.AppendLine($"Expired: No (Expires in {(credential.ExpirationDate.Value - DateTime.Now).Days} days)");
                }
            }
            else
            {
                sb.AppendLine("Expiration Date: Not available");
            }
            
            if (credential.LastRotated.HasValue)
            {
                sb.AppendLine($"Last Rotated: {credential.LastRotated.Value.ToShortDateString()} ({(DateTime.Now - credential.LastRotated.Value).Days} days ago)");
            }
            else
            {
                sb.AppendLine("Last Rotated: Never");
            }

            var result = sb.ToString();
            _logger.LogInformation($"Retrieved credential status for device {deviceId}");
            return result;
        }

        [KernelFunction, Description("Generates new credentials for a device")]
        public string GenerateCredentials(string deviceId)
        {
            _logger.LogInformation($"Function called: GenerateCredentials for device {deviceId}");
            
            if (!_credentials.ContainsKey(deviceId))
            {
                // Create new credentials for the device
                _credentials[deviceId] = new DeviceCredential
                {
                    DeviceId = deviceId,
                    Status = "Valid",
                    ExpirationDate = DateTime.Now.AddDays(365),
                    LastRotated = DateTime.Now,
                    Type = "Certificate"
                };
                
                _logger.LogInformation($"New credentials created for device {deviceId}");
                return $"New credentials have been generated for device {deviceId}. Expiration date: {DateTime.Now.AddDays(365).ToShortDateString()}";
            }
            else
            {
                var credential = _credentials[deviceId];
                var oldStatus = credential.Status;
                
                // Update the existing credentials
                credential.Status = "Valid";
                credential.ExpirationDate = DateTime.Now.AddDays(365);
                credential.LastRotated = DateTime.Now;
                
                _logger.LogInformation($"Credentials updated for device {deviceId}");
                return $"Credentials have been regenerated for device {deviceId}. Previous status: {oldStatus}, New status: Valid, Expiration date: {DateTime.Now.AddDays(365).ToShortDateString()}";
            }
        }

        [KernelFunction, Description("Validates device credentials")]
        public bool ValidateCredentials(string deviceId)
        {
            _logger.LogInformation($"Function called: ValidateCredentials for device {deviceId}");
            
            if (!_credentials.ContainsKey(deviceId))
            {
                _logger.LogWarning($"No credentials found for device {deviceId}");
                return false;
            }

            var credential = _credentials[deviceId];
            var isValid = credential.Status == "Valid" && 
                         credential.ExpirationDate.HasValue && 
                         credential.ExpirationDate.Value > DateTime.Now;
            
            _logger.LogInformation($"Credential validation for device {deviceId}: {isValid}");
            return isValid;
        }

        [KernelFunction, Description("Rotates device credentials")]
        public string RotateCredentials(string deviceId)
        {
            _logger.LogInformation($"Function called: RotateCredentials for device {deviceId}");
            
            if (!_credentials.ContainsKey(deviceId))
            {
                var errorMessage = $"No credentials found for device {deviceId}";
                _logger.LogWarning(errorMessage);
                return errorMessage;
            }

            var credential = _credentials[deviceId];
            
            // Update the credentials
            credential.Status = "Valid";
            credential.ExpirationDate = DateTime.Now.AddDays(365);
            credential.LastRotated = DateTime.Now;
            
            _logger.LogInformation($"Credentials rotated for device {deviceId}");
            return $"Credentials have been rotated for device {deviceId}. New expiration date: {DateTime.Now.AddDays(365).ToShortDateString()}";
        }
    }

    /// <summary>
    /// Represents device credentials
    /// </summary>
    public class DeviceCredential
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpirationDate { get; set; }
        public DateTime? LastRotated { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}
