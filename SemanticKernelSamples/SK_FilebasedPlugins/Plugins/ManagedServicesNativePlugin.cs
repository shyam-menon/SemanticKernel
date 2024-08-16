using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SK_FilebasedPlugins.Plugins
{
    public sealed class ManagedServicesNativePlugin
    {
        //Lets semantic kernel know that this is a native function
        [KernelFunction, Description("Get the current status of a managed service")]
        public string GetServiceStatus(string serviceName)
        {
            // Simulated status check
            return $"The status of {serviceName} is: Operational";
        }

        [KernelFunction, Description("Calculate the monthly cost of a managed service")]
        public double CalculateServiceCost(double basePrice, int userCount)
        {
            return basePrice + userCount * 10; // Simplified cost calculation
        }
    }
}
