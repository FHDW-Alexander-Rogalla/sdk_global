# SDK E-Commerce Platform

Full-stack e-commerce application with .NET 9.0 backend API, Angular 20.3.0 frontend, and Supabase database.

## 📋 Tech Stack

- **Frontend**: Angular 20.3.0
- **Backend API**: .NET 9.0 SDK / ASP.NET Core
- **Database**: Supabase
- **Containerization**: Docker & Docker Compose

## 🔧 System Requirements

- **Docker Desktop**: Latest version ([Download](https://www.docker.com/products/docker-desktop/))
- **PowerShell**: 5.1 or higher (Windows) or PowerShell Core 7+ (cross-platform)
- **.NET SDK**: 9.0 or higher (for HTTPS mode) ([Download](https://dotnet.microsoft.com/download))

## 🚀 Quick Start

### Prerequisites

1. **Install Docker Desktop** and ensure it's running
2. **Clone this repository**:
   ```powershell
   git clone https://github.com/FHDW-Alexander-Rogalla/sdk_global.git
   cd sdk_global
   ```

### Choose Your Mode

The application supports two startup modes:

#### 🌐 HTTP Mode (Development - Recommended for local development)

**Simple one-command startup:**

```powershell
docker compose -f docker-compose.http.yaml up --build
```

**Access the application:**
- Frontend: http://localhost:4200
- Backend API: http://localhost:5139
- Backend API configuration in frontend: `http://localhost:5139/api` (default)

---

#### 🔒 HTTPS Mode (Production-like)

**One-time certificate setup (required before first HTTPS start):**

```powershell
# 1. Remove old certificate (if exists)
dotnet dev-certs https --clean

# 2. Create directory for certificate
New-Item -ItemType Directory -Force -Path ${env:USERPROFILE}\.aspnet\https

# 3. Create and export new development certificate
dotnet dev-certs https -ep ${env:USERPROFILE}\.aspnet\https\aspnetapp.pfx -p YourSecurePassword123

# 4. Trust the certificate (Windows/macOS)
dotnet dev-certs https --trust
```

**Start the application with HTTPS:**
- Backend API configuration: Update `sdk_ec_frontend/ec_frontend/src/environments/environment.ts` to use `https://localhost:7129/api`
```powershell
docker compose -f docker-compose.https.yaml up --build
```

**Access the application:**
- Frontend: http://localhost:4200 (frontend serves via HTTP but connects to HTTPS backend)
- Backend API: https://localhost:7129

---

## ⚙️ Configuration

### Backend API URL Configuration

The frontend needs to be configured to match your chosen mode (HTTP/HTTPS):

**File location**: `sdk_ec_frontend/ec_frontend/src/environments/environment.ts`

**HTTP Mode (default):**
```typescript
export const environment = {
  apiUrl: 'http://localhost:5139/api'
};
```

**HTTPS Mode:**
```typescript
export const environment = {
  apiUrl: 'https://localhost:7129/api'
};
```

### Database Configuration

Configure Supabase settings in `sdk_ec_backend_api/Sdk_EC_Backend/appsettings.json`:

```json
{
  "Supabase": {
    "Url": "YOUR_SUPABASE_URL",
    "Key": "YOUR_SUPABASE_KEY"
  }
}
```

## 📦 Project Structure

```
sdk_global/
├── docker-compose.http.yaml       # HTTP mode orchestration
├── docker-compose.https.yaml      # HTTPS mode orchestration
├── README.md                      # This file
├── sdk_ec_backend_api/            # .NET Backend API
│   ├── README.md
│   └── Sdk_EC_Backend/
│       ├── Controllers/
│       ├── Models/
│       ├── Services/
│       └── Dockerfile
└── sdk_ec_frontend/               # Angular Frontend
    ├── README.md
    ├── Dockerfile.http
    ├── Dockerfile.https
    └── ec_frontend/
        └── src/
```

## 👥 Test Accounts

### Admin Account (Full access to admin features)
- **Email**: admin@gmail.com
- **Password**: adminadmin

### Regular Customer Account
- **Email**: foo@gmail.com
- **Password**: foofoofoo

## 🛠️ Common Commands

### Stop all containers
```powershell
docker compose -f docker-compose.http.yaml down
# or
docker compose -f docker-compose.https.yaml down
```

### View logs
```powershell
# All services
docker compose -f docker-compose.http.yaml logs -f

# Specific service
docker compose -f docker-compose.http.yaml logs -f backend
docker compose -f docker-compose.http.yaml logs -f frontend
```

### Rebuild without cache
```powershell
docker compose -f docker-compose.http.yaml build --no-cache
docker compose -f docker-compose.http.yaml up
```

### Remove all containers, networks, and volumes
```powershell
docker compose -f docker-compose.http.yaml down -v
```

## 🐛 Troubleshooting

### Port already in use
If you get a "port already in use" error:
```powershell
# Find and stop the process using the port
netstat -ano | findstr :<PORT_NUMBER>
taskkill /PID <PID> /F
```

### Certificate issues (HTTPS mode)
If you encounter certificate errors:
1. Ensure you've completed the one-time certificate setup
2. Verify the certificate exists: `Test-Path ${env:USERPROFILE}\.aspnet\https\aspnetapp.pfx`
3. Re-run the certificate setup commands
4. Restart Docker Desktop

### Frontend can't connect to backend
1. Check that both containers are running: `docker ps`
2. Verify the `environment.ts` file has the correct API URL
3. Check backend logs: `docker compose -f docker-compose.http.yaml logs backend`

### Docker build fails
1. Ensure Docker Desktop is running
2. Clear Docker cache: `docker system prune -a`
3. Rebuild: `docker compose -f docker-compose.http.yaml build --no-cache`

## 📚 Additional Resources

- [Backend API Documentation](./sdk_ec_backend_api/README.md)
- [Frontend Documentation](./sdk_ec_frontend/README.md)
- [Docker Documentation](https://docs.docker.com/)
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [Angular Documentation](https://angular.io/docs)

## 📝 License

This project is part of the FHDW coursework.

## 🤝 Contributing

This is an educational project for FHDW. For questions or issues, please contact the repository owner.

---

**Happy coding! 🎉**
