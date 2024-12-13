FROM mcr.microsoft.com/dotnet/aspnet:8.0 as runtime
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder-base
WORKDIR /src
COPY . .

FROM builder-base AS build
RUN dotnet build "GCloud.Compute.Metadata.TestServer/GCloud.Compute.Metadata.TestServer.csproj" -c Release -o /app/build

FROM runtime as final
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "GCloud.Compute.Metadata.TestServer.dll"]
