/* Date: 17.11.2017, Time: 21:30 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using iotools;

namespace logio
{
	class Program
	{
		public static int? BufferSize;
		public static bool Quiet;
		public static TimeSpan? Utc;
		public static string DateFormat;
		public static string Culture;
		
		public static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			
			var options = new Options();
			try{
				options.Parse(args);
				
				if(!Quiet)
				{
					options.Banner();
				}
				
				int bufferSize = BufferSize ?? 4096;
				string format = DateFormat ?? "[{}] ";
				
				var formatCulture = Culture != null ? CultureInfo.GetCultureInfo(Culture) : CultureInfo.InstalledUICulture;
				
				format = Regex.Replace(format, @"{(.)", m => m.Groups[1].Value == "{" ? "{{" : "{0:"+m.Groups[1].Value);
				
				Func<string> formatter = ()=>String.Format(formatCulture, format, Utc != null ? DateTime.UtcNow+Utc.Value : DateTime.Now);
				
				using(var stdout = Console.OpenStandardOutput(bufferSize))
				{
					byte[] buffer = new byte[bufferSize];
					State state = State.LineNew;
					
					using(var stdin = Console.OpenStandardInput(bufferSize))
					{
						int count;
						while((count = stdin.Read(buffer, 0, buffer.Length)) != 0)
						{
							for(int i = 0; i < count; i++)
							{
								byte b = buffer[i];
								switch((char)b)
								{
									case '\r':
										if(state == State.LineNew)
										{
											Console.Write(formatter());
										}
										state = State.LineReturn;
										break;
									case '\n':
										switch(state)
										{
											case State.LineReturn:
												state = State.LineNew;
												Console.WriteLine();
												break;
											case State.LineNew:
												Console.WriteLine(formatter());
												break;
											default:
												state = State.LineNew;
												break;
										}
										break;
									default:
										if(state != State.Line)
										{
											if(state == State.LineReturn)
											{
												Console.WriteLine();
											}
											Console.Write(formatter());
											state = State.Line;
										}
										stdout.WriteByte(b);
										break;
								}
							}
						}
					}
					
					if(state == State.LineReturn)
					{
						Console.WriteLine();
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
		
		enum State
		{
			LineNew,
			Line,
			LineReturn,
		}
	}
	
	class Options : ApplicationOptions
	{
		protected override string Usage{
			get{
				return "[options]";
			}
		}
		
		public override void Description()
		{
			base.Description();
			Console.Error.WriteLine();
			Console.Error.Write(" ");
			OutputWrapPad("This programs reads lines from the standard input, " +
			              "and prepends them with the current date and time, " +
			              "formatted in a user-specified way.", 1);
		}
		
		public override IList<OptionInfo> GetOptions()
		{
			return new OptionInfoCollection{
				{"q", "quiet", null, "do not print any additional messages"},
				{"u", "utc", "(offset)", "display dates in UTC"},
				{"f", "format", "string", "sets the log prefix format (default [{}] )"},
				{"c", "culture", "name", "specifies the culture used in date formatting (default system)"},
				{"b", "buffer-size", "size", "sets the size of the buffers (default 4096)"},
				{"?", "help", null, "displays this help message"},
			};
		}
		
		protected override void Notes()
		{
			//Console.Error.WriteLine();
			//Console.Error.WriteLine("Example: "+ExecutableName+" ");
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
				case "c":
				case "culture":
					return OptionArgument.Required;
				case "u":
				case "utc":
					return OptionArgument.Optional;
				case "f":
				case "format":
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
				case "u":
				case "utc":
					if(Program.Utc != null)
					{
						throw OptionAlreadySpecified(option);
					}
					if(argument != null)
					{
						int offset = Int32.Parse(argument);
						Program.Utc = TimeSpan.FromHours(offset);
					}
					break;
				case "f":
				case "format":
					if(Program.DateFormat != null)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.DateFormat = argument;
					break;
				case "c":
				case "culture":
					if(Program.Culture != null)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.Culture = argument;
					break;
			}
		}
		
		protected override OperandState OnOperandFound(string operand)
		{
			throw new ApplicationException("No operand expected.");
		}
	}
}