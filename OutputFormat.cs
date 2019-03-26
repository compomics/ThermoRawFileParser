namespace ThermoRawFileParser
{
    public enum OutputFormat
    {
        MGF,
        MzML,
        IndexMzML,
        Parquet,
        MGFNoProfileData,
        NONE
    }

    public enum MetadataFormat
    {
        JSON,
        TXT,
        PARQUET,
        NONE
    }
}