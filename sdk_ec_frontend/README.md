# sdk_ec_frontend
Software Development Kit, E-Commerce project, Angular 20.3.0-Frontend

## System Requirements
- **Node.js**: v20.x or higher ([Download](https://nodejs.org/))
- **npm**: v10.x or higher (included with Node.js)
- **Docker**: Latest version ([Download](https://www.docker.com/products/docker-desktop/))
- **Docker Compose**: Included with Docker Desktop

## Prerequisites
- Docker & Docker Compose
- Backend API must be running (see `sdk_ec_backend_api`)

## Startup
```Docker must be open to start the containers
```

### HTTP Mode (Development)
```open a new terminal
docker compose -f compose.http.yaml up --build
```

### HTTPS Mode (Production-like)
```open a new terminal
docker compose -f compose.https.yaml up --build
```

## Configuration (needed for different HTTP/HTTPS-startups of the API. Default expects API to start in HTTP)
Configure backend URL in `src/environments/environment.ts`:
- HTTP Backend: `http://localhost:5139/api`
- HTTPS Backend: `https://localhost:7129/api`