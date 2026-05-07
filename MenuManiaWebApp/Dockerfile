FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["MenuManiaWebApp/MenuManiaWebApp.csproj", "MenuManiaWebApp/"]
RUN dotnet restore "./MenuManiaWebApp/MenuManiaWebApp.csproj"

COPY . .
WORKDIR "/src/MenuManiaWebApp"
RUN dotnet publish "./MenuManiaWebApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MenuManiaWebApp.dll"]