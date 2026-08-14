# Use .NET 6.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["ClinicaOdontologica.csproj", "./"]
RUN dotnet restore "ClinicaOdontologica.csproj"

# Copy all files and build
COPY . .
WORKDIR "/src"
RUN dotnet publish "ClinicaOdontologica.csproj" -c Release -o /app/publish --no-restore

# Use .NET 6.0 runtime for running
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:${PORT}
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE ${PORT}
ENTRYPOINT ["dotnet", "ClinicaOdontologica.dll"]
