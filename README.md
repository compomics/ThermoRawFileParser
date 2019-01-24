# ThermoRawFileParser

Wrapper around the .net (C#) ThermoFisher ThermoRawFileReader library for running on Linux with mono (works on Windows too). It takes a thermo RAW file as input and outputs a metadata file and the spectra in 3 possible formats
* MGF: only MS2 spectra
* mzML: both MS1 and MS2 spectra
* Apache Parquet: under development

RawFileReader reading tool. Copyright © 2016 by Thermo Fisher Scientific, Inc. All rights reserved

## (Linux) Requirements
[Mono](https://www.mono-project.com/download/stable/#download-lin) (install mono-complete if you encounter "assembly not found" errors).

## Usage
```
mono ThermoRawFileParser.exe -i=/home/user/data_input/raw_file.raw -o=/home/user/data_input/output/ -f=0 -g -m=0 -c=PXD00001
```
For running on Windows, omit `mono`. The optional parameters only work in the -option=value format. The tool can output some RAW file metadata `-m=0|1` (0 for JSON format, 1 for TXT format) and the spectra file `-f` or both. For the MGF format, `-p` flag is used to exclude MS2 profile mode data (the MGF files can get big when the MS2 spectra were acquired in profile mode). 

```
ThermoRawFileParser.exe usage is (use -option=value for the optional arguments):
  -h, --help                 Prints out the options.
  -i, --input=VALUE          The raw file input.
  -o, --output=VALUE         The output directory.
  -f, --format=VALUE         The output format for the spectra (0 for MGF, 1
                               for MzMl, 2 for Parquet)
  -m, --metadata=VALUE       The metadata output format (0 for JSON, 1 for TXT).
  -g, --gzip                 GZip the output file if this flag is specified (
                               without value).
  -p, --profiledata          Exclude MS2 profile data if this flag is specified
                               (without value). Only for MGF format!
  -c, --collection[=VALUE]   The optional collection identifier (PXD identifier
                               for example).
  -r, --run[=VALUE]          The optional mass spectrometry run name used in
                               the spectrum title. The RAW file name will be
                               used if not specified.
  -s, --subfolder[=VALUE]    Optional, to disambiguate instances where the same
                               collection has 2 or more MS runs with the same
                               name.
```

## Download

Click [here](https://github.com/compomics/ThermoRawFileParser/releases) to go to the release page.

## Build

If you want to build the project using nuget, put the ThermoFisher.CommonCore.RawFileReader.4.0.26.nupkg package in your local nuget directory.

## Logging

The default log file is `ThermoRawFileParser.log`. The log settings can be changed in `log4net.config`.

## Docker

### Basic docker

Use the `Dockerfile_basic` docker file to build an image. It fetches to source code from github and builds it.
```
docker build --no-cache -t thermorawparser -f Dockerfile_basic .
```
Run example:
```
docker run -v /home/user/raw:/data_input -i -t thermorawparser mono /src/bin/Debug/ThermoRawFileParser.exe -i=/data_input/raw_file.raw -o=/data_input/output/ -f=0 -g -m=0 -c=PXD00001
```
Create example for reusing the container:
```
docker create -v /home/user/raw:/data_input --name=rawparser -it thermorawparser
docker start rawparser
docker exec rawparser mono /src/bin/x64/Debug/ThermoRawFileParser.exe -i=/data_input/raw_file.raw -o=/data_input/output/ -f=0 -g -m=0 -c=PXD00001
docker exec rawparser mono /src/bin/x64/Debug/ThermoRawFileParser.exe -i=/data_input/another_raw_file.raw -o=/data_input/output/ -f=0 -g -m=0 -c=PXD00001
docker stop rawparser
```

### Biocontainers docker

Use the `Dockerfile` docker file to build an image. It fetches to source code from github and builds it.
```
docker build --no-cache -t thermorawparser .
```
Run example:
```
docker run -v /home/user/raw:/data_input -i -t --user biodocker thermorawparser mono /home/biodocker/bin/bin/x64/Debug/ThermoRawFileParser.exe -i=/data_input/raw_file.raw -o=/data_input/output/ -f=0 -g -m=0 -c=PXD00001
```
or with the bash script (`ThermoRawFileParser.sh`):
```
docker run -v /home/user/raw:/data_input -i -t --user biodocker thermorawparser /bin/bash /home/biodocker/bin/ThermoRawFileParser.sh -i=/data_input/raw_file.raw -o=/data_input/output/ -f=0 -g -m=0 -c=PXD00001
```
