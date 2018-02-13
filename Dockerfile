FROM mono:5.8.0.108

RUN apt-get update
RUN apt-get --yes install curl wget libunwind8 gettext apt-transport-https unzip git-core

RUN curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
RUN mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
RUN sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-jessie-prod jessie main" > /etc/apt/sources.list.d/dotnetdev.list'
RUN apt-get --yes install dotnet-sdk-2.0.0 

WORKDIR /
RUN curl -s https://api.github.com/repos/skbkontur/cement/releases/latest | grep "browser_download_url.*zip" | cut -d : -f 2,3 | tr -d \" | wget -O cement.zip -i -
RUN mkdir ./cement
RUN unzip -o cement.zip -d ./cement
RUN mono ../cement/dotnet/cm.exe init
