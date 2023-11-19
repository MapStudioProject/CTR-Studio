#!/bin/sh

pwd

dotnet build --runtime linux-x64 --self-contained "$(pwd)/CTR Studio"
cp -R "$(pwd)/Plugins" "$(pwd)/CTR Studio/bin/Debug/net6.0/linux-x64/"
cp -R "$(pwd)/CTR Studio/bin/Debug/net6.0/Plugins" "$(pwd)/CTR Studio/bin/Debug/net6.0/linux-x64/"

echo "Built CTR Studio correctly! Run './CTR\ Studio/bin/Debug/net6.0/linux-x64/CTR\ Studio' to launch!"
