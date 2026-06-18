FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["global.json", "./"]
COPY ["HackatonFiap.Donations.sln", "./"]
COPY ["src/HackatonFiap.Donations.Domain/HackatonFiap.Donations.Domain.csproj", "src/HackatonFiap.Donations.Domain/"]
COPY ["src/HackatonFiap.Donations.Application/HackatonFiap.Donations.Application.csproj", "src/HackatonFiap.Donations.Application/"]
COPY ["src/HackatonFiap.Donations.Infrastructure/HackatonFiap.Donations.Infrastructure.csproj", "src/HackatonFiap.Donations.Infrastructure/"]
COPY ["src/HackatonFiap.Donations.API/HackatonFiap.Donations.API.csproj", "src/HackatonFiap.Donations.API/"]
RUN dotnet restore "src/HackatonFiap.Donations.API/HackatonFiap.Donations.API.csproj"
COPY . .
RUN dotnet publish "src/HackatonFiap.Donations.API/HackatonFiap.Donations.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HackatonFiap.Donations.API.dll"]
