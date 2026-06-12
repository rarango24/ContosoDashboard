FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore ContosoDashboard/ContosoDashboard.csproj

RUN dotnet publish ContosoDashboard/ContosoDashboard.csproj \
    -c Release \
    -o /app/publish

FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ContosoDashboard.dll"]