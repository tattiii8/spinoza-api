FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# 必要に応じてポート番号を変更してください
ENV ASPNETCORE_URLS=http://0.0.0.0:5005
EXPOSE 5005

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# リポジトリ全体をコピー
COPY . .

# ルートからの相対パスで復元を実行
RUN dotnet restore "spinoza-notify/spinoza-notify.csproj"

# ビルド実行
WORKDIR "/src/spinoza-notify"
RUN dotnet build "spinoza-notify.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "spinoza-notify.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "spinoza-notify.dll"]