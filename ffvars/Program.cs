/* Date: 26.12.2017, Time: 0:29 */
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using iotools;

namespace ffvars
{
	class Program
	{
		public static int? BufferSize;
		public static bool Exit;
		public static bool CopyInput;
		public static string StreamSpec;
		public static bool CommandOperands;
		public static List<string> Entries = new List<string>();
		public static List<string> Command = new List<string>();
		public static string InputFile;
		public static bool LiteralLine;
		public static bool Debug;
		public static string Shell;
		
		public static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			
			var options = new Options();
			try{
				options.Parse(args);
				
				//if(!Quiet && !Verbose)
				{
					options.Banner();
				}
				if(Entries.Count == 0)
				{
					Console.Error.WriteLine("No entries specified. Use --help for help.");
					return;
				}
				foreach(var entry in Entries)
				{
					if(!Regex.IsMatch(entry, @"^[a-zA-Z0-9=,_]*$"))
					{
						Console.Error.WriteLine("Error: Invalid characters in an entry specifier ({0}).", entry);
						return;
					}
				}
				if(Command.Count == 0)
				{
					Console.Error.WriteLine("No command specified.");
					return;
				}
				if(StreamSpec != null)
				{
					if(!Regex.IsMatch(StreamSpec, @"^[a-zA-Z0-9\.]*$"))
					{
						Console.Error.WriteLine("Error: Invalid characters in the stream specifier ({0}).", StreamSpec);
						return;
					}
					StreamSpec = StreamSpec.Replace('.', ':');
				}
				
				var extractor = new FFProbeExtractor();
				extractor.BufferSize = BufferSize ?? 4096;
				extractor.LiteralLine = LiteralLine;
				extractor.Shell = Shell;
				extractor.CopyInput = CopyInput;
				extractor.Exit = Exit;
				if(Debug)
				{
					extractor.Logger = options.Log;
				}
				
				extractor.Run(InputFile == "-" ? null : InputFile, Entries, StreamSpec, Command);
			}catch(Exception e)
			{
				//if(!Quiet && !Verbose)
				{
					options.Log(e.Message);
				}
			}
		}
	}
	
	class Options : ApplicationOptions
	{
		protected override string Usage{
			get{
				return "[options] entry... : command";
			}
		}
		
		public override void Description()
		{
			base.Description();
			Console.Error.WriteLine();
			Console.Error.Write(" ");
			OutputWrapPad("This program uses ffprobe to extract specified " +
			              "metadata from a file, and stores them as " +
			              "environment variables for a new command.", 1);
		}
		
		public override IList<OptionInfo> GetOptions()
		{
			return new OptionInfoCollection{
				{"i", "input", "file", "sets the input file (stdin by default)"},
				{"c", "copy", null, "copy the input to the inner command"},
				{"s", "streams", "specifier", "sets the stream specifier for the file"},
				{"l", "literal-command", null, "copies the command line without parsing the arguments"},
				{"e", "exit", null, "do not wait for the inner command to exit"},
				{"d", "debug", null, "print debug messages"},
				{"b", "buffer-size", "size", "sets the size of the buffers (default 4096)"},
				{"S", "shell", "program", "specifies the interpreter to run inner commands"},
				{"?", "help", null, "displays this help message"},
			};
		}
		
		protected override void Notes()
		{
			Console.Error.WriteLine();
			Console.Error.WriteLine("Example: "+ExecutableName+" format=duration : echo %format.duration% <file.avi");
		}
		
		protected override OptionArgument OnOptionFound(string option)
		{
			switch(option)
			{
				case "b":
				case "buffer-size":
					return OptionArgument.Required;
				case "e":
				case "exit":
					if(Program.Exit)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.Exit = true;
					return OptionArgument.None;
				case "d":
				case "debug":
					if(Program.Debug)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.Debug = true;
					return OptionArgument.None;
				case "c":
				case "copy":
					if(Program.CopyInput)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.CopyInput = true;
					return OptionArgument.None;
				case "l":
				case "literal-command":
					if(Program.LiteralLine)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.LiteralLine = true;
					return OptionArgument.None;
				case "s":
				case "streams":
					return OptionArgument.Required;
				case "i":
				case "input":
					return OptionArgument.Required;
				case "S":
				case "shell":
					return OptionArgument.Required;
				case "?":
				case "help":
					Help();
					return OptionArgument.None;
				default:
					throw UnrecognizedOption(option);
			}
		}
		
		protected override void OnOptionArgumentFound(string option, string argument)
		{
			switch(option)
			{
				case "b":
				case "buffer-size":
					if(Program.BufferSize != null)
					{
						throw OptionAlreadySpecified(option);
					}
					int bufferSize;
					if(!Int32.TryParse(argument, out bufferSize))
					{
						throw ArgumentInvalid(option, "integer");
					}
					Program.BufferSize = bufferSize;
					break;
				case "s":
				case "stream":
					if(Program.StreamSpec != null)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.StreamSpec = argument;
					break;
				case "i":
				case "input":
					if(Program.InputFile != null)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.InputFile = argument;
					break;
				case "S":
				case "shell":
					if(Program.Shell != null)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.Shell = argument;
					break;
			}
		}
		
		protected override OperandState OnOperandFound(string operand)
		{
			if(operand == ":")
			{
				if(Program.CommandOperands)
				{
					Program.Command.Add(operand);
				}else{
					Program.CommandOperands = true;
				}
				return OperandState.OnlyOperands;
			}else if(Program.CommandOperands)
			{
				Program.Command.Add(operand);
				return OperandState.OnlyOperands;
			}else{
				Program.Entries.Add(operand);
				return OperandState.ContinueOptions;
			}
		}
	}
}