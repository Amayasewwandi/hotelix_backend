
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["hotlix project.csproj", "./"]
RUN dotnet restore "hotlix project.csproj"

COPY . .
RUN dotnet publish "hotlix project.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["sh", "-c", "dotnet 'hotlix project.dll' --urls http://0.0.0.0:${PORT:-8080}"]
