using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using TagLib;
using File = System.IO.File;

namespace ArtSizeReader
{
    public class ArtReader : IArtReader
    {
        // Preserve standard Console output
        private static readonly StreamWriter DefaultConsoleOutput = new StreamWriter(Console.OpenStandardOutput());

        // Used for progress bar
        private int analyzedNumberOfFiles;

        // Optional parameter booleans
        private bool hasMaxThreshold;
        private bool hasRatio;
        private bool hasSizeLimit;
        private bool hasThreshold;

        // Optional parameter values
        private string logfilePath;
        private StreamWriter logfileWriter = DefaultConsoleOutput;
        private uint[] maxResolution;
        private string maxThreshold;
        private int numberOfFiles;

        // Input values converted to other data types:
        private Playlist playlist;
        private string playlistPath;
        private uint[] resolution;

        // Required parameter values
        private double? size;
        private string targetPath;
        private string threshold;
        private bool withLogfile;
        private bool withPlaylist;

        /// <summary>
        ///     Builds an ArtReader object from the specified parameters and checks if they are valid.
        /// </summary>
        /// <returns>An ArtReader objects with the desired input parameters.</returns>
        /// <exception cref="ArgumentException">Thrown when any of the supplied arguments are invalid.</exception>
        public ArtReader Create()
        {
            if (withLogfile)
            {
                ValidateLogfile();
            }

            // Check if target path is valid.
            ValidateTargetPath();

            if (hasThreshold)
            {
                ValidateResolution(threshold, false);
                Console.WriteLine("Threshold enabled, selected value: " + resolution[0] + "x" + resolution[1]);
            }

            if (hasRatio)
            {
                Console.WriteLine("Checking for 1:1 ratio is enabled.");
            }

            if (hasSizeLimit)
            {
                Console.WriteLine("File size threshold enabled, reporting files above " + size + " kB");
            }

            if (hasMaxThreshold)
            {
                ValidateResolution(maxThreshold, true);
                Console.WriteLine("Maximum threshold enabled, selected value: " + maxResolution[0] + "x" + maxResolution[1]);
            }

            if (withPlaylist)
            {
                ValidatePlaylist();
            }

            return this;
        }

        /// <summary>
        ///     Starts fetching the album art from the specified file or directory.
        /// </summary>
        /// <returns>True if analyzing succeeded, false if the file or path could not be found.</returns>
        public bool GetAlbumArt()
        {
            // Target is a single file
            if (File.Exists(targetPath))
            {
                AnalyzeFile(targetPath);
                return true;
            }

            // Target is a directory

            if (Directory.Exists(targetPath))
            {
                // Search for files in the directory, but filter out inaccessible folders before.

                var accessibleDirectories = SafeFileEnumerator.EnumerateDirectories(targetPath, "*.*", SearchOption.AllDirectories);
                IEnumerable<string> temp = new[] { targetPath };
                accessibleDirectories = accessibleDirectories.Concat(temp);
                var directories = accessibleDirectories as string[] ?? accessibleDirectories.ToArray();
                numberOfFiles = CountFiles(directories);

                foreach (var file in directories.SelectMany(ReadFiles))
                {
                    AnalyzeFile(file);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Specifies the file or path that will be analyzed.
        /// </summary>
        /// <param name="toRead">The file or path to analyse.</param>
        /// <returns>The instance of the current object.</returns>
        public IArtReader ToRead(string toRead)
        {
            targetPath = toRead;
            return this;
        }

        /// <summary>
        ///     Specifies the filename and path of the logfile.
        /// </summary>
        /// <param name="logfile">The path and filename of the logfile.</param>
        /// <returns>The instance of the current object.</returns>
        public IArtReader WithLogfile(string logfile)
        {
            logfilePath = logfile;
            withLogfile = true;
            return this;
        }

        /// <summary>
        ///     Specifies the art size maximum threshold in the format WIDTH x HEIGHT.
        /// </summary>
        /// <param name="customMaxThreshold">The maximum threshold.</param>
        /// <returns>The instance of the current object.</returns>
        public IArtReader WithMaxThreshold(string customMaxThreshold)
        {
            maxThreshold = customMaxThreshold;
            hasMaxThreshold = true;
            return this;
        }

        /// <summary>
        ///     Specifies the filename and path of the playlist.
        /// </summary>
        /// <param name="customPlaylist">The filename and path of the playlist.</param>
        /// <returns>The instance of the current object.</returns>
        public IArtReader WithPlaylist(string customPlaylist)
        {
            playlistPath = customPlaylist;
            withPlaylist = true;
            return this;
        }

        /// <summary>
        ///     Specifies the art size threshold in the format WIDTH x HEIGHT.
        /// </summary>
        /// <param name="customHasRatio">The threshold.</param>
        /// <returns>The instance of the current object.</returns>
        public IArtReader WithRatio(bool customHasRatio)
        {
            hasRatio = customHasRatio;
            return this;
        }

        /// <summary>
        ///     Specifies the file size threshold.
        /// </summary>
        /// <param name="customSize">The size in kilobytes.</param>
        /// <returns>The instance of the current object.</returns>
        public IArtReader WithSize(double? customSize)
        {
            size = customSize;
            hasSizeLimit = true;
            return this;
        }

        /// <summary>
        ///     Specifies the art size threshold in the format WIDTH x HEIGHT.
        /// </summary>
        /// <param name="customThreshold">The threshold.</param>
        /// <returns>The instance of the current object.</returns>
        public IArtReader WithThreshold(string customThreshold)
        {
            threshold = customThreshold;
            hasThreshold = true;
            return this;
        }

        /// <summary>
        ///     Analyzes a file for album art and handles checking of the size.
        /// </summary>
        /// <param name="file">The file to check.</param>
        private void AnalyzeFile(string file)
        {
            try
            {
                var mp3 = TagLib.File.Create(file);

                var message = GetMessages(file, mp3);
                // If one of the checks failed, write it to console.
                if (!message.Equals(string.Empty))
                {
                    if (!withLogfile) Console.Write("\r");
                    Console.WriteLine(file + ": " + message);
                    if (withPlaylist) playlist.Write(file);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unhandled Exception while reading tags for file {file}: { e.Message}");
            }
        }

        private string GetMessages(string file, TagLib.File mp3)
        {
            var message = string.Empty;
            var tag = mp3.GetTag(TagTypes.Id3v2);

            var covers = tag.Pictures.Where(p => p.Type != PictureType.NotAPicture).ToList();

            // Check if there actually is a cover.
            if (covers.Any())
            {
                var cover = new Bitmap(new MemoryStream(covers[0].Data.Data));

                if (hasSizeLimit)
                {
                    try
                    {
                        var imagesize = GetImageSize(cover);

                        if (imagesize > size)
                        {
                            message += "Artwork file size is " + imagesize + " kB. ";
                        }
                    }
                    catch (Exception e)
                    {
                        message += $"Could not get image size from file {file}, Reason: {e.Message} ({e.GetType().Name})";
                    }
                }

                if (!IsWellFormedImage(cover))
                {
                    message += "Artwork image size is " + cover.Size.Width + "x" + cover.Size.Height;
                }
            }

            // No covers found.
            else
            {
                message += "No cover found.";
            }

            return message;
        }

        /// <summary>
        ///     Counts the number of individual MP3 files in an enumeration of directories.
        /// </summary>
        /// <param name="dirs">The enumerated directories.</param>
        /// <returns>The number of MP3 files in the directories.</returns>
        private int CountFiles(IEnumerable<string> dirs)
        {
            return dirs.Sum(dir => SafeFileEnumerator.EnumerateFiles(dir, "*.mp3", SearchOption.TopDirectoryOnly).Count());
        }

        /// <summary>
        ///     Calculates the file size of an image.
        /// </summary>
        /// <param name="image">The image to check.</param>
        /// <returns>The file size in bytes.</returns>
        private double GetImageSize(Image image)
        {
            using var ms = new MemoryStream();
            image.Save(ms, image.RawFormat);
            // Convert to kB.                    
            return ms.Length >> 10;
        }

        /// <summary>
        ///     Manages the initialization of the logfile.
        /// </summary>
        /// <returns>true if the path is valid, false when not.</returns>
        private bool InitialiseLogging()
        {
            try
            {
                var fullLogfilePath = Path.GetFullPath(logfilePath);
                var validDir = Directory.Exists(Path.GetDirectoryName(fullLogfilePath));
                if (validDir)
                {
                    var fs = new FileStream(fullLogfilePath, FileMode.Append);
                    logfileWriter = new StreamWriter(fs)
                    {
                        AutoFlush = true
                    };
                    Console.SetOut(logfileWriter);
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not create logfile: " + e.Message + "(" + e.GetType().Name + ")");
                return false;
            }
        }

        /// <summary>
        ///     Checks whether the size of an image is below the global threshold.
        /// </summary>
        /// <param name="image">The image to check.</param>
        /// <returns>false if the image is below the limit or has no 1:1 ratio, true if not.</returns>
        private bool IsWellFormedImage(Image image)
        {
            // Check if image is below (minimum) threshold.
            if (hasThreshold)
            {
                if ((image.Size.Width < resolution[0]) || (image.Size.Height < resolution[1]))
                {
                    return false;
                }
            }

            // Check if image is above maximum threshold.
            if (hasMaxThreshold)
            {
                if ((image.Size.Width > maxResolution[0]) || (image.Size.Height > maxResolution[1]))
                {
                    return false;
                }
            }

            // Check for 1:1 ratio.
            if (hasRatio)
            {
                if (image.Size.Width != image.Size.Height)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Enumerates the files in a certain directory and returns one file at a time.
        /// </summary>
        /// <param name="directory"> The directory to check.</param>
        /// <returns>An IEnumerable containing all found files.</returns>
        private IEnumerable<string> ReadFiles(string directory)
        {
            // Get all files in the directory.            
            var musicFiles = SafeFileEnumerator.EnumerateFiles(directory, "*.mp3", SearchOption.TopDirectoryOnly);

            foreach (var currentFile in musicFiles)
            {
                // If logging to file is enabled, print out the progress to console anyway.
                if (withLogfile)
                {
                    Console.SetOut(DefaultConsoleOutput);
                    Console.Write(
                        "\r{0} of {1} ({2}%) finished.{3}",
                        ++analyzedNumberOfFiles,
                        numberOfFiles,
                        (float) analyzedNumberOfFiles / numberOfFiles * 100,
                        new string(' ', Console.LargestWindowWidth));
                    Console.SetOut(logfileWriter);
                }
                else
                {
                    /* Print out progress. Argument {3} ensures that any text right of the progress is cleared,
                     * otherwise old chars are not removed, since the number of decimal places of the percentage may vary.*/
                    Console.Write(
                        "\r{0} of {1} ({2}%) finished.{3}",
                        ++analyzedNumberOfFiles,
                        numberOfFiles,
                        (float) analyzedNumberOfFiles / numberOfFiles * 100,
                        new string(' ', Console.LargestWindowWidth));
                }

                yield return currentFile;
            }
        }

        /// <summary>
        ///     Checks the logfile path and starts the initialization of the logfile.
        /// </summary>
        private void ValidateLogfile()
        {
            if (logfilePath != null)
            {
                if (InitialiseLogging())
                {
                    Console.WriteLine("Logging enabled, writing log to: " + logfilePath);
                }
                else
                {
                    throw new ArgumentException("Invalid logfile path: " + logfilePath);
                }
            }
        }

        /// <summary>
        ///     Manages the initialization of the playlist.
        /// </summary>
        private void ValidatePlaylist()
        {
            if (playlistPath != null)
            {
                try
                {
                    var fullPlaylistPath = Path.GetFullPath(playlistPath);
                    var validDir = Directory.Exists(Path.GetDirectoryName(fullPlaylistPath));
                    if (validDir)
                    {
                        playlist = new Playlist(playlistPath);
                        Console.WriteLine("Playlist enabled, writing to " + fullPlaylistPath);
                    }
                    else throw new ArgumentException("Invalid playlist path: " + playlistPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not create playlist: " + e.Message + "(" + e.GetType().Name + ")");
                    throw;
                }
            }
        }

        /// <summary>
        ///     Checks if the resolution string is valid.
        /// </summary>
        /// <param name="thresholdToValidate">The string with the resolution.</param>
        /// <param name="isMaxThreshold">
        ///     True if the parameter is the maximum threshold string, false if it's the normal
        ///     resolution.
        /// </param>
        private void ValidateResolution(string thresholdToValidate, bool isMaxThreshold)
        {
            try
            {
                if (isMaxThreshold)
                {
                    maxResolution = thresholdToValidate.Split('x').Select(uint.Parse).ToArray();
                }
                else
                {
                    resolution = thresholdToValidate.Split('x').Select(uint.Parse).ToArray();
                }
            }
            catch (FormatException fe)
            {
                // Resolution is < 0 or doesn't fit into the uint Array
                throw new ArgumentException("Can not parse resolution " + thresholdToValidate + ", must be in format e.g.: 300x300", fe);
            }
        }

        /// <summary>
        ///     Checks if the target path is valid and exists.
        /// </summary>
        private void ValidateTargetPath()
        {
            if (Directory.Exists(targetPath))
            {
                Console.WriteLine("Analyzing file(s) in " + targetPath);
            }
            else if (File.Exists(targetPath))
            {
                Console.WriteLine("Analyzing file " + targetPath);
            }
            else
            {
                throw new ArgumentException("Invalid target path: " + targetPath);
            }
        }
    }
}