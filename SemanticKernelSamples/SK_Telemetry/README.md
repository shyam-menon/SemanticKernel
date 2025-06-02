# Semantic Kernel Telemetry Example

This project demonstrates how to implement OpenTelemetry with Microsoft's Semantic Kernel.

## Prerequisites

- .NET 8.0 SDK
- Azure OpenAI API access
- Environment variables:
  - `AZURE_API_KEY`: Your Azure OpenAI API key
  - `AZURE_ENDPOINT`: Your Azure OpenAI endpoint URL

## Setup

1. Clone the repository
2. Set the required environment variables
3. Run the project:

dotnet run


## Features

- OpenTelemetry integration with Semantic Kernel
- Tracing, Metrics, and Logging
- Console exporter for all telemetry data
- Sensitive data collection enabled for debugging

## Telemetry Components

- Traces: Track execution flow
- Metrics: Monitor performance
- Logs: Capture detailed information

## License

MIT