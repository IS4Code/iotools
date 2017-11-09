pipeio
==========

This program exposes Windows named pipes for command scripts and other applications. By default, it creates two named pipes for its standard input and output (named `%PIPE_IN%` and `%PIPE_OUT%` by default; can be set via `-i` and `-o`) and runs the specified command with these pipes usable by the inner command. The pipes are deleted when the inner commands exits.

Using the `-p` option, additional pipes may be created for use within the command. Each new pipe is exported via the variables `%varname_IN%` and `%varname_OUT%` where `varname` is the name of the pipe specified with the `-p` option.

The inner command can also be run in a "contained" mode (set by `-c`) which turns off any standard stream redirection (suitable if you don't want to use `%PIPE_IN%` and `%PIPE_OUT%`). By default, both standard output and error streams are redirected to the standard error of the program.

Example: `echo Hello | pipeio "passio <%PIPE_IN% >%PIPE_OUT%"`

This prints "Hello" to the standard output.
