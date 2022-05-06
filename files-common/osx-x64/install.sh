#!/bin/bash

rm -rf ~/.cement
rm -rf ~/bin/dotnet
mkdir ~/bin
cp -R ../dotnet ~/bin/dotnet
cm.exe reinstall
chmod +x ~/bin/cm
