/* Date: 4.11.2017, Time: 15:38 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using iotools;

namespace passio
{
	class Program
	{
		public static int? BufferSize;
		public static readonly List<string> InputFiles = new List<string>();
		public static bool Quiet;
		public static bool Verbose;
		public static bool TotalLength;
		
		public static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			
			var options = new Options();
			try{
				options.Parse(args);
				
				if(!Quiet)
				{
					options.Banner();
					if(InputFiles.Count == 0)
					{
						Console.Error.WriteLine("No input files given. Use --help for help. Use -q to remove this message.");
					}
				}
				
				int bufferSize = BufferSize ?? 4096;
				
				using(var stdout = Console.OpenStandardOutput(bufferSize))
				{
					long length = 0;
					byte[] buffer = new byte[bufferSize];
					
					if(InputFiles.Count == 0) InputFiles.Add("-");
					
					foreach(string input in InputFiles)
					{
						if(!Program.TotalLength)
						{
							length = 0;
						}
						using(var stream = input == "-" ? Console.OpenStandardInput(bufferSize) : new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
						{
							int count;
							while((count = stream.Read(buffer, 0, buffer.Length)) != 0)
							{
								stdout.Write(buffer, 0, count);
								
								length += count;
							}
						}
						if(!Quiet && !TotalLength)
						{
							if(Verbose)
							{
								string name = input == "-" ? "input" : Path.GetFileName(input);
								options.Log("Length of "+name+": "+length);
							}else{
								Console.Error.WriteLine(length);
							}
						}
					}
					
					if(!Quiet && TotalLength)
					{
						if(Verbose)
						{
							options.Log("Total length: "+length);
						}else{
							Console.Error.WriteLine(length);
						}
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
				return "[options] [input...]";
			}
		}
		
		public override void Description()
		{
			base.Description();
			Console.Error.WriteLine();
			Console.Error.Write(" ");
			OutputWrapPad("Use this program to concatenate data from files or the " +
			              "standard input and pass it to the standard output. By " +
			              "default, the file sizes are written to the standard error " +
			              "stream.", 1);
		}
		
		public override IList<OptionInfo> GetOptions()
		{
			return new OptionInfoCollection{
				{"q", "quiet", null, "do not print any additional messages"},
				{"v", "verbose", null, "more detailed information"},
				{"t", "total-length", null, "display the total length instead of individual lengths"},
				{"b", "buffer-size", "size", "sets the size of the buffers (default 4096)"},
				{"?", "help", null, "displays this help message"},
			};
		}
		
		protected override void Notes()
		{
			Console.Error.WriteLine();
			Console.Error.WriteLine("Example: "+ExecutableName+" a.txt b.txt c.txt >abc.txt");
		}
		
		protected override OptionArgument OnOptionFound(string option)
		{
			switch(option)
			{
				case "b":
				case "buffer-size":
					return OptionArgument.Required;
				case "q":
				case "quiet":
					if(Program.Quiet)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.Quiet = true;
					return OptionArgument.None;
				case "v":
				case "verbose":
					if(Program.Verbose)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.Verbose = true;
					return OptionArgument.None;
				case "t":
				case "total-length":
					if(Program.TotalLength)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.TotalLength = true;
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
			}
		}
		
		protected override OperandState OnOperandFound(string operand)
		{
			Program.InputFiles.Add(operand);
			return OperandState.ContinueOptions;
		}
	}
}