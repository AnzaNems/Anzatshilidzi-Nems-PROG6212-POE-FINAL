# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore the project file first so Docker can cache the restore layer.
COPY ["RaceDayMvc/RaceDayMvc.csproj", "RaceDayMvc/"]
RUN dotnet restore "RaceDayMvc/RaceDayMvc.csproj"

# Copy the rest of the source and publish a release build.
COPY . .
WORKDIR /src/RaceDayMvc
RUN dotnet publish -c Release -o /app/publish

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Kestrel listens on plain HTTP inside the container.
ENV ASPNETCORE_HTTP_PORTS=8080

# The MVC app must reach the API running ON THE HOST (outside the container).
# "host.docker.internal" resolves to the host machine from inside Docker Desktop
# on Windows - the API keeps running normally in Visual Studio on port 7123.
# The double underscore maps to the config key Api:BaseUrl.
ENV Api__BaseUrl=http://host.docker.internal:7123

ENTRYPOINT ["dotnet", "RaceDayMvc.dll"]
