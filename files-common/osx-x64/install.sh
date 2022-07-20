#!/bin/bash

rm -rf ~/.cement
rm -rf ~/bin/dotnet

mkdir ~/bin

cp -R ../../dotnet ~/.cement
cp -R ../../dotnet ~/bin/dotnet

cp -R ~/bin/dotnet/osx-x64/cm ~/bin/dotnet/cm.exe

rm -rf ~/bin/dotnet/osx-x64/cm

chmod +x ./cm

./cm reinstall

chmod -R +rwx ~/.cement
chmod +x ~/bin/cm
chmod +x ~/bin/dotnet/cm.exe
