#!/bin/sh

WORKDIR="$(pwd)"
echo "Working Directory: $WORKDIR"

echo "Copying CtrLibrary into bin (else build fails)..."
mkdir -p "$WORKDIR/CTR Studio/bin/Debug/net6.0/linux-x64/"
cp -R "$WORKDIR/Plugins" "$WORKDIR/CTR Studio/bin/Debug/net6.0/linux-x64/"
echo "Applying xcopy workaraound to CTR Studio.csproj"
sed -i 's|<Exec Command="xcopy.*|<Exec Command="cp ../Plugins/CtrLibrary/bin/Debug/net6.0 ../CTR\\ Studio/bin/Debug/net6.0/Plugins -r" />|' "$WORKDIR/CTR Studio/CTR Studio.csproj"

echo "Building now..."
if ! dotnet build --runtime linux-x64 --self-contained "$WORKDIR/CTR Studio"; then
    echo "Build failed! Please look at the errors provided and open an issue if you can't solve them."
    exit 1;
fi

cp -R "$WORKDIR/CTR Studio/bin/Debug/net6.0/Plugins" "$WORKDIR/CTR Studio/bin/Debug/net6.0/linux-x64/" && echo "Success! Run './CTR\\ Studio/bin/Debug/net6.0/linux-x64/CTR\\ Studio' to launch!" || echo "Something went wrong!"
