# ThermoRawFileParser

Wrapper around the .net (C#) ThermoFisher ThermoRawFileReader library for running on Linux with mono (works on Windows too). It takes a thermo RAW file as input and outputs a metadata file and the spectra in 3 possible formats
* MGF: MS2 and MS3 spectra
* mzML and indexed mzML: both MS1, MS2 and MS3 spectra
* Apache Parquet: under development

RawFileReader reading tool. Copyright © 2016 by Thermo Fisher Scientific, Inc. All rights reserved

## (Linux) Requirements
[Mono](https://www.mono-project.com/download/stable/#download-lin) (install mono-complete if you encounter "assembly not found" errors).

## Usage
```
mono ThermoRawFileParser.exe -i=/home/user/data_input/raw_file.raw -o=/home/user/data_input/output/ -f=0 -g -m=0
```
For running on Windows, omit `mono`. The optional parameters only work in the -option=value format. The tool can output some RAW file metadata `-m=0|1` (0 for JSON, 1 for TXT) and the spectra file `-f=0|1|2|3` (0 for MGF, 1 for mzML, 2 for indexed mzML, 3 for Parquet) or both. Use the `-p` flag to disable the thermo native peak picking. 

```
ThermoRawFileParser.exe --help
 usage is (use -option=value for the optional arguments):
  -h, --help                 Prints out the options.
      --version              Prints out the library version.
  -i, --input=VALUE          The raw file input.
  -o, --output=VALUE         The output directory. Specify this or an output
                               file.
  -b, --output_file=VALUE    The output file. Specify this or an output
                               directory.
  -f, --format=VALUE         The output format for the spectra (0 for MGF, 1
                               for mzMl, 2 for indexed mzML, 3 for Parquet).
  -m, --metadata=VALUE       The metadata output format (0 for JSON, 1 for TXT).
  -g, --gzip                 GZip the output file if this flag is specified (
                               without value).
  -p, --noPeakPicking        Don't use the peak picking provided by the native
                               thermo library (by default peak picking is
                               enabled).
  -z, --noZlibCompression    Don't use zlib compression for the m/z ratios and
                               intensities (by default zlib compression is
                               enabled).
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

A (java) graphical user interface is also available [here](https://github.com/compomics/ThermoRawFileParserGUI) that enables the selection of an input RAW directory or one ore more RAW files.

## Download

Click [here](https://github.com/compomics/ThermoRawFileParser/releases) to go to the release page (with [release notes](https://github.com/compomics/ThermoRawFileParser/wiki/ReleaseNotes) starting from v1.1.7).

You can find the ThermoRawFileParserGUI [here](https://github.com/compomics/ThermoRawFileParserGUI).

## Galaxy integration

ThermoRawFileParser is available in the Galaxy [ToolShed](https://toolshed.g2.bx.psu.edu/view/galaxyp/thermo_raw_file_converter/a3edda696e4d) and is deployed at the [European Galaxy Server](https://usegalaxy.eu/root?tool_id=toolshed.g2.bx.psu.edu/repos/galaxyp/thermo_raw_file_converter/thermo_raw_file_converter/).

## Logging

By default the parser only logs to console. To enable logging to file, uncomment the file appender in the `log4net.config` file.

```
<log4net>
    <root>
        <level value="INFO" />
        <appender-ref ref="console" />
        <!--<appender-ref ref="file" />-->
    </root>
    <appender name="console" type="log4net.Appender.ConsoleAppender">
        <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%date %level %logger - %message%newline" />
        </layout>
    </appender>
    <!--<appender name="file" type="log4net.Appender.RollingFileAppender">
        <file value="ThermoRawFileParser.log" />
        <appendToFile value="true" />
        <rollingStyle value="Size" />
        <maxSizeRollBackups value="5" />
        <maximumFileSize value="10MB" />
        <staticLogFileName value="true" />
        <layout type="log4net.Layout.PatternLayout">
            <conversionPattern value="%date [%thread] %level %logger - %message%newline" />
        </layout>
    </appender>-->
</log4net>
```

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
