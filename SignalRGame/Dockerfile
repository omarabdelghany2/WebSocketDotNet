# Use the official .NET 8.0 SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app
EXPOSE 5274

# Copy everything
COPY . .

# Restore dependencies
RUN dotnet restore

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5274

# Start the application
CMD ["dotnet", "run"]