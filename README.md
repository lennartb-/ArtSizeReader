ArtSizeReader
=============

ArtSizeReader is a small commandline utility to check the size of covers in your digial music collection. This way you can quickly determine songs without covers or below a certain threshold (i.e. low resolution covers).

Requires .NET Framework 4.0 or higher.

Art Size Reader uses UltraID3Lib (http://home.fuse.net/honnert/UltraID3Lib/), an MP3 ID3 Tag Editor and MPEG Info Reader Library Copyright 2002 - 2010 Hundred Miles Software (Mitchell S. Honnert) for accessing the MP3 Tags and the Command Line Parser Library (https://commandline.codeplex.com).

The downloads for current and previous version are hosted at Bintray: https://bintray.com/nan0/ArtSizeReader/ArtSizeReader

Available arguments:

-i, --input Required. A file or directory to analyze.

-l, --logfile Writes output into the specified file. If no directory is given, the current directory will be used.

-t, --threshold Required. Cover sizes above this threshold (in pixels) will be ignored. Format example: 400x400.

-p, --playlist Creates a M3U playlist with all scanned tracks below the threshold. Use to quickly load all affected files into your favorite tag editor or media player.

-r, --ratio Additionally restrict the cover size to a 1:1 aspect ratio. If enabled e.g 400x350 will cause an error while 400x400 won't.

-s, --size Cover file sizes below this threshold (in kilobytes) will be ignored. Useful to check for large image files. Format example: 1000 (equals 1 Megabyte). Use a negative value to print files sizes for all images.

-m, --max-threshold Can be used together with -t to define a maximum upper limit for covers, i.e. covers larger than this resolution will be reported. Format example: 1000x1000.

--help Display the help screen.
