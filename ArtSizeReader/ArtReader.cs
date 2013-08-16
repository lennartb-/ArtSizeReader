﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using HundredMilesSoftware.UltraID3Lib;

namespace ArtSizeReader {

    public class ArtReader : IArtReader {
        private string targetPath;
        private string logfile;
        private uint[] resolution;
        private string threshold;

        // Log to console per default
        private static StreamWriter defaultConsoleOutput = new StreamWriter(Console.OpenStandardOutput());

        private StreamWriter logger = defaultConsoleOutput;

        private bool hasLog = false;
        private bool hasThreshold = false;

        /// <summary>
        /// Specifies the file or path that will be analysed.
        /// </summary>
        /// <param name="toRead">The file or path to analyse.</param>
        /// <returns></returns>
        public IArtReader ToRead(string toRead) {
            this.targetPath = toRead;
            return this;
        }

        public IArtReader WithLogfile(string logfile) {
            this.logfile = logfile;
            return this;
        }

        public IArtReader WithThreshold(string threshold) {
            this.threshold = threshold;
            return this;
        }

        /// <summary>
        /// Builds an ArtReader object from the specified parameters and checks if they are valid.
        /// </summary>
        /// <returns>An ArtReader objects with the desired input parameters.</returns>

        public ArtReader Create() {
            ArtReader reader = new ArtReader();

            // Set up logfile.
            if (logfile != null) {
                Console.WriteLine("Logging enabled, writing log to: " + logfile);
                logger = InitialiseLogging();
                Console.SetOut(logger);
                hasLog = true;
            }

            // Check if target path is valid.
            try {
                reader.targetPath = Path.GetFullPath(targetPath);
            }

            catch (Exception e) {
                Console.WriteLine("Could not find target path: " + e.Message);
                Console.WriteLine("for path " + targetPath);
                throw new ArgumentException();
            }

            // Check and Parse resolution.
            if (threshold != null) {
                reader.resolution = ParseResolution();
                hasThreshold = true;
                Console.WriteLine("Threshold enabled, selected value: " + threshold);
            }

            return reader;
        }

        /// <summary>
        /// Parses the resolution from a WIDTHxHEIGHT string into an array.
        /// </summary>
        /// <param name="toParse">The string to parse.</param>
        /// <returns>A uint[2] array containing the width in the first and height in the second field.</returns>

        private uint[] ParseResolution() {
            try {
                return threshold.Split('x').Select(uint.Parse).ToArray();
            }
            catch (Exception e) {
                // Resolution is < 0 or doesn't fit into the uint Array
                Console.WriteLine("Can not parse Resolution, must be in format e.g.: 300x300");
                throw new InvalidCastException();
            }
        }

        /// <summary>
        /// Starts fetching the album art from the specified file or directory.
        /// </summary>

        public void GetAlbumArt() {
            // Target is a single file
            if (File.Exists(targetPath)) {
                AnalyzeFile(targetPath);
            }

            // Target is a directory
            else if (Directory.Exists(targetPath)) {
                foreach (string file in ReadFiles(targetPath)) {
                    AnalyzeFile(file);
                }
            }
        }

        /// <summary>
        /// Enumerates the files in a certain directory and returns one file at a time.
        /// </summary>
        /// <param name="directory"> The directory to check.</param>
        /// <returns>An IEnumerable containing all found files.</returns>

        private IEnumerable<string> ReadFiles(string directory) {
            IEnumerable<string> musicFiles;
            int numOfFiles;

            // Get all files in the directory.
            try {
                musicFiles = Directory.EnumerateFiles(directory, "*.mp3", SearchOption.AllDirectories);
                numOfFiles = musicFiles.Count();
            }
            catch (UnauthorizedAccessException uae) {
                Console.WriteLine(uae.Message);
                yield break;
            }
            catch (PathTooLongException ptle) {
                Console.WriteLine(ptle.Message);
                yield break;
            }

            int i = 0;
            foreach (string currentFile in musicFiles) {
                // If logging to file is enabled, print out the progress to console anyway.
                if (hasLog) {
                    Console.SetOut(defaultConsoleOutput);
                    Console.Write("\r{0} of {1} ({2}%) finished.", i++, numOfFiles, ((float)i / (float)numOfFiles) * 100);
                    Console.SetOut(logger);
                }

                else {
                    Console.Write("\r{0} of {1} ({2}%) finished.", i++, numOfFiles, ((float)i / (float)numOfFiles) * 100);
                }
                yield return currentFile;
            }
        }

        /// <summary>
        /// Checks whether the size of an image is below the global threshold.
        /// </summary>
        /// <param name="image">The image to check.</param>
        /// <returns>false if the image is below the limit, true if not.</returns>

        private bool CheckSize(Bitmap image) {
            if (image.Size.Width < resolution[0] || image.Size.Height < resolution[1]) {
                return false;
            }
            else return true;
        }

        /// <summary>
        /// Analyzes a file for album art and handles checking of the size.
        /// </summary>
        /// <param name="file">The file to check.</param>

        private void AnalyzeFile(string file) {
            UltraID3 tags = new UltraID3();
            try {
                String infoLine;
                tags.Read(file);
                ID3FrameCollection covers = tags.ID3v2Tag.Frames.GetFrames(CommonMultipleInstanceID3v2FrameTypes.Picture);
                ID3v2PictureFrame cover = (ID3v2PictureFrame)covers[0];
                Bitmap image = new Bitmap((Image)cover.Picture);
                if (hasThreshold && !CheckSize(image)) {
                    Console.WriteLine("Checked Artwork size for file " + file + " is below limit: " + image.Size.Width + "x" + image.Size.Height);
                }
            }
            catch (Exception e) {
                Console.WriteLine("No cover found for: " + file);
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Writes a line to into the logfile.
        /// </summary>
        /// <param name="line">The line to write.</param>

        private void WriteToLogFile(string line) {
            try {
                Console.WriteLine(line);
            }
            catch (Exception e) {
                Console.WriteLine("Could not create logfile: " + e.Message);
                Console.WriteLine("for path " + logfile);
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// Manages the initialisation of the logfile.
        /// </summary>
        /// <returns>A StreamWriter targeting the path of the logfile.</returns>
        private StreamWriter InitialiseLogging() {
            try {
                string checkedPath = Path.GetFullPath(logfile);
                StreamWriter writer = new StreamWriter(logfile, true);
                return writer;
            }
            catch (Exception e) {
                Console.WriteLine("Could not create logfile: " + e.Message);
                Console.WriteLine("for path " + logfile);
                throw new ArgumentException();
            }
        }
    }

    /// <summary>
    /// Exposes the ArtReader, which supports the analysis of a file or directory with various options.
    /// </summary>

    public interface IArtReader {

        IArtReader ToRead(string toRead);
        IArtReader WithThreshold(string resolution);
        IArtReader WithLogfile(string logfile);
        ArtReader Create();
    }
}