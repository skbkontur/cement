#!/bin/bash

rm -rf ~/.cement
rm -rf ~/bin/dotnet
cp -R ../dotnet ~/bin/dotnet 
mono cm.exe reinstall
