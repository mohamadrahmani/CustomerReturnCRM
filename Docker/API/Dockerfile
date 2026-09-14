# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY CustomerReturnCRM.sln .

COPY src/CustomerReturnCRM.Domain/CustomerReturnCRM.Domain.csproj src/CustomerReturnCRM.Domain/
COPY src/CustomerReturnCRM.Application/CustomerReturnCRM.Application.csproj src/CustomerReturnCRM.Application/
COPY src/CustomerReturnCRM.Infrastructure/CustomerReturnCRM.Infrastructure.csproj src/CustomerReturnCRM.Infrastructure/
COPY src/CustomerReturnCRM.API/CustomerReturnCRM.API.csproj src/CustomerReturnCRM.API/

RUN dotnet restore CustomerReturnCRM.sln

COPY src/ src/

RUN dotnet publish src/CustomerReturnCRM.API/CustomerReturnCRM.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "CustomerReturnCRM.API.dll"]