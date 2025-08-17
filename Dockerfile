# ============================
# BUILD STAGE
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the source
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# ============================
# RUNTIME STAGE
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy build output
COPY --from=build /app/publish .

# Set ASP.NET Core URL
ENV ASPNETCORE_URLS=http://+:5290

# Expose container port
EXPOSE 5290

# Run the app
ENTRYPOINT ["dotnet", "MzansiMarket.dll"]
