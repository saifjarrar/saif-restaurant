FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY saifRestaurantApi/saifRestaurantApi.csproj saifRestaurantApi/
RUN dotnet restore saifRestaurantApi/saifRestaurantApi.csproj

COPY saifRestaurantApi/ saifRestaurantApi/
RUN dotnet publish saifRestaurantApi/saifRestaurantApi.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
USER $APP_UID
ENTRYPOINT ["dotnet", "saifRestaurantApi.dll"]
