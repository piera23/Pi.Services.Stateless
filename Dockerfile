FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copia file di solution e progetti per restore
COPY Pi.Services.Stateless.sln ./
COPY src/Directory.Build.props ./src/
COPY src/Pi.Common/Pi.Common.csproj ./src/Pi.Common/
COPY src/Pi.FormGenerator/Pi.FormGenerator.csproj ./src/Pi.FormGenerator/
COPY src/Pi.Workflow/Pi.Workflow.csproj ./src/Pi.Workflow/
COPY src/Pi.Documents/Pi.Documents.csproj ./src/Pi.Documents/
COPY src/Pi.Identity/Pi.Identity.csproj ./src/Pi.Identity/
COPY src/Pi.Integrations/Pi.Integrations.csproj ./src/Pi.Integrations/
COPY src/Pi.Services.Stateless.Api/Pi.Services.Stateless.Api.csproj ./src/Pi.Services.Stateless.Api/

RUN dotnet restore Pi.Services.Stateless.sln

# Copia l'intero sorgente e compila
COPY src/ ./src/
RUN dotnet publish src/Pi.Services.Stateless.Api/Pi.Services.Stateless.Api.csproj -c Release -o /out --no-restore

# Immagine runtime leggera
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Pi.Services.Stateless.Api.dll"]
