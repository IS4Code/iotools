/* Date: 12.11.2017, Time: 23:46 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using iotools;

namespace ffcat
{
	class Program
	{
		public static readonly List<Pattern> Patterns = new List<Pattern>();
		public static PatternType? NextPattern;
		public static bool Quiet;
		
		public static decimal? Duration;
		
		public static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			
			var options = new Options();
			try{
				options.Parse(args);
				
				if(!Quiet)
				{
					options.Banner();
					if(Patterns.Count == 0)
					{
						Console.Error.WriteLine("No pattern specified. Use --help for help.");
						return;
					}
				}
				
				Console.WriteLine("ffconcat version 1.0");
				
				foreach(var pattern in Patterns)
				{
					foreach(var file in pattern.FindPaths())
					{
						string escaped = file.Replace("\'", @"\\'").Replace("'", @"\'");
						if(escaped.EndsWith("\\")) escaped += "\\";
						Console.WriteLine("file '{0}'", escaped);
						
						if(pattern.Duration != null)
						{
							Console.WriteLine("duration {0}", pattern.Duration);
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
	
	class Pattern
	{
		public string Text{get; private set;}
		public PatternType Type{get; private set;}
		public decimal? Duration{get; private set;}
		
		private readonly Regex regex;
		
		public Pattern(string pattern, PatternType type, decimal? duration)
		{
			Text = pattern;
			Type = type;
			Duration = duration;
			
			if(Type == PatternType.Regex)
			{
				regex = new Regex(pattern, RegexOptions.Compiled);
			}
		}
		
		public IEnumerable<string> FindPaths()
		{
			switch(Type)
			{
				case PatternType.Static:
					return new[]{Path.GetFullPath(Text)};
				case PatternType.Regex:
					return RegexPaths();
				case PatternType.Glob:
					string[] split = Text.Split(Path.DirectorySeparatorChar);
					if(Path.IsPathRooted(Text))
					{
						var root = new DirectoryInfo(split[0]+Path.DirectorySeparatorChar);
						return EnumInner(root, split, 1);
					}else{
						return EnumInner(new DirectoryInfo("."), split, 0);
					}
				default:
					throw new NotSupportedException();
			}
		}
		
		private IEnumerable<string> RegexPaths()
		{
			foreach(var file in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories))
			{
				if(regex.IsMatch(file))
				{
					yield return file;
				}
			}
		}
		
		private IEnumerable<string> EnumInner(DirectoryInfo root, string[] components, int start)
		{
			string pattern = components[start];
			if(start == components.Length-1)
			{
				foreach(var file in root.EnumerateFiles(pattern))
				{
					yield return file.FullName;
				}
			}else{
				if(pattern.Contains("*") || pattern.Contains("?"))
				{
					foreach(var dir in root.EnumerateDirectories(pattern))
					{
						foreach(var file in EnumInner(dir, components, start+1))
						{
							yield return file;
						}
					}
				}else{
					foreach(var file in EnumInner(new DirectoryInfo(Path.Combine(root.FullName, pattern)), components, start+1))
					{
						yield return file;
					}
				}
			}
		}
	}
	
	enum PatternType
	{
		Static,
		Glob,
		Regex
	}
	
	class Options : ApplicationOptions
	{
		protected override string Usage{
			get{
				return "[options] [pattern...]";
			}
		}
		
		public override void Description()
		{
			base.Description();
			Console.Error.WriteLine();
			Console.Error.Write(" ");
			OutputWrapPad("Use this program to produce an ffmpeg concat format file " +
			              "from a series of patterns.", 1);
		}
		
		public override IList<OptionInfo> GetOptions()
		{
			return new OptionInfoCollection{
				{"q", "quiet", null, "do not print any additional messages"},
				{"p", "pattern-type", "type", "set next pattern type (static, glob, regex)"},
				{"r", "frame-rate", "number", "the frame rate for all images"},
				{"d", "duration", "number", "the duration of all images"},
				{"?", "help", null, "displays this help message"},
			};
		}
		
		protected override void Notes()
		{
			Console.Error.WriteLine();
			Console.Error.WriteLine("Example: "+ExecutableName+" *.png | ffmpeg -i - -");
		}
		
		protected override OptionArgument OnOptionFound(string option)
		{
			switch(option)
			{
				case "q":
				case "quiet":
					if(Program.Quiet)
					{
						throw OptionAlreadySpecified(option);
					}
					Program.Quiet = true;
					return OptionArgument.None;
				case "p":
				case "pattern-type":
					return OptionArgument.Required;
				case "r":
				case "frame-rate":
					return OptionArgument.Required;
				case "d":
				case "duration":
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
				case "p":
				case "pattern-type":
					if(Program.NextPattern != null)
					{
						throw OptionAlreadySpecified(option);
					}
					switch(argument)
					{
						case "glob":
							Program.NextPattern = PatternType.Glob;
							break;
						case "regex":
							Program.NextPattern = PatternType.Regex;
							break;
						default:
							throw ArgumentInvalid(option, "static|glob|regex");
					}
					break;
				case "r":
				case "frame-rate":
					if(Program.Duration != null)
					{
						throw OptionAlreadySpecified(option);
					}
					decimal r;
					if(!Decimal.TryParse(argument, out r))
					{
						throw ArgumentInvalid(option, "number");
					}
					Program.Duration = 1/r;
					break;
				case "d":
				case "duration":
					if(Program.Duration != null)
					{
						throw OptionAlreadySpecified(option);
					}
					decimal d;
					if(!Decimal.TryParse(argument, out d))
					{
						throw ArgumentInvalid(option, "number");
					}
					Program.Duration = d;
					break;
			}
		}
		
		protected override OperandState OnOperandFound(string operand)
		{
			Program.Patterns.Add(new Pattern(operand, Program.NextPattern ?? PatternType.Static, Program.Duration));
			Program.NextPattern = null;
			Program.Duration = null;
			return OperandState.ContinueOptions;
		}
	}
}