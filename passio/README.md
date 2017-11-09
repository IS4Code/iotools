passio
==========

This tool works in a similar way to the `cat` utility for Unix. If used without any input files, it will read data from the standard input. Every input is read sequentially and copied to the standard output without changes. If the `-q` option is not set, it will also print the size of each input (or the total size if `-t` is set) to the standard error stream.

Example: `passio a.txt b.txt - >abx.txt`

This command attempts to copy data from files a.txt and b.txt, and then open the standard input for reading. All bytes are copied to abx.txt.
