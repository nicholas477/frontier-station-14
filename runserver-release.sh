#!/bin/sh
dotnet run --project Content.Server --configuration Release --config-file=./Config/server_config_release.toml
read -p "Press enter to continue"
