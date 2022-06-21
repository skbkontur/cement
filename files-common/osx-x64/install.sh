#!/bin/bash

rm -rf ~/.cement
rm -rf ~/bin/dotnet

mkdir ~/bin

cp -R ../../dotnet ~/.cement
cp -R ../../dotnet ~/bin/dotnet

cp -R ~/bin/dotnet/linux-x64/cm ~/bin/dotnet/cm.exe

rm -rf ~/bin/dotnet/linux-x64/cm

chmod +x ./cm

./cm reinstall

chmod +x ~/bin/cm
chmod +x ~/bin/dotnet/cm.exe