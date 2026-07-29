FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /App

COPY . ./

RUN dotnet restore tms-template-net8.csproj
RUN dotnet publish tms-template-net8.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /App
COPY --from=build /App/out .

ARG ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}

ARG ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_URLS=${ASPNETCORE_URLS}

EXPOSE 8080

ENTRYPOINT ["dotnet", "tms-template-net8.dll"]
