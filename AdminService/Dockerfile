FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["AdminService/AdminService.csproj", "AdminService/"]
COPY ["RetailPOS.Common/RetailPOS.Common.csproj", "RetailPOS.Common/"]
COPY ["RetailPOS.Contracts/RetailPOS.Contracts.csproj", "RetailPOS.Contracts/"]
RUN dotnet restore "AdminService/AdminService.csproj"
COPY . .
WORKDIR "/src/AdminService"
RUN dotnet build "AdminService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AdminService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AdminService.dll"]
