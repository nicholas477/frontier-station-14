@echo off
dotnet run --project Content.Server --configuration Release --config-file=./Config/server_config_release.toml
pause
