FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5139
EXPOSE 7129

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# Copy the project file (assumes build is run from inside the Sdk_EC_Backend folder)
COPY ["Sdk_EC_Backend.csproj", "./"]
RUN dotnet restore "Sdk_EC_Backend.csproj"

# Copy the rest of the source and publish
COPY . .
RUN dotnet publish "Sdk_EC_Backend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "Sdk_EC_Backend.dll"]
