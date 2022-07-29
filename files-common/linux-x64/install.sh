#!/bin/bash

rm -rf $HOME/.cement
rm -rf $HOME/bin/dotnet

mkdir $HOME/bin

SCRIPT_DIR="$(dirname -- "$(readlink -f "${BASH_SOURCE}")")"

echo "Installing cement from: $SCRIPT_DIR"

echo "Copying binaries"
cp -R $SCRIPT_DIR/../../dotnet $HOME/.cement
cp -R $SCRIPT_DIR/../../dotnet $HOME/bin/dotnet
cp -R $HOME/bin/dotnet/linux-x64/cm $HOME/bin/dotnet/cm.exe

chmod -R +rw $HOME/.cement
chmod +x $HOME/bin/cm
chmod +x $HOME/bin/dotnet/cm.exe

echo "Removing installed binary"
rm -rf $HOME/bin/dotnet/linux-x64/cm

echo "Running cm reinstall"
chmod +x $SCRIPT_DIR/cm
$SCRIPT_DIR/cm reinstall

chmod -R +rw $HOME/.cement
chmod +x $HOME/bin/cm
chmod +x $HOME/bin/dotnet/cm.exe

echo "Cement is installed. It's available at '\$HOME/bin/cm'. If you want to run it from your shell add '\$HOME/bin' to your PATH"
