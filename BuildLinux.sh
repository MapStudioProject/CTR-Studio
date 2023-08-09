#!/bin/sh

cd "CTR Studio"
dotnet build --runtime linux-x64 --self-contained

cd "bin/Debug/net6.0"
cp -R -T "Plugins/net6.0" "linux-x64/Plugins"

read -p "Press any key to continue..."