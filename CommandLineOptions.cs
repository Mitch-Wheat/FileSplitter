using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSplitter
{
    public class CommandLineOptions
    {
        public string FilePattern;
        public bool RecurseSubfolders;
        public int NumHeaderRows;
        public bool RepeatHeaderRows;
        public string OutputFolder;
        public string OutputFilenameBase;
        public bool OverwriteOutputFiles;
        public bool Compress;
        public long MaxLinesPerFile;
        public int MaxFileSizeMB;
    }
}
