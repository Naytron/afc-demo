FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["src/afc-api/Afc.Api.csproj", "src/afc-api/"]
COPY ["src/afc-domain/Afc.Domain.csproj", "src/afc-domain/"]
RUN dotnet restore "src/afc-api/Afc.Api.csproj"

COPY src/ src/
RUN dotnet publish "src/afc-api/Afc.Api.csproj" --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Afc.Api.dll"]
