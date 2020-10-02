# ThermoRawFileParser

Wrapper around the .net (C#) ThermoFisher ThermoRawFileReader library for running on Linux with mono (works on Windows too). It takes a thermo RAW file as input and outputs a metadata file and the spectra in 3 possible formats:
* MGF: MS2 and MS3 spectra
* mzML and indexed mzML: MS1, MS2 and MS3 spectra
* Apache Parquet: under development

As of version 1.2.0, 2 subcommands are available (shoutout to the [eubic 2020 developers meeting](https://eubic-ms.org/events/2020-developers-meeting/), see [usage](#usage) for examples):
* query: returns one or more spectra in JSON PROXI by scan number(s)
* xic: returns chromatogram data based on JSON filter input

These features are still under development, remarks or suggestions are more than welcome.

RawFileReader reading tool. Copyright © 2016 by Thermo Fisher Scientific, Inc. All rights reserved

## ThermoRawFileParser Publication:
  * Hulstaert N, Shofstahl J, Sachsenberg T, Walzer M, Barsnes H, Martens L, Perez-Riverol Y: _ThermoRawFileParser: Modular, Scalable, and Cross-Platform RAW File Conversion_ [[PMID 31755270](https://www.ncbi.nlm.nih.gov/pubmed/31755270)].
  * If you use ThermoRawFileParser as part of a publication, please include this reference.

## (Linux) Requirements
[Mono](https://www.mono-project.com/download/stable/#download-lin) (install mono-complete if you encounter "assembly not found" errors).

## Download

Click [here](https://github.com/compomics/ThermoRawFileParser/releases) to go to the release page (with [release notes](https://github.com/compomics/ThermoRawFileParser/wiki/ReleaseNotes) starting from v1.1.7).

You can find the ThermoRawFileParserGUI [here](https://github.com/compomics/ThermoRawFileParserGUI).

## Usage
```
mono ThermoRawFileParser.exe -i=/home/user/data_input/raw_file.raw -o=/home/user/data_input/output/ -f=0 -g -m=0
```
with only the mimimal required argument `-i` or `-d` this becomes
```
mono ThermoRawFileParser.exe -i=/home/user/data_input/raw_file.raw
```
or
```
mono ThermoRawFileParser.exe -d=/home/user/data_input/
```
For running on Windows, omit `mono`. The optional parameters only work in the -option=value format. The tool can output some RAW file metadata `-m=0|1` (0 for JSON, 1 for TXT) and the spectra file `-f=0|1|2|3` (0 for MGF, 1 for mzML, 2 for indexed mzML, 3 for Parquet) or both. Use the `-p` flag to disable the thermo native peak picking. 

```
ThermoRawFileParser.exe --help
 usage is ThermoRawFileParser.exe [subcommand] [options]
optional subcommands are xic|query (use [subcommand] -h for more info]):
  -h, --help                 Prints out the options.
      --version              Prints out the library version.
  -i, --input=VALUE          The raw file input (Required). Specify this or an
                               input directory -d.
  -d, --input_directory=VALUE
                             The directory containing the raw files (Required).
                               Specify this or an input raw file -i.
  -o, --output=VALUE         The output directory. Specify this or an output
                               file -b. Specifying neither writes to the input
                               directory.
  -b, --output_file=VALUE    The output file. Specify this or an output
                               directory -o. Specifying neither writes to the
                               input directory.
  -f, --format=VALUE         The spectra output format: 0 for MGF, 1 for mzML,
                               2 for indexed mzML, 3 for Parquet. Defaults to
                               mzML if no format is specified.
  -m, --metadata=VALUE       The metadata output format: 0 for JSON, 1 for TXT.
  -c, --metadata_output_file=VALUE
                             The metadata output file. By default the metadata
                               file is written to the output directory.
  -g, --gzip                 GZip the output file.
  -p, --noPeakPicking        Don't use the peak picking provided by the native
                               Thermo library. By default peak picking is
                               enabled.
  -z, --noZlibCompression    Don't use zlib compression for the m/z ratios and
                               intensities. By default zlib compression is
                               enabled.
  -a, --allDetectors         Extract additional detector data: UV/PDA etc
  -l, --logging=VALUE        Optional logging level: 0 for silent, 1 for
                               verbose.
  -e, --ignoreInstrumentErrors
                             Ignore missing properties by the instrument.
  -x, --includeExceptionData Include reference and exception data
  -L, --msLevel=VALUE        Select MS levels (MS1, MS2, etc) included in the
                               output, should be a comma-separated list of
                               integers ( 1,2,3 ) and/or intervals ( 1-3 ),
                               open-end intervals ( 1- ) are allowed
  -P, --mgfPrecursor         Include precursor scan number in MGF file TITLE
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

### query subcommand
Enables the retrieval spectra by (a) scan number(s) in [PROXI format](https://github.com/HUPO-PSI/proxi-schemas).
```
mono ThermoRawFileParser.exe query -i=/home/user/data_input/raw_file.raw -o=/home/user/output.json n="1-5, 20, 25-30"
```
```
ThermoRawFileParser.exe query --help
usage is:
  -h, --help                 Prints out the options.
  -i, --input=VALUE          The raw file input (Required).
  -n, --scans=VALUE          The scan numbers. e.g. "1-5, 20, 25-30"
  -b, --output_file=VALUE    The output file. Specifying none writes the output
                               file to the input file parent directory.
  -p, --noPeakPicking        Don't use the peak picking provided by the native
                               Thermo library. By default peak picking is
                               enabled.
  -s, --stdout               Pipes the output into standard output. Logging is
                               being turned off.
```
### xic subcommand
Return one or more chromatograms based on query JSON input.
```
mono ThermoRawFileParser.exe xic -i=/home/user/data_input/raw_file.raw -j=/home/user/xic_input.json
```
```
ThermoRawFileParser.exe query --help
usage is:
  -h, --help                 Prints out the options.
  -i, --input=VALUE          The raw file input (Required). Specify this or an
                               input directory -d
  -d, --input_directory=VALUE
                             The directory containing the raw files (Required).
                               Specify this or an input file -i.
  -j, --json=VALUE           The json input file (Required).
  -p, --print_example        Show a json input file example.
  -o, --output=VALUE         The output directory. If not specified, the output
                               is written to the input directory
  -b, --base64               Encodes the content of the xic vectors as base 64
                               encoded string.
  -s, --stdout               Pipes the output into standard output. Logging is
                               being turned off.
```
Provide one of the following filters:
 * M/Z and tolerance (tolerance unit optional, default `ppm`)
 * M/Z start and end
 * sequence and tolerance (tolerance unit optional, default `ppm`)
 
with optional parameters start en end retention time and filter (thermo filter string, defaults to `ms`)

An example input JSON file:
```
[
        {
            "mz":488.5384,
            "tolerance":10,
            "tolerance_unit":"ppm"           
        },
        {
            "mz":575.2413,
            "tolerance":10,
            "rt_start":630,
            "rt_end":660,
            "scan_filter":"ms2"
        },
        {
            "mz_start":749.7860,
            "mz_end" : 750.4,            
            "rt_start":630,
            "rt_end":660
        },
        {
            "sequence":"TRANNEL",
            "tolerance":10
        }
]

```

[Go to top of page](#thermorawfileparser)

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
[Go to top of page](#thermorawfileparser)
