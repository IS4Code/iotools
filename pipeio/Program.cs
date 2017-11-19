/* Date: 4.11.2017, Time: 12:47 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using iotools;

namespace pipeio
{
	class Program
	{
		public static int? BufferSize;
		public static List<string> Command = new List<string>();
		public static List<string> Pipes = new List<string>();
		public static bool Contained;
		public static bool Quiet;
		public static bool Debug;
		public static string Shell;
		
		public static string PipeInVar;
		public static string PipeOutVar;
		
		public static void Main(string[] args)
		{
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			
			var options = new Options();
			try{
				options.Parse(args);
				
				if(!Quiet)
				{
					options.Banner();
					if(Command.Count == 0)
					{
						Console.Error.WriteLine("No command specified. Use --help for help.");
						return;
					}
				}
			
				string cmd = String.Join(" ", Command);
				
				string inName = PipeInVar ?? "PIPE_IN";
				string outName = PipeOutVar ?? "PIPE_OUT";
				
				string cmdExe, cmdline;
				if(Shell != null)
				{
					cmdExe = Shell;
					cmdline = cmd;
				}else{
					ShellTools.CreateCommandLine(cmd, out cmdExe, out cmdline, (new[]{inName, outName}).Concat(Pipes.SelectMany(p => new[]{p+"_IN", p+"_OUT"})));
				}
				
				var io = new ProcessPipeIo(cmdExe, cmdline, BufferSize ?? 4096);
				
				if(!Quiet && Debug)
				{
					io.Logger = options.Log;
				}
				
				if(!Contained)
				{
					io.StandardOutputRedirect = StandardPipe.Error;
					io.StandardErrorRedirect = StandardPipe.Error;
					
					io.InName = inName;
					io.OutName = outName;
				}
				io.InnerPipes.AddRange(Pipes);
				var t = io.Start();
				t.Wait();
			}catch(AggregateException ae)
			{
				if(!Quiet)
				{
					foreach(var e in ae.InnerExceptions)
					{
						options.Log(e.Message);
						options.Log(e.StackTrace);
					}
				}
			}catch(Exception e)
			{
				if(!Quiet)
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
				return "[options] command";
			}
		}
		
		public override void Description()
		{
			base.Description();
			Console.Error.WriteLine();
			Console.Error.Write(" ");
			OutputWrapPad("This program can be used to create named pipes and export " +
			              "them for applications that do not use standard input or " +
			              "output streams for data. By default, the program will " +
			              "create named pipes for its standard input and output streams " +
			              "and export them in the %PIPE_IN% and %PIPE_OUT% environment " +
			              "variables as Win32 paths. For the lifetime of the process, " +
			              "additional internal pipes may be created.", 1);
		}
		
		public override IList<OptionInfo> GetOptions()
		{
			return new OptionInfoCollection{
				{"q", "quiet", null, "do not print any additional messages"},
				{"d", "debug", null, "print debug messages"},
				{"p", "pipe", "varname", "creates an internal pipe and exports it as %varname_IN% and %varname_OUT%"},
				{"i", "in-name", "varname", "sets the name of the input pipe (default PIPE_IN)"},
				{"o", "out-name", "varname", "sets the name of the output pipe (default PIPE_OUT)"},
				{"c", "contained", null, "the program won't create i/o pipes"},
				{"b", "buffer-size", "size", "sets the size of the buffers (default 4096)"},
				{"S", "shell", "program", "specifies the interpreter to run inner commands"},
				{"?", "help", null, "displays this help message"},
			};
		}
		
		protected override void Notes()
		{
			Console.Error.WriteLine();
			Console.Error.WriteLine("Example: echo Hello | "+ExecutableName+" \"type %PIPE_IN% > %PIPE_OUT%\"");
			Console.Error.WriteLine();
			Console.Error.WriteLine("Standard output from the inner command is redirected to the standard error.");
		}
		
		protected override OperandState OnOperandFound(string operand)
		{
			Program.Command.Add(operand);
			return OperandState.OnlyOperands;
		}
		
		protected override OptionArgument OnOptionFound(string option)
		{
			switch(option)
			{
				case "b":
				case "buffer-size":
				case "p":
				case "pipe":
				case "i":
				case "in-name":
				case "o":
				case "out-name":
					return OptionArgument.Required;
				case "S":
				case "shell":
					return OptionArgument.Required;
				case "c":
				case "contained":
					if(Program.Contained)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.Contained = true;
					return OptionArgument.None;
				case "q":
				case "quiet":
					if(Program.Quiet)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.Quiet = true;
					return OptionArgument.None;
				case "d":
				case "debug":
					if(Program.Debug)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.Debug = true;
					return OptionArgument.None;
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
				case "S":
				case "shell":
					if(Program.Shell != null)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.Shell = argument;
					break;
				case "p":
				case "pipe":
					if(Program.Pipes.Contains(argument))
					{
						throw new ApplicationException("Pipe '"+argument+"' is already defined.");
					}
					Program.Pipes.Add(argument);
					break;
				case "i":
				case "in-name":
					if(Program.PipeInVar != null)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.PipeInVar = argument;
					break;
				case "o":
				case "out-name":
					if(Program.PipeOutVar != null)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.PipeOutVar = argument;
					break;
				default:
					throw UnrecognizedOption(option);
			}
		}
	}
}