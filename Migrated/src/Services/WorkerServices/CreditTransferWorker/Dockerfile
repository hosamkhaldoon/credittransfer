# Credit Transfer Worker Service Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY ["CreditTransfer.Modern.sln", "."]

# Copy project files
COPY ["src/Services/WorkerServices/CreditTransferWorker/CreditTransfer.Services.WorkerService.csproj", "src/Services/WorkerServices/CreditTransferWorker/"]
COPY ["src/Core/Domain/CreditTransfer.Core.Domain.csproj", "src/Core/Domain/"]
COPY ["src/Core/Application/CreditTransfer.Core.Application.csproj", "src/Core/Application/"]
COPY ["src/Infrastructure/CreditTransfer.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Core/Authentication/CreditTransfer.Core.Authentication.csproj", "src/Core/Authentication/"]

# Restore dependencies
RUN dotnet restore "src/Services/WorkerServices/CreditTransferWorker/CreditTransfer.Services.WorkerService.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/Services/WorkerServices/CreditTransferWorker"
RUN dotnet build "CreditTransfer.Services.WorkerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CreditTransfer.Services.WorkerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CreditTransfer.Services.WorkerService.dll"] 