namespace ArtSizeReader
{
    /// <summary>
    ///     Exposes the fluent interface for ArtReader, which supports the analysis of a file or directory with various
    ///     options.
    /// </summary>
    public interface IArtReader
    {
        ArtReader Create();

        IArtReader ToRead(string toRead);

        IArtReader WithLogfile(string logfile);

        IArtReader WithMaxThreshold(string resolution);

        IArtReader WithPlaylist(string playlist);

        IArtReader WithRatio(bool hasRatio);

        IArtReader WithSize(double? size);

        IArtReader WithThreshold(string resolution);
    }
}