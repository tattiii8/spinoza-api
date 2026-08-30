FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY spinoza/spinoza.csproj spinoza/
RUN dotnet restore spinoza/spinoza.csproj
COPY . .
RUN dotnet publish spinoza/spinoza.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:5004
EXPOSE 5004
ENTRYPOINT ["dotnet", "spinoza.dll"]
