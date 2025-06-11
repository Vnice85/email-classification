FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/EmailClassification.API/EmailClassification.API.csproj", "EmailClassification.API/"]
COPY ["src/EmailClassification.Application/EmailClassification.Application.csproj", "EmailClassification.Application/"]
COPY ["src/EmailClassification.Domain/EmailClassification.Domain.csproj", "EmailClassification.Domain/"]
COPY ["src/EmailClassification.Infrastructure/EmailClassification.Infrastructure.csproj", "EmailClassification.Infrastructure/"]
RUN dotnet restore "EmailClassification.API/EmailClassification.API.csproj"
COPY src/ .
WORKDIR "/src/EmailClassification.API"
RUN dotnet build "EmailClassification.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EmailClassification.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY certs/email-classification.pfx /https/email-classification.pfx
ENTRYPOINT ["dotnet", "EmailClassification.API.dll"]
