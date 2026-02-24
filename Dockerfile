FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY global.json ./
COPY src/MockS3/MockS3.csproj src/MockS3/

RUN dotnet restore src/MockS3/MockS3.csproj

COPY src/MockS3 src/MockS3/

RUN dotnet publish src/MockS3/MockS3.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:9090
EXPOSE 9090
ENTRYPOINT ["dotnet", "MockS3.dll"]
