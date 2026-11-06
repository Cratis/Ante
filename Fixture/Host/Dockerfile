# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble
WORKDIR /app
COPY out ./
ENTRYPOINT ["dotnet", "Ante.Fixture.Host.dll"]
