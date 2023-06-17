FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["LunarMods/LunarMods.csproj", "LunarMods/"]
RUN dotnet restore "LunarMods/LunarMods.csproj"
COPY . .
WORKDIR "/src/LunarMods"
RUN dotnet build "LunarMods.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LunarMods.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LunarMods.dll"]
