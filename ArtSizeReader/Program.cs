using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using CommandLine;
using CommandLine.Text;

namespace ArtSizeReader
{
    public class Program
    {
        private const int UncaughtException = 5;

        private static void DisplayHelp(ParserResult<Options> parserResult)
        {
            HelpText.AutoBuild(
                parserResult,
                help =>
                {
                    help.AdditionalNewLineAfterOption = true;
                    help.AddPreOptionsLine("ERROR(S):\n  -t/--threshold and/or -s/--size are required.");
                    Console.WriteLine(help.ToString());
                    return help;
                });
        }

        private static void Main(string[] args)
        {
#if DEBUG
            // Translates Exceptions and other messages to english.
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
#endif
          
            if (ParseOptions(args))
            {
                Console.WriteLine("\nFinished!");
            }
            else
            {
                Console.WriteLine("\nFinished with errors!");

                // Wait for user input/keep cmd window open.
                //// Console.ReadLine();
            }
#if DEBUG
            // Wait for user input/keep cmd window open.
            Console.ReadLine();
#endif
        }

        private static void OnRun(Options options)
        {
            // If either one or both options are present, continue.
            if ((options.Size == null) && (options.Threshold == null))
            {
                return;
            }

            var ar = new ArtReader();
            // Check if we have a target.
            if (options.InputFile != null)
            {
                ar.ToRead(options.InputFile);
            }

            // Check if a resolution limit is set.
            if (options.Threshold != null)
            {
                ar.WithThreshold(options.Threshold);
            }

            // Check if output will be logged to file.
            if (options.Logfile != null)
            {
                ar.WithLogfile(options.Logfile);
            }

            // Check if output will be logged to playlist.
            if (options.Playlist != null)
            {
                ar.WithPlaylist(options.Playlist);
            }

            // Check if the covers should be checked for a 1:1 ratio.
            if (options.Ratio)
            {
                ar.WithRatio(true);
            }

            // Check if a maximum file size is set.
            if (options.Size != null)
            {
                ar.WithSize(options.Size);
            }

            // Check if the have a maximum threshold.
            if (options.MaxThreshold != null)
            {
                ar.WithMaxThreshold(options.MaxThreshold);
            }

            try
            {
                // Create object and start analyzing the files.
                ar.Create().GetAlbumArt();
            }
            catch (ArgumentException ae)
            {
                Console.WriteLine("Error: " + ae.Message);
            }
        }

        /// <summary>
        ///     Handles the parsing of the supplied program arguments.
        /// </summary>
        /// <param name="args">Main method's arguments.</param>
        /// <returns>True if everything went well, false if an error occurred.</returns>
        private static bool ParseOptions(IEnumerable<string> args)
        {
            // Get command line parser
            var parserResult = Parser.Default.ParseArguments<Options>(args);
            parserResult.WithParsed(OnRun).WithNotParsed(errs => DisplayHelp(parserResult));

            return false;
        }
    }
}