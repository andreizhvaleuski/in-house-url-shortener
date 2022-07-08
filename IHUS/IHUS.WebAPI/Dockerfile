FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS='http://*:80'
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["IHUS.WebAPI/IHUS.WebAPI.csproj", "IHUS.WebAPI/"]
COPY ["IHUS.Infrastructure/IHUS.Database.csproj", "IHUS.Infrastructure/"]
COPY ["IHUS.Domain/IHUS.Domain.csproj", "IHUS.Domain/"]
RUN dotnet restore "IHUS.WebAPI/IHUS.WebAPI.csproj"
COPY . .
WORKDIR "/src/IHUS.WebAPI"
RUN dotnet build "IHUS.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IHUS.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IHUS.WebAPI.dll"]
