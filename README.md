# RetailPOS - Advanced Microservices Point of Sale System

RetailPOS is a modern, distributed POS system designed for high scalability and intelligent store management. It features a microservices architecture, real-time data processing, and an AI-powered assistant for store insights.

## 🚀 Quick Start

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 9.0+ SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)

### Run with Docker (Recommended)
To start the entire ecosystem including databases and messaging:
```powershell
docker-compose up -d
```
Access the system at:
- **Frontend UI**: [http://localhost:4200](http://localhost:4200)
- **API Gateway**: [http://localhost:5000](http://localhost:5000)

### Manual Setup (Development)
Run the automated script to launch all services locally:
```powershell
.\run-all.ps1
```

## 📖 Documentation
- **[Software Requirements Specification (SRS)](docs/SRS.md)**: Detailed project overview, modules, and architecture.
- **Microservices Flow**: View our service interaction logic in the SRS.

## 🛠 Tech Stack
- **Backend**: .NET 9/10, Ocelot Gateway, MassTransit, RabbitMQ.
- **Frontend**: Angular 18+, Tailwind CSS.
- **AI**: Google Gemini API.
- **Database**: SQL Server, Redis.
- **Observability**: Serilog, Seq.

## 👥 Default Admin Account
- **Email**: `admin@gmail.com`
- **Password**: `Admin@123`
