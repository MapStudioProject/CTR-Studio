#!/bin/sh

cd "CTR Studio"
dotnet build --runtime linux-x64 --self-contained
read -p "Press any key to continue..."