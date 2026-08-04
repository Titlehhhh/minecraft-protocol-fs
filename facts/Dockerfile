FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY . .

# -m:1 avoids race condition in Directory.Build.targets when parallel projects
# write to the same Generated/*.generated.cs file simultaneously
RUN dotnet publish src/McpServer/McpServer.csproj -c Release -o /out -m:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /out .
COPY --from=build /app/minecraft-data/data minecraft-data/data

# artifacts is written at runtime (generated packet files) — mount as volume
VOLUME /app/artifacts

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "McpServer.dll"]
