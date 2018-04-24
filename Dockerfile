FROM mono:latest

RUN apt-get update
RUN apt-get install -y git
WORKDIR /src
RUN git clone  -b master  --single-branch https://github.com/compomics/ThermoRawFileParser /src
RUN msbuild

