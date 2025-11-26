# SDK EC Backend API
Backend API for the E-Commerce Software Development Kit project, built with .NET 9.0 and ASP.NET Core.

## System Requirements
- **.NET SDK**: 9.0 or higher ([Download](https://dotnet.microsoft.com/download))
- **Docker**: Latest version ([Download](https://www.docker.com/products/docker-desktop/))
- **PowerShell**: 5.1 or higher (Windows) or PowerShell Core 7+ (cross-platform)

### IMPORTANT
Depending on the HTTP/HTTPS-mode, the routes have to be configured in the frontend in file (default expects API to be started in HTTP):
sdk_ec_frontend\ec_frontend\src\environments\environment.ts



## Startup
```Docker must be open to start the containers
Open a terminal
```

#### HTTP Mode (Default)

```powershell
# 1. Navigate to the project directory
cd Sdk_EC_Backend

# 2. Build the Docker image
docker build -t sdk-ec-backend:latest .

# 3. Start the container (HTTP on port 5139)
docker run --rm -p 5139:5139 --name sdk-ec-backend sdk-ec-backend:latest
```

The API will be accessible at: **http://localhost:5139**

---



#### HTTPS Mode

**One-time certificate setup (only needed once):**

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


**Start container with HTTPS:**

```powershell
# 1. Navigate to the project directory
cd Sdk_EC_Backend

# 2. Build the Docker image
docker build -t sdk-ec-backend:latest .

# 3. Start container with HTTPS
docker run --rm -p 5139:5139 -p 7129:7129 `
  -e USE_HTTPS=true `
  -e ASPNETCORE_Kestrel__Certificates__Default__Password="YourSecurePassword123" `
  -e ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx `
  -v ${env:USERPROFILE}\.aspnet\https:/https:ro `
  --name sdk-ec-backend `
  sdk-ec-backend:latest
```