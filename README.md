# ThermoRawFileParser

Wrapper around the .net (C#) ThermoFisher ThermoRawFileReader library for running on Linux with mono (works on Windows too). It takes a thermo RAW file as input and outputs a metadata file and the spectra in 3 possible formats
* MGF: only MS2 spectra
* mzML and indexed mzML: both MS1 and MS2 spectra
* Apache Parquet: under development

RawFileReader reading tool. Copyright © 2016 by Thermo Fisher Scientific, Inc. All rights reserved

## (Linux) Requirements
[Mono](https://www.mono-project.com/download/stable/#download-lin) (install mono-complete if you encounter "assembly not found" errors).

## Usage
```
mono ThermoRawFileParser.exe -i=/home/user/data_input/raw_file.raw -o=/home/user/data_input/output/ -f=0 -g -m=0
```
For running on Windows, omit `mono`. The optional parameters only work in the -option=value format. The tool can output some RAW file metadata `-m=0|1` (0 for JSON, 1 for TXT) and the spectra file `-f=0|1|2|3` (0 for MGF, 1 for mzML, 2 for indexed mzML, 3 for Parquet) or both. Use the `-p` flag to disable the thermo native peak peacking. 

```
ThermoRawFileParser.exe --help
 usage is (use -option=value for the optional arguments):
  -h, --help                 Prints out the options.
  -i, --input=VALUE          The raw file input.
  -o, --output=VALUE         The output directory. Specify this or an output
                               file.
  -b, --output_file=VALUE    The output file. Specify this or an output
                               directory
  -f, --format=VALUE         The output format for the spectra (0 for MGF, 1
                               for mzMl, 2 for indexed mzML, 3 for Parquet)
  -m, --metadata=VALUE       The metadata output format (0 for JSON, 1 for TXT).
  -g, --gzip                 GZip the output file if this flag is specified (
                               without value).
  -p, --noPeakPicking        Don't use the peak picking provided by the native
                               thermo library (by default peak picking is
                               enabled)
  -v, --verbose              Enable verbose logging.
  -e, --ignoreInstrumentErrors
                             Ignore missing properties by the instrument.
  -u, --s3_url[=VALUE]       Optional property to write directly the data into
                               S3 Storage.
  -k, --s3_accesskeyid[=VALUE]
                             Optional key for the S3 bucket to write the file
                               output.
  -t, --s3_secretaccesskey[=VALUE]
                             Optional key for the S3 bucket to write the file
                               output.
  -n, --s3_bucketName[=VALUE]
                             S3 bucket name
```

## Download

Click [here](https://github.com/compomics/ThermoRawFileParser/releases) to go to the release page.

## Galaxy integration

ThermoRawFileParser is available in the Galaxy [ToolShed](https://toolshed.g2.bx.psu.edu/view/galaxyp/thermo_raw_file_converter/a3edda696e4d) and is deployed at the [European Galaxy Server](https://usegalaxy.eu/root?tool_id=toolshed.g2.bx.psu.edu/repos/galaxyp/thermo_raw_file_converter/thermo_raw_file_converter/).

## Build

If you want to build the project using nuget, put the ThermoFisher.CommonCore.RawFileReader.4.0.26.nupkg package in your local nuget directory.

## Logging

The default log file is `ThermoRawFileParser.log`. The log settings can be changed in `log4net.config`.

## Docker

Run ThermoRawFileParser simply with the pre-build biocontainer:

```bash
docker run -i -t -v /home/user/raw:/data_input quay.io/biocontainers/thermorawfileparser:1.1.2--0 ThermoRawFileParser.sh --help
```

### Basic docker

Use the `Dockerfile_basic` docker file to build an image. It fetches to source code from github and builds it.
```
docker build --no-cache -t thermorawparser -f Dockerfile_basic .
```
Run example:
```
docker run -v /home/user/raw:/data_input -i -t thermorawparser mono /src/bin/x64/Debug/ThermoRawFileParser.exe -i=/data_input/raw_file.raw -o=/data_input/output/ -f=0 -g -m=0
```
Create example for reusing the container:
```
docker create -v /home/user/raw:/data_input --name=rawparser -it thermorawparser
docker start rawparser
docker exec rawparser mono /src/bin/x64/Debug/ThermoRawFileParser.exe -i=/data_input/raw_file.raw -o=/data_input/output/ -f=0 -g -m=0
docker exec rawparser mono /src/bin/x64/Debug/ThermoRawFileParser.exe -i=/data_input/another_raw_file.raw -o=/data_input/output/ -f=0 -g -m=0
docker stop rawparser
```

### Biocontainers docker

Use the `Dockerfile` docker file to build an image. It fetches to source code from github and builds it.
```
docker build --no-cache -t thermorawparser .
```
Run example:
```
docker run -v /home/user/raw:/data_input -i -t --user biodocker thermorawparser mono /home/biodocker/bin/bin/x64/Debug/ThermoRawFileParser.exe -i=/data_input/raw_file.raw -o=/data_input/output/ -f=0 -g -m=0
```
or with the bash script (`ThermoRawFileParser.sh`):
```
docker run -v /home/user/raw:/data_input -i -t --user biodocker thermorawparser /bin/bash /home/biodocker/bin/ThermoRawFileParser.sh -i=/data_input/raw_file.raw -o=/data_input/output/ -f=0 -g -m=0
```
