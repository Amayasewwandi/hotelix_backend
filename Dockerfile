# Build the Hotelix ASP.NET Core application (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first for better Docker layer caching
COPY ["hotlix project.csproj", "./"]
RUN dotnet restore "hotlix project.csproj"

# Copy source and publish
COPY . .
RUN dotnet publish "hotlix project.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Render's default web-service port
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "hotlix project.dll"]
