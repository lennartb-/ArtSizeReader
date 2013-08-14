﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using HundredMilesSoftware.UltraID3Lib;

namespace ArtSizeReader {
    public class ArtReader : IArtReader {

        private string target;
        private string logfile;
        private uint[] resolution;
        private string threshold;

        private bool hasLog = false;
        private bool hasThreshold = false;

        public IArtReader toRead(string toRead) {
            this.target = toRead;
            return this;
        }

        public IArtReader withLogfile(string logfile) {
            this.logfile = logfile;
            return this;
        }
        public IArtReader withThreshold(string threshold) {
            this.threshold = threshold;
            return this;
        }

        private bool checkSize(Bitmap image) {
            if (image.Size.Width < resolution[0] || image.Size.Height < resolution[1]) {
                return false;
            }
            else return true;

        }

        public ArtReader create() {
            ArtReader reader = new ArtReader();
            try {
                reader.target = Path.GetFullPath(target);
            }
            catch (Exception e) {
                Console.WriteLine("Could not find target path: " + e.Message);
                Console.WriteLine("for path " + target);
                throw new ArgumentException();
            }
            if (logfile != null) {
                createLogfile(logfile);
                hasLog = true;
            }
            if (threshold != null) {
                reader.resolution = resolution = parseResolution(threshold);
                hasThreshold = true;

            }

            return reader;

        }

        private uint[] parseResolution(string toParse) {
            try {
                return toParse.Split('x').Select(uint.Parse).ToArray();
            }
            catch (Exception e) {
                // Resolution is < 0 or doesn't fit into the uint Array
                Console.WriteLine("Can not parse Resolution, must be in format e.g.: 300x300");
                throw new InvalidCastException();
            }


        }

        public void getAlbumArt() {

            // Target is a single file
            if (File.Exists(target)) {
                analyzeFile(target);
            }

            // Target is a directory
            else if (Directory.Exists(target)) {
                foreach (string file in readFiles(target)) {
                    analyzeFile(file);
                }

            }

        }

        private IEnumerable<string> readFiles(string directory) {
            IEnumerable<string> musicFiles;
            int numOfFiles;
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
                Console.Write("\r{0} of {1} ({2}%) finished.", i++, numOfFiles, ((float)i / (float)numOfFiles) * 100);
                yield return currentFile;
            }


        }

        private void analyzeFile(string file) {
            UltraID3 tags = new UltraID3();
            try {

                tags.Read(file);
                ID3FrameCollection covers = tags.ID3v2Tag.Frames.GetFrames(CommonMultipleInstanceID3v2FrameTypes.Picture);
                ID3v2PictureFrame cover = (ID3v2PictureFrame)covers[0];
                Bitmap image = new Bitmap((Image)cover.Picture);

                if (hasThreshold && !checkSize(image)) {
                    Console.WriteLine("Checked Artwork size for file " + file + " is below limit: " + image.Size.Width + "x" + image.Size.Height);
                }
                //Console.WriteLine("Artwork size is: " + image.Size.Width + "x" + image.Size.Height);

            }
            catch (Exception e) {
                Console.WriteLine("No cover found for: " + file);
            }
        }


        private bool createLogfile(string path) {

            try {
                string checkedPath = Path.GetFullPath(path);
                using (FileStream fileTest = System.IO.File.Open(checkedPath, FileMode.OpenOrCreate)) {

                    fileTest.Close();
                }
                return true;
            }
            catch (Exception e) {
                Console.WriteLine("Could not create logfile: " + e.Message);
                Console.WriteLine("for path " + path);
                throw new ArgumentException();
            }
        }
    }



    public interface IArtReader {
        IArtReader toRead(string toRead);
        IArtReader withThreshold(string resolution);
        IArtReader withLogfile(string logfile);
        ArtReader create();
    }
}
