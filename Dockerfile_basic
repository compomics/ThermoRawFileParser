################## BASE IMAGE ######################
FROM mono:latest

################## METADATA ######################
LABEL base_image="mono:latest"
LABEL version="1"
LABEL software="ThermoRawFileParser"
LABEL software.version="1.1.8"
LABEL about.summary="A software to convert Thermo RAW files to mgf and mzML"
LABEL about.home="https://github.com/compomics/ThermoRawFileParser"
LABEL about.documentation="https://github.com/compomics/ThermoRawFileParser"
LABEL about.license_file="https://github.com/compomics/ThermoRawFileParser"
LABEL about.license="SPDX:Unknown"
LABEL about.tags="Proteomics"

################## MAINTAINER ######################
MAINTAINER Niels Hulstaert <niels.hulstaert@ugent.be>
MAINTAINER Yasset PErez-Riverol <ypriverol@gmail.com>

################## INSTALLATION ######################
RUN apt-get update
RUN apt-get install -y git

WORKDIR /src
RUN git clone -b master --single-branch https://github.com/compomics/ThermoRawFileParser /src
RUN msbuild
