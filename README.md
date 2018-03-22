# ThermoRawFileParser

Wrapper around the .net (C#) ThermoFisher ThermoRawFileReader library for running on Linux with mono. It takes a thermo RAW file as input and outputs a metadata file and the MS2 spectra (centroided) in MGF format.

RawFileReader reading tool. Copyright © 2016 by Thermo Fisher Scientific, Inc. All rights reserved

## Usage

```
ThermoRawFileParser.exe usage (use -option=value for the optional arguments)
  -h, --help                 Prints out the options.
  -i, --input=VALUE          The raw file input.
  -o, --output=VALUE         The metadata and mgf output directory.
  -c, --collection[=VALUE]   The optional collection identifier (PXD identifier
                               for example).
  -m, --msrun[=VALUE]        The optional mass spectrometry run name used in
                               the spectrum title. The RAW file name will be
                               used if not specified.
  -s, --subfolder[=VALUE]    Optional, to disambiguate instances where the same
                               collection has 2 or more MS runs with the same
                               name.
```
