#!/bin/bash

rm -rf ~/.cement
rm -rf ~/bin/dotnet
mkdir ~/bin
cp -R ../../dotnet ~/bin/dotnet
linux-x64/cm.exe reinstall
chmod +x ~/bin/cm
