FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["CatalogService/CatalogService.csproj", "CatalogService/"]
COPY ["RetailPOS.Common/RetailPOS.Common.csproj", "RetailPOS.Common/"]
COPY ["RetailPOS.Contracts/RetailPOS.Contracts.csproj", "RetailPOS.Contracts/"]
RUN dotnet restore "CatalogService/CatalogService.csproj"
COPY . .
WORKDIR "/src/CatalogService"
RUN dotnet build "CatalogService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CatalogService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CatalogService.dll"]
