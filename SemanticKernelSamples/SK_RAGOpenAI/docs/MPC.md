# Managed Print Central (MPC)

## Role
- MPC is HP's primary Partner Experience Platform for Indirect Quoting, Business Enablement and Contract Onboarding
- Designed to address MPS market segments that Direct MPS didn't cover
- Supports multiple business models including:
  - CMPS
  - PMPS
  - MP Flex
  - Resell
  - Agent

## Core Functionality

### HP Business Enablement
- Program Onboarding
- Country Onboarding
- Partner Onboarding
- Creation, Import and Management of Catalogs
- Catalog OPGs

### Sales Journey
1. Pre-Quote Functions
   - Set Sales Defaults
   - Identify Prospect
   - Lookup Organizations
   - MSD Id Configuration
   - Sold To, Ship To, Bill

2. Assessment/Design
   - Discovery
   - Current State Fleets and Usage Details
   - Current State Cost
   - Future Fleet Design
   - Device Configuration

3. Quote and Price
   - Quote Creation
   - Pricing
   - Special Pricing
   - Add-Ons/Renewals

4. Additional Functions
   - Scenarios/Versions
   - Proposal Docs
   - Deal Win

### Post-Sales Journey
- Customer Onboard
- Contract Onboarding Workflow
- Device Onboarding
- Solutions Onboarding
- User Onboarding

## Technology Stack

### Frontend
- Angular for single-page applications
- Separate applications for different business models (PMPS, SMPS, etc.)

### Backend
- .NET Core for API hosts

### Databases
- PostgreSQL
- SQL Server
- Redis for caching

### Cloud Infrastructure
- AWS Services:
  - SNS/SQS for messaging
  - ECS for container orchestration
  - Amazon RDS for database hosting
  - CloudFront and S3 for UI and artifact distribution

### Integration Services
- WS02 for integration services

## Key Integrations

### Core System Integrations
1. Deal Management & Sales
   - HP Dynamics
   - Apollo
   - MS4
   - PriceHub
   - Deal Configuration Service

2. Device Management
   - DART
   - TMC (Transition Management Central)
   - DCC (Device Configuration and Control)
   - CDAX
   - CDCA

3. Service Management
   - ITSM ServiceNow
   - Broker
   - S4
   - BRIM
   - CC/CM

4. Data Services
   - Master Data Service
   - Enablement Service
   - ARC
   - BIRD (Business Intelligence)

## External APIs

### TMC APIs
- Project Management APIs
  - GET api/v1/TMC/Project
  - POST api/v1/TMC/Device
  - POST api/v1/TMC/Software
  - PUT api/v1/TMC/Project
  - POST api/v1/TMC/OnboardContract

### ITSM APIs
- Service Management APIs
  - PUT api/v1/ITSM/CustomHeaders
  - PUT api/v1/ITSM/Opportunity

### Device Configuration APIs
- POST api/v1/DC/AddCustomer
- POST api/v1/DC/UpdatePresaleDCCustomer
- POST api/v1/DC/SaveFleetPreferences
- GET api/v1/DC/CustomerFleetView

### Enterprise Catalog APIs
- GET api/v1/EC/EnterpriseCatalog
- POST api/v1/EC/DealService

## Infrastructure Details

### Security
- Authentication through HP ID
- Authorization using service-specific tokens
- API key and secret management for service access

### Environment Structure
- Separate environments for:
  - Development
  - Integration/Testing
  - Staging
  - Production

### Deployment
- Containerized services using ECS
- CloudFront for content delivery
- S3 for static assets and artifacts

### Data Flow Architecture
- Event-driven architecture using SNS/SQS
- RESTful APIs for service communication
- GraphQL for specific data services

### Monitoring & Telemetry
- Business intelligence through BIRD service
- Integrated logging and monitoring
- Service health tracking through ITSM