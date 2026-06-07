FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY src/HairyPaws.Api/HairyPaws.Api.csproj src/HairyPaws.Api/
COPY src/HairyPaws.Application/HairyPaws.Application.csproj src/HairyPaws.Application/
COPY src/HairyPaws.Contracts/HairyPaws.Contracts.csproj src/HairyPaws.Contracts/
COPY src/HairyPaws.Domain/HairyPaws.Domain.csproj src/HairyPaws.Domain/
COPY src/HairyPaws.Infrastructure/HairyPaws.Infrastructure.csproj src/HairyPaws.Infrastructure/

RUN dotnet restore src/HairyPaws.Api/HairyPaws.Api.csproj

COPY src/ src/

RUN dotnet build src/HairyPaws.Api/HairyPaws.Api.csproj -c Release --no-restore
RUN dotnet publish src/HairyPaws.Api/HairyPaws.Api.csproj -c Release -o /app/publish --no-build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "HairyPaws.Api.dll"]
