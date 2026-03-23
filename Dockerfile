FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./src/PlexModernMetadataProvider.Api/PlexModernMetadataProvider.Api.csproj ./src/PlexModernMetadataProvider.Api/
COPY ./tests/PlexModernMetadataProvider.Tests/PlexModernMetadataProvider.Tests.csproj ./tests/PlexModernMetadataProvider.Tests/
RUN dotnet restore ./src/PlexModernMetadataProvider.Api/PlexModernMetadataProvider.Api.csproj
COPY . .
RUN dotnet publish ./src/PlexModernMetadataProvider.Api/PlexModernMetadataProvider.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:3000
EXPOSE 3000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PlexModernMetadataProvider.Api.dll"]
