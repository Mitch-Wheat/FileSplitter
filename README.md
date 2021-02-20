# FileSplitter

Splits text and .csv files. Standalone .NET 5.0 

Uses Mono.Options and Fody + Costura

Options:

  -i, --inputfilepattern=VALUE : the files(s) to split.
                             
  -o, --outputfolder=VALUE     : the output folder.
  
  -d, --headerrows=VALUE       : the number of header rows.
  
  -m, --maxlinesperfile=VALUE  : the maximum number of lines in each split file.
                             
  -r, --repeatheaderrows=VALUE : repeat header rows in each split file.
                             
  -c, --compress               : gzip compress split files.
  
  -b, --outputfilenamebase=VALUE : specifies optional filename base for split files.
                             
  -w, --overwrite              : overwrite output files.
  
  -s, --recursesubfolders      : find matching files in all sub folders.
  
  -h, --help                   : show this message and exit
