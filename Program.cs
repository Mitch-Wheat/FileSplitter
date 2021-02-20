using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

using Mono.Options;

namespace FileSplitter
{
    class Program
    {
        static void Main(string[] args)
        {
            bool showHelp = false;
            CommandLineOptions commandLineOptions = new CommandLineOptions() { NumHeaderRows = 1, RepeatHeaderRows = true, Compress = false };

            var options = new OptionSet
            {
                { "i|inputfilepattern=", "the files(s) to split.", i => commandLineOptions.FilePattern = i },
                { "o|outputfolder=", "the output folder.", o => commandLineOptions.OutputFolder = o },
                { "d|headerrows=", "the number of header rows.", (int d) => commandLineOptions.NumHeaderRows = d },
                { "m|maxlinesperfile=", "the maximum number of lines in each split file.", (int m) => commandLineOptions.MaxLinesPerFile = m },
                //{ "x|maxfilesizeMB=", "the maximum size of each split file (MB).", (int x) => commandLineOptions.MaxFileSizeMB = x },
                { "r|repeatheaderrows=", "repeat header rows in each split file.", r => commandLineOptions.RepeatHeaderRows = r != null },
                { "c|compress", "gzip compress split files (zip).", c => commandLineOptions.Compress = c != null },
                { "b|outputfilenamebase=", "specifies filename base for split files.", b => commandLineOptions.OutputFilenameBase = b },
                { "w|overwrite", "overwrite output files.", w => commandLineOptions.OverwriteOutputFiles = w != null },
                { "s|recursesubfolders=", "find matching files in all sub folders.", s => commandLineOptions.RecurseSubfolders = s != null },
                { "h|help", "show this message and exit", h => showHelp = h != null },
            };

            try
            {
                List<string> extra = options.Parse(args);

                if (showHelp)
                {
                    ShowHelp(options);
                    return;
                }

                if (string.IsNullOrWhiteSpace(commandLineOptions.FilePattern))
                    throw new InvalidOperationException("Missing required option -i=inputfilepattern");

                if (string.IsNullOrWhiteSpace(commandLineOptions.OutputFolder))
                    throw new InvalidOperationException("Missing required option -o=outputfolder");

                if (commandLineOptions.MaxLinesPerFile == 0L) //&& commandLineOptions.MaxFileSizeMB == 0 )
                    throw new InvalidOperationException("Missing required option, one of -m=maxlinesperfile or -x=maxfilesizeMB");

                Split(commandLineOptions, extra);
            }
            catch (OptionException e)
            {
                Console.Write("FileSplitter: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `FileSplitter --help' for more information.");

                return;
            }
        }

        private static void ShowHelp(OptionSet o)
        {
            Console.WriteLine("Usage: FileSplitter [OPTIONS]+");
            Console.WriteLine("");
            Console.WriteLine();
            Console.WriteLine("Options:");
            o.WriteOptionDescriptions(Console.Out);
        }

        private static void Split(CommandLineOptions options, List<string> extra)
        {
            string folderPath = Path.GetDirectoryName(options.FilePattern);
            string filePattern = Path.GetFileName(options.FilePattern);
            SearchOption searchOption = options.RecurseSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            List<string> files = (filePattern.Contains('*')) ? Directory.EnumerateFiles(folderPath, filePattern, searchOption).ToList() : new List<string> { options.FilePattern };

            foreach (var file in files)
            {
                SplitFile(options, file);
            }

            // doing this as a separate action as ZipArchive is messy when maintaining state
            if (options.Compress)
            {
                var toZipfiles = Directory.EnumerateFiles(options.OutputFolder, "*.csv", SearchOption.TopDirectoryOnly).ToList();
                foreach (var fileToCompress in toZipfiles)
                {
                    CompressFile(fileToCompress);
                }
            }
        }

        public static void CompressFile(string fullfilename)
        {
            using (FileStream fs = File.Open(fullfilename, FileMode.Open))
            using (FileStream compressedFileStream = File.Create(fullfilename + ".gz"))
            using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionLevel.Optimal))
            {
                fs.CopyTo(compressionStream);
            }
        }

        private static void SplitFile(CommandLineOptions options, string inputfile)
        {
            long lineCounter = long.MaxValue - 1; // Note: causes first call to GetOutputFile() to create the first file
            int filecounter = 0;
            string line;

            string outputFilenamebase = (string.IsNullOrWhiteSpace(options.OutputFilenameBase) || options.FilePattern.Contains('*'))
                ? Path.GetFileNameWithoutExtension(inputfile) : options.OutputFilenameBase;
            string fileExtension = Path.GetExtension(inputfile);
            StreamWriter writer = null;

            using (var reader = new StreamReader(inputfile))
            {
                List<string> headerRows = ReadHeaderRows(reader, options);

                while ((line = reader.ReadLine()) != null)
                {
                    writer = GetOutputFile(writer, options, outputFilenamebase, fileExtension, headerRows, ref lineCounter, ref filecounter);
                    writer.WriteLine(line);
                }

                writer.Flush();
                writer.Close();
            }
        }

        private static List<string> ReadHeaderRows(StreamReader sr, CommandLineOptions options)
        {
            List<string> headerRows = new List<string>();
            string line;

            for (int i = 0; i < options.NumHeaderRows; i++)
            {
                line = sr.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    headerRows.Add(line);
                }
            }

            return headerRows;
        }

        private static void WriteHeaderRows(StreamWriter sw, List<string> headerRows)
        {
            foreach (var row in headerRows)
            {
                sw.WriteLine(row);
            }
        }

        private static StreamWriter GetOutputFile(StreamWriter sw, CommandLineOptions options, string filenamebase, string extension, List<string> headerRows, ref long lineCounter, ref int fileCounter)
        {
            // Determine if a new file is required...
            if (++lineCounter > options.MaxLinesPerFile)
            {
                if (sw != null)
                {
                    sw.Flush();
                    sw.Close();
                }

                fileCounter++;
                lineCounter = 1L;

                sw = new StreamWriter(GetOutputFilename(options.OutputFolder, filenamebase, fileCounter, extension));

                if (options.RepeatHeaderRows || fileCounter == 1)
                {
                    WriteHeaderRows(sw, headerRows);
                }
            }

            return sw;
        }

        private static string GetOutputFilename(string folder, string filenamebase, int filenumber, string extension)
        {
            return Path.Combine(folder, filenamebase) + "_" + $"{filenumber:000000}" + extension;
        }

    }
}
