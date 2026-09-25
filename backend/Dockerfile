# Framework-dependent publish output is portable .NET assemblies, so it is built once on the machine
# doing the build; only the runtime image below is per-platform (avoids slow QEMU emulation for arm64).
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PtcgpTracker.slnx .
COPY src/PtcgpTracker.Api/PtcgpTracker.Api.csproj src/PtcgpTracker.Api/
RUN dotnet restore src/PtcgpTracker.Api/PtcgpTracker.Api.csproj

COPY src/PtcgpTracker.Api/ src/PtcgpTracker.Api/
RUN dotnet publish src/PtcgpTracker.Api/PtcgpTracker.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "PtcgpTracker.Api.dll"]
