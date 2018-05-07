# ThermoRawFileParser

Wrapper around the .net (C#) ThermoFisher ThermoRawFileReader library for running on Linux with mono. It takes a thermo RAW file as input and outputs a metadata file and the MS2 spectra (centroided) in MGF format.

RawFileReader reading tool. Copyright © 2016 by Thermo Fisher Scientific, Inc. All rights reserved

## Usage
The optional parameters only work in the -option=value format. The metadata file is only created when the `-m` is specified. 

```
ThermoRawFileParser.exe usage (use -option=value for the optional arguments)
  -h, --help                 Prints out the options.
  -i, --input=VALUE          The raw file input.
  -o, --output=VALUE         The metadata and mgf output directory.
  -m, --metadata             Write the metadata output file if this flag is
                               specified (without value).
  -c, --collection[=VALUE]   The optional collection identifier (PXD identifier
                               for example).
  -r, --run[=VALUE]          The optional mass spectrometry run name used in
                               the spectrum title. The RAW file name will be
                               used if not specified.
  -s, --subfolder[=VALUE]    Optional, to disambiguate instances where the same
                               collection has 2 or more MS runs with the same
                               name.
```

## Build

If you want to build the project using nuget, put the ThermoFisher.CommonCore.RawFileReader.4.0.26.nupkg package in your local nuget directory.

## Logging

The default log file is `ThermoRawFileParser.log`. The log settings can be changed in `log4net.config`.

## Docker

Use the docker file to build an image. It fetches to source code from github and builds it.
```
docker build --no-cache -t thermorawparser .
```
Run example:
```
docker run -v /home/user/raw:/data_input -i -t --user biodocker thermorawparser mono /home/biodocker/bin/bin/Debug/ThermoRawFileParser.exe -i=/data_input/raw_file.raw -o=/data_input/output/ -m -c=PXD00001
```
or with the bash script (`ThermoRawFileParser.sh`):
```
docker run -v /home/user/raw:/data_input -i -t --user biodocker thermorawparser /bin/bash /home/biodocker/bin/ThermoRawFileParser.sh -i=/data_input/raw_file.raw -o=/data_input/output/ -m -c=PXD00001
```

