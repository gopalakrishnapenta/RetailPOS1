FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["ReturnsService/ReturnsService.csproj", "ReturnsService/"]
COPY ["RetailPOS.Common/RetailPOS.Common.csproj", "RetailPOS.Common/"]
COPY ["RetailPOS.Contracts/RetailPOS.Contracts.csproj", "RetailPOS.Contracts/"]
RUN dotnet restore "ReturnsService/ReturnsService.csproj"
COPY . .
WORKDIR "/src/ReturnsService"
RUN dotnet build "ReturnsService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ReturnsService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReturnsService.dll"]
