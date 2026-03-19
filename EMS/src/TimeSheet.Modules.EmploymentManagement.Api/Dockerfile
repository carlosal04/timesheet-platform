FROM mcr.microsoft.com/dotnet/sdk@sha256:2b0e46d490f5b53a8dc07fbf636cdf5b90796878a256e1ce5b441e8d9675c5f4 AS build
WORKDIR /src

COPY . .
RUN dotnet restore TimeSheet.Modules.EmploymentManagement.slnx --configfile NuGet.Config
RUN dotnet publish src/TimeSheet.Modules.EmploymentManagement.Api/TimeSheet.Modules.EmploymentManagement.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet@sha256:49dce27611d8fe4fbe50483ea9438abd18ccb0198d0737af231335244d0c9b94 AS final
WORKDIR /app

RUN apk add --no-cache wget

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 CMD wget -qO- http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "TimeSheet.Modules.EmploymentManagement.Api.dll"]
