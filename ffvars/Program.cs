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
		
		public static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			
			var options = new Options();
			try{
				options.Parse(args);
				
				//if(!Quiet && !Verbose)
				{
					options.Banner();
					if(Entries.Count == 0)
					{
						Console.Error.WriteLine("No entries specified. Use --help for help. Use -v to remove this message.");
					}
					foreach(var entry in Entries)
					{
						if(entry.IndexOf(':') != -1)
						{
							Console.Error.WriteLine("Error: ':' cannot be a part of an entry specifier ({0}).", entry);
							return;
						}
					}
				}
				
				int bufferSize = BufferSize ?? 4096;
			
				string entries = String.Join(":", Entries);
				
				var ffprobeargs = new List<string>();
				ffprobeargs.Add("ffprobe");
				
				ffprobeargs.Add("-v");
				ffprobeargs.Add("error");
				ffprobeargs.Add("-of");
				ffprobeargs.Add("flat");
				if(Entries.Count > 0)
				{
					ffprobeargs.Add("-show_entries");
					ffprobeargs.Add(String.Join(":", entries));
				}
				if(StreamSpec != null)
				{
					ffprobeargs.Add("-select_streams");
					ffprobeargs.Add(StreamSpec);
				}
				
				ffprobeargs.Add("-");
				
				string fileName, arguments;
				ShellTools.CreateCommandLine(ffprobeargs, out fileName, out arguments);
				
				var start = new ProcessStartInfo(fileName, arguments);
				start.UseShellExecute = false;
				start.RedirectStandardOutput = true;
				start.RedirectStandardInput = true;
				var proc = new Process{StartInfo = start};
				proc.EnableRaisingEvents = true;
				
				proc.Start();
				
				
				var pin = proc.StandardInput.BaseStream;
				using(var input = InputFile == null || InputFile == "-" ? Console.OpenStandardInput(bufferSize) : new FileStream(InputFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
				{
					var inputBuffer = CopyInput ? new MemoryStream() : null;
					byte[] buffer = new byte[bufferSize];
					while(!proc.HasExited)
					{
						int read = input.Read(buffer, 0, buffer.Length);
						if(read == 0) break;
						pin.Write(buffer, 0, buffer.Length);
						if(CopyInput)
						{
							inputBuffer.Write(buffer, 0, buffer.Length);
						}
					}
				
					ShellTools.CreateCommandLine(Command, out fileName, out arguments);
					
					var cmdstart = new ProcessStartInfo(fileName, arguments);
					cmdstart.UseShellExecute = false;
					cmdstart.RedirectStandardInput = true;
					
					string param;
					while((param = proc.StandardOutput.ReadLine()) != null)
					{
						options.Log(param);
						ParseEntry(param, cmdstart.EnvironmentVariables);
					}
					
					proc = new Process{StartInfo = cmdstart};
					
					proc.Start();
					
					if(CopyInput)
					{
						pin = proc.StandardInput.BaseStream;
						
						inputBuffer.Position = 0;
						while(!proc.HasExited)
						{
							int read = inputBuffer.Read(buffer, 0, buffer.Length);
							if(read == 0) break;
							pin.Write(buffer, 0, buffer.Length);
						}
						while(!proc.HasExited)
						{
							int read = input.Read(buffer, 0, buffer.Length);
							if(read == 0) break;
							pin.Write(buffer, 0, buffer.Length);
						}
					}
				}
				
				if(!Exit)
				{
					proc.WaitForExit();
				}
			}catch(Exception e)
			{
				//if(!Quiet && !Verbose)
				{
					options.Log(e.Message);
				}
			}
		}
		
		static readonly Regex entryRegex = new Regex(@"^(?<name>.+?)=(?:""(?<value>.*)""|(?<value>.*))$", RegexOptions.Compiled);
		public static bool ParseEntry(string entry, StringDictionary environment)
		{
			var match = entryRegex.Match(entry);
			if(!match.Success) return false;
			
			string name = match.Groups["name"].Value;
			string value = match.Groups["value"].Value;
			
			environment[name] = RemoveBackslashes(value);
			
			return true;
		}
		
		static readonly Regex backslashRegex = new Regex(@"\\+", RegexOptions.Compiled);
		public static string RemoveBackslashes(string str)
		{
			return backslashRegex.Replace(str, m => new String('\\', m.Value.Length/2));
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
				{"e", "exit", null, "do not wait for the inner command to exit"},
				{"b", "buffer-size", "size", "sets the size of the buffers (default 4096)"},
				{"?", "help", null, "displays this help message"},
			};
		}
		
		protected override void Notes()
		{
			Console.Error.WriteLine();
			Console.Error.WriteLine("Example: "+ExecutableName+" format.duration : echo %format.duration% <file.avi");
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
				case "c":
				case "copy":
					if(Program.CopyInput)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.CopyInput = true;
					return OptionArgument.None;
				case "s":
				case "streams":
					return OptionArgument.Required;
				case "i":
				case "input":
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