FROM ubuntu:18.04 AS ubuntu_asp_base
RUN apt-get -y update
RUN apt-get install -y wget
RUN apt-get install -y software-properties-common
RUN wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN apt-get -y update
RUN add-apt-repository universe
RUN apt-get -y update
RUN apt-get install -y apt-transport-https
RUN apt-get install -y liblttng-ust0 libcurl3 libssl1.0.0 libkrb5-3 zlib1g libicu60 libasound2
RUN apt-get -y update
ENV BIN_PATH "/usr/bin"
RUN apt-get install -y screen
ENV DOTNET_ROOT $BIN_PATH/dotnet
ENV PATH $PATH:$BIN_PATH/dotnet/
EXPOSE 80
EXPOSE 443

FROM classtranscribe/ubuntu_asp_base:latest AS dotnet_sdk_2.2
RUN wget -q https://download.visualstudio.microsoft.com/download/pr/3224f4c4-8333-4b78-b357-144f7d575ce5/ce8cb4b466bba08d7554fe0900ddc9dd/dotnet-sdk-2.2.301-linux-x64.tar.gz
RUN mkdir -p $BIN_PATH/dotnet && tar zxf dotnet-sdk-2.2.301-linux-x64.tar.gz -C $BIN_PATH/dotnet

FROM classtranscribe/ubuntu_asp_base:latest AS dotnet_sdk_3.0
RUN wget -q https://download.visualstudio.microsoft.com/download/pr/72ce4d40-9063-4a2e-a962-0bf2574f75d1/5463bb92cff4f9c76935838d1efbc757/dotnet-sdk-3.0.100-preview6-012264-linux-x64.tar.gz
RUN mkdir -p $BIN_PATH/dotnet && tar zxf dotnet-sdk-3.0.100-preview6-012264-linux-x64.tar.gz -C $BIN_PATH/dotnet