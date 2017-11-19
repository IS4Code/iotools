/* Date: 5.11.2017, Time: 4:02 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using iotools;

namespace imgio
{
	class Program
	{
		public static bool Quiet;
		public static bool Verbose;
		public static bool Debug;
		public static bool Copy;
		public static int? BufferSize;
		public static string Extension;
		public static readonly List<string> Command = new List<string>();
		
		public static string InputName;
		public static string OutputName;
		
		public static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			
			var options = new Options();
			try{
				options.Parse(args);
				
				if(!Quiet && !Verbose)
				{
					options.Banner();
					if(Copy && Command.Count > 0)
					{
						Console.Error.WriteLine("You cannot specify any command with the -C option.");
						return;
					}else if(!Copy && Command.Count == 0)
					{
						Console.Error.WriteLine("No command specified. Use --help for help.");
						return;
					}
				}
				
				int bufferSize = BufferSize ?? 4096;
				
				string cmd = String.Join(" ", Command);
				
				string inName = InputName ?? "IMG_IN";
				string outName = OutputName ?? "IMG_OUT";
				
				string cmdExe, cmdline;
				ShellTools.CreateCommandLine(cmd, out cmdExe, out cmdline, inName, outName);
				
				using(var stdin = Console.OpenStandardInput(bufferSize))
				{
					int count = 0;
					Extractor ex;
					while((ex = Extractor.Create(stdin)) != null)
					{
						if(!Quiet)
						{
							if(Verbose)
							{
								options.Log((count++).ToString());
							}else{
								options.Log(ex.Description+" image #"+(count++)+" found.");
							}
							
							if(Debug)
							{
								ex.Logger = options.Log;
							}
						}
						
						ex.BufferSize = bufferSize;
						
						if(!Copy)
						{
							string inPath = GetTempPath(Extension ?? ex.Extension);
							string outPath = GetTempPath(Extension ?? ex.Extension);
							
							using(var output = new FileStream(inPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize))
							{
								ex.Extract(output);
							}
							
							var start = new ProcessStartInfo(cmdExe, cmdline);
							start.UseShellExecute = false;
							start.RedirectStandardInput = true;
							start.RedirectStandardOutput = true;
							
							start.EnvironmentVariables[inName] = inPath;
							start.EnvironmentVariables[outName] = outPath;
							
							var proc = new Process{StartInfo = start};
							
							proc.Start();
							
							using(var stderr = Console.OpenStandardError(bufferSize))
							{
								var stdout = proc.StandardOutput.BaseStream;
								stdout.CopyTo(stderr, bufferSize);
							}
							
							proc.WaitForExit();
							
							if(File.Exists(outPath))
							{
								using(var stdout = Console.OpenStandardOutput(bufferSize))
								using(var inFile = new FileStream(outPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
								{
									inFile.CopyTo(stdout, bufferSize);
								}
								File.Delete(outPath);
							}else{
								if(!Quiet && !Verbose)
								{
									options.Log("No image was written to the output path, skipping!");
								}
							}
							
							File.Delete(inPath);
						}else{
							using(var stdout = Console.OpenStandardOutput(bufferSize))
							{
								ex.Extract(stdout);
							}
						}
					}
				}
			}catch(Exception e)
			{
				if(!Quiet && !Verbose)
				{
					options.Log(e.Message);
				}
			}
		}
		
		private static string GetTempPath(string extension)
		{
			return Path.ChangeExtension(Path.GetTempFileName(), extension);
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
			OutputWrapPad("This program attempts to find image data in the standard input, " +
			              "extract them to a temporary location, and run a specified image " +
			              "processor, writing its output to the standard output.", 1);
		}
		
		public override IList<OptionInfo> GetOptions()
		{
			return new OptionInfoCollection{
				{"q", "quiet", null, "do not print any additional messages"},
				{"v", "verbose", null, "display less information"},
				{"d", "debug", null, "print debug messages"},
				{"e", "extension", "ext", "force an extension for the temporary paths"},
				{"i", "in-name", "varname", "sets the name of the input image (default IMG_IN)"},
				{"o", "out-name", "varname", "sets the name of the output image (default IMG_OUT)"},
				{"c", "contained", null, "the program won't redirect the internal process' standard streams"},
				{"b", "buffer-size", "size", "sets the size of the buffers (default 4096)"},
				{"C", "copy", null, "no command will be run, it simply detects the images"},
				{"?", "help", null, "displays this help message"},
			};
		}
		
		protected override void Notes()
		{
			Console.Error.WriteLine();
			Console.Error.WriteLine("Example: "+ExecutableName+" copy $IMG_IN $IMG_OUT <image.png >same.png");
			Console.Error.WriteLine();
			Console.Error.WriteLine("Supported formats: PNG, JPEG, GIF, BMP, RIFF, IFF.");
			Console.Error.WriteLine("Standard output from the inner command is redirected to the standard error.");
			Console.Error.WriteLine("The command-line interpreter is determined from the COMSPEC and SHELL environment variables. " +
			                        "If CONHOST is specified, POSIX-style variables can be used in the command and they will be replaced " +
			                        "with the correct CMD.EXE syntax.");
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
				case "d":
				case "debug":
					if(Program.Debug)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.Debug = true;
					return OptionArgument.None;
				case "C":
				case "copy":
					if(Program.Copy)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.Copy = true;
					return OptionArgument.None;
				case "e":
				case "extension":
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
				case "e":
				case "extension":
					if(Program.Extension != null)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.Extension = argument;
					break;
				case "i":
				case "in-name":
					if(Program.InputName != null)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.InputName = argument;
					break;
				case "o":
				case "out-name":
					if(Program.OutputName != null)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.OutputName = argument;
					break;
			}
		}
		
		protected override OperandState OnOperandFound(string operand)
		{
			Program.Command.Add(operand);
			return OperandState.OnlyOperands;
		}
	}
}