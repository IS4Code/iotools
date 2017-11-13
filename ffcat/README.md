ffcat
==========

This tool produces ffmpeg concat demuxer files and writes them to the standard output pipe. You can use different types of patterns to specify arbitrary number of files. You can also set the duration at which the files are to be displayed.

Example: `ffcat -p glob *.png >cat.txt`

This creates an ffmpeg-compatible file listing all .png images in the current directory.