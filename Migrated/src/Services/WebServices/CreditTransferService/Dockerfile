# Credit Transfer WCF Service Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY ["CreditTransfer.Modern.sln", "."]

# Copy project files
COPY ["src/Services/WebServices/CreditTransferService/CreditTransfer.Services.WcfService.csproj", "src/Services/WebServices/CreditTransferService/"]
COPY ["src/Core/Domain/CreditTransfer.Core.Domain.csproj", "src/Core/Domain/"]
COPY ["src/Core/Application/CreditTransfer.Core.Application.csproj", "src/Core/Application/"]
COPY ["src/Infrastructure/CreditTransfer.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Core/Authentication/CreditTransfer.Core.Authentication.csproj", "src/Core/Authentication/"]

# Restore dependencies
RUN dotnet restore "src/Services/WebServices/CreditTransferService/CreditTransfer.Services.WcfService.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/Services/WebServices/CreditTransferService"
RUN dotnet build "CreditTransfer.Services.WcfService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CreditTransfer.Services.WcfService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "CreditTransfer.Services.WcfService.dll"] 