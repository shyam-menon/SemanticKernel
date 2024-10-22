using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SK_AgentTroubleshoot
{
    // Plugin that contains printer-related functions
    public class PrinterPlugin
    {
        private ILogger<PrinterPlugin> _logger;
        private PrinterState _printerState;

        public PrinterPlugin(ILogger<PrinterPlugin> logger)
        {
            _logger = logger;
            _printerState = new PrinterState();
        }

        [KernelFunction, Description("Gets the current status of the printer including paper, toner, and connection state")]
        public string GetPrinterStatus()
        {
            _logger.LogInformation("Function called: GetPrinterStatus");
            var status = $"Printer Status:\n" +
                       $"Paper Level: {_printerState.PaperLevel}%\n" +
                       $"Toner Level: {_printerState.TonerLevel}%\n" +
                       $"Connection Status: {_printerState.IsConnected}\n" +
                       $"Last Error: {_printerState.LastError}\n" +
                       $"Print Queue: {_printerState.QueuedJobs} jobs";

            _logger.LogInformation($"Status retrieved: {status}");
            return status;
        }

        [KernelFunction, Description("Checks if printer is connected to the network")]
        public bool CheckConnection()
        {
            _logger.LogInformation("Function called: CheckConnection");
            // Simulated network check with randomized results for testing
            _printerState.IsConnected = DateTime.Now.Second % 3 != 0; // Fails roughly 1/3 of the time
            _logger.LogInformation($"Connection check result: {_printerState.IsConnected}");
            return _printerState.IsConnected;
        }

        [KernelFunction, Description("Attempts to reset the printer connection")]
        public string ResetConnection()
        {
            _logger.LogInformation("Function called: ResetConnection");
            _printerState.IsConnected = true;
            _printerState.LastError = "None";
            var result = "Printer connection has been reset successfully.";
            _logger.LogInformation(result);
            return result;
        }

        [KernelFunction, Description("Checks paper level and jams")]
        public string CheckPaper()
        {
            _logger.LogInformation("Function called: CheckPaper");
            // Simulate random paper issues for testing
            _printerState.PaperLevel = new Random().Next(0, 100);
            string result;

            if (_printerState.PaperLevel < 10)
            {
                result = $"Warning: Paper level is critically low ({_printerState.PaperLevel}%). Please add paper.";
            }
            else
            {
                result = $"Paper level is adequate ({_printerState.PaperLevel}%).";
            }

            _logger.LogInformation(result);
            return result;
        }

        [KernelFunction, Description("Checks toner level")]
        public string CheckToner()
        {
            _logger.LogInformation("Function called: CheckToner");
            // Simulate random toner levels for testing
            _printerState.TonerLevel = new Random().Next(0, 100);
            string result;

            if (_printerState.TonerLevel < 15)
            {
                result = $"Warning: Toner level is low ({_printerState.TonerLevel}%). Please replace toner cartridge.";
            }
            else
            {
                result = $"Toner level is adequate ({_printerState.TonerLevel}%).";
            }

            _logger.LogInformation(result);
            return result;
        }

        [KernelFunction, Description("Clears the print queue")]
        public string ClearPrintQueue()
        {
            _logger.LogInformation("Function called: ClearPrintQueue");
            var previousJobs = _printerState.QueuedJobs;
            _printerState.QueuedJobs = 0;
            var result = $"Print queue has been cleared. Removed {previousJobs} jobs.";
            _logger.LogInformation(result);
            return result;
        }

        [KernelFunction, Description("Simulates a paper jam")]
        public string SimulatePaperJam()
        {
            _logger.LogInformation("Function called: SimulatePaperJam");
            _printerState.LastError = "Paper Jam Detected";
            var result = "Paper jam has been simulated in the printer.";
            _logger.LogInformation(result);
            return result;
        }

        [KernelFunction, Description("Clears a paper jam")]
        public string ClearPaperJam()
        {
            _logger.LogInformation("Function called: ClearPaperJam");
            if (_printerState.LastError == "Paper Jam Detected")
            {
                _printerState.LastError = "None";
                var result = "Paper jam has been cleared successfully.";
                _logger.LogInformation(result);
                return result;
            }
            var noJamResult = "No paper jam detected to clear.";
            _logger.LogInformation(noJamResult);
            return noJamResult;
        }
    }
}
