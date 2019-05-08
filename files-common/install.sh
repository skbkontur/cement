#!/bin/bash

rm -rf ~/.cement
rm -rf ~/bin/dotnet
mkdir -p ~/bin/dotnet
cp -R ../dotnet ~/bin/dotnet
chmod +x ~/bin/cm
mono cm.exe reinstall
