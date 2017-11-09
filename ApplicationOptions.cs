/* Date: 6.11.2017, Time: 2:27 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace iotools
{
	public abstract class ApplicationOptions
	{
		protected Assembly Assembly{
			get{
				return Assembly.GetExecutingAssembly();
			}
		}
		
		public string ApplicationName{
			get{
				return Assembly.GetName().Name;
			}
		}
		
		public string ExecutableName{
			get{
				return Path.GetFileNameWithoutExtension(Assembly.Location);
			}
		}
		
		public virtual void Banner()
		{
			var asm = Assembly;
			string msg = "";
			
			var copyright = asm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
			if(copyright.Length > 0)
			{
				msg = ((AssemblyCopyrightAttribute)copyright[0]).Copyright+" ";
			}
			var title = asm.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
			if(title.Length > 0)
			{
				msg += ((AssemblyTitleAttribute)title[0]).Title+" by ";
			}
			var author = asm.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
			if(author.Length > 0)
			{
				msg += ((AssemblyCompanyAttribute)author[0]).Company;
			}
			Console.Error.WriteLine(msg);
		}
		
		public virtual void Description()
		{
			foreach(AssemblyDescriptionAttribute desc in Assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false))
			{
				Console.Error.WriteLine(desc.Description);
			}
		}
		
		public virtual IList<OptionInfo> GetOptions()
		{
			return new OptionInfoCollection();
		}
		
		protected virtual string Usage{
			get{
				return "";
			}
		}
		
		protected void Help()
		{
			Banner();
			Description();
			Console.Error.WriteLine();
			Console.Error.WriteLine("Usage: {0} {1}", ExecutableName, Usage);
			Console.Error.WriteLine();
			
			string optFormat = " -{0} [ --{1} ] {2} ";
			var options = GetOptions();
			
			int colLength = options.Max(o => String.Format(optFormat, o.ShortName, o.LongName, o.ArgumentText).Length);
			
			foreach(var opt in options)
			{
				string usage = String.Format(optFormat, opt.ShortName, opt.LongName, opt.ArgumentText);
				Console.Error.Write(usage);
				Console.Error.Write(new string(' ', colLength-usage.Length));
				OutputWrapPad(opt.Description, colLength, Console.WindowWidth-colLength, Console.Error);
			}
			
			Notes();
			
			Environment.Exit(0);
		}
		
		protected virtual void Notes()
		{
			
		}
		
		public void OutputWrapPad(string text, int padLeft)
		{
			OutputWrapPad(text, padLeft, Console.WindowWidth-padLeft);
		}
		
		public void OutputWrapPad(string text, int padLeft, int textWidth)
		{
			OutputWrapPad(text, padLeft, textWidth, Console.Error);
		}
		
		public void OutputWrapPad(string text, int padLeft, int textWidth, TextWriter writer)
		{
			int totalLength = 0;
			foreach(string s in text.Split(' '))
			{
				//Console.Error.Write(totalLength);
				bool first = totalLength == 0;
				totalLength += s.Length;
				if(totalLength >= textWidth-1)
				{
					writer.WriteLine();
					if(padLeft > 0)
					{
						writer.Write(new string(' ', padLeft));
					}
					totalLength = s.Length;
					first = true;
				}
				if(!first)
				{
					writer.Write(" ");
					totalLength += 1;
				}
				writer.Write(s);
			}
			if(totalLength > 0)
			{
				writer.WriteLine();
			}
		}
		
		public void Log(string message)
		{
			Console.Error.WriteLine("[{0}] {1}", ApplicationName, message);
		}
		
		protected abstract OptionArgument OnOptionFound(string option);
		protected abstract void OnOptionArgumentFound(string option, string argument);
		protected abstract OperandState OnOperandFound(string operand);
		
		protected virtual OptionArgument OnShortOptionFound(char option)
		{
			return OnOptionFound(option.ToString());
		}
		
		protected virtual void OnShortOptionArgumentFound(char option, string argument)
		{
			OnOptionArgumentFound(option.ToString(), argument);
		}
		
		
		public ApplicationOptions()
		{
			
		}
		
		public void Parse(string[] args)
		{
			bool operands = false;
			for(int i = 0; i < args.Length; i++)
			{
				string arg = args[i];
				if(operands)
				{
					OnOperandFound(arg);
				}else if(arg == "--")
				{
					operands = true;
				}else if(arg.StartsWith("--"))
				{
					int delim = arg.IndexOf('=');
					if(delim != -1)
					{
						string name = arg.Substring(2, delim-2);
						if(OnOptionFound(name) == OptionArgument.None)
						{
							throw ArgumentNotExpected(name);
						}
						string argument = arg.Substring(delim+1);
						OnOptionArgumentFound(name, argument);
					}else{
						string name = arg.Substring(2);
						switch(OnOptionFound(name))
						{
							case OptionArgument.Optional:
								OnOptionArgumentFound(name, null);
								break;
							case OptionArgument.Required:
								if(++i >= args.Length) throw ArgumentExpected(name);
								OnOptionArgumentFound(name, args[i]);
								break;
						}
					}
				}else if(arg.Length > 1 && arg[0] == '-' && IsOptionChar(arg[1]))
				{
					for(int j = 1; j < arg.Length; j++)
					{
						char opt = arg[j];
						string argument = String.Join("", arg.Skip(j+1).TakeWhile(c => !IsOptionChar(c)));
						
						switch(OnShortOptionFound(opt))
						{
							case OptionArgument.None:
								if(argument.Length > 0) throw ArgumentNotExpected(opt);
								break;
							case OptionArgument.Optional:
								if(argument.Length > 0)
								{
									OnShortOptionArgumentFound(opt, argument);
								}else{
									OnShortOptionArgumentFound(opt, null);
								}
								break;
							case OptionArgument.Required:
								if(argument.Length > 0)
								{
									OnShortOptionArgumentFound(opt, argument);
								}else{
									if(j+1 < arg.Length)
									{
										OnShortOptionArgumentFound(opt, arg.Substring(j+1));
										j = arg.Length-1;
									}else if(++i >= args.Length)
									{
										throw ArgumentExpected(opt);
									}else{
										OnShortOptionArgumentFound(opt, args[i]);
									}
								}
								break;
						}
						j += argument.Length;
					}
				}else{
					if(OnOperandFound(arg) == OperandState.OnlyOperands)
					{
						operands = true;
					}
				}
			}
		}
		
		private static bool IsOptionChar(char c)
		{
			return c == '?' || Char.IsLetter(c);
		}
		
		public ApplicationException UnrecognizedOption(char option)
		{
			return UnrecognizedOption(option.ToString());
		}
		
		public ApplicationException UnrecognizedOption(string option)
		{
			return new ApplicationException("Unrecognized option '"+option+"'.");
		}
		
		public ApplicationException ArgumentExpected(char option)
		{
			return ArgumentExpected(option.ToString());
		}
		
		public ApplicationException ArgumentExpected(string option)
		{
			return new ApplicationException("Argument expected for option '"+option+"'.");
		}
		
		public ApplicationException ArgumentNotExpected(char option)
		{
			return ArgumentNotExpected(option.ToString());
		}
		
		public ApplicationException ArgumentNotExpected(string option)
		{
			return new ApplicationException("Argument not expected for option '"+option+"'.");
		}
		
		public ApplicationException ArgumentInvalid(char option, string expected)
		{
			return ArgumentInvalid(option.ToString(), expected);
		}
		
		public ApplicationException ArgumentInvalid(string option, string expected)
		{
			return new ApplicationException("Invalid argument provided for option '"+option+"', "+expected+" expected.");
		}
		
		public ApplicationException OptionAlreadySpecified(char option)
		{
			return OptionAlreadySpecified(option.ToString());
		}
		
		public ApplicationException OptionAlreadySpecified(string option)
		{
			return new ApplicationException("Option '"+option+"' has been already specified.");
		}
		
		public class OptionInfoCollection : List<OptionInfo>
		{
			public OptionInfoCollection()
			{
				
			}
			
			public OptionInfoCollection(int capacity) : base(capacity)
			{
				
			}
			
			public OptionInfoCollection(IEnumerable<OptionInfo> collection) : base(collection)
			{
				
			}
			
			public void Add(string shortName, string longName, string argument, string description)
			{
				Add(new OptionInfo(shortName, longName, argument, description));
			}
		}
		
		public struct OptionInfo
		{
			public string ShortName{get; private set;}
			public string LongName{get; private set;}
			public string Argument{get; private set;}
			public string Description{get; private set;}
			
			public string ArgumentText{
				get{
					return Argument == null ? "" : Argument+" ";
				}
			}
			
			public OptionInfo(string shortName, string longName, string argument, string description) : this()
			{
				ShortName = shortName;
				LongName = longName;
				Argument = argument;
				Description = description;
			}
		}
	}
	
	public enum OptionArgument
	{
		None, Optional, Required
	}
	
	public enum OperandState
	{
		ContinueOptions, OnlyOperands
	}
	
	public class DebugOptions : ApplicationOptions
	{
		protected override OptionArgument OnShortOptionFound(char option)
		{
			Console.WriteLine("Option "+option);
			return option == 'r' ? OptionArgument.Required : option == 'o' ? OptionArgument.Optional : 0;
		}
		
		protected override void OnShortOptionArgumentFound(char option, string argument)
		{
			Console.WriteLine("Option "+option+"="+argument);
		}
		
		protected override OptionArgument OnOptionFound(string option)
		{
			Console.WriteLine("Long option "+option);
			return option == "req" ? OptionArgument.Required : option == "opt" ? OptionArgument.Optional : 0;
		}
		
		protected override void OnOptionArgumentFound(string option, string argument)
		{
			Console.WriteLine("Long option "+option+"="+argument);
		}
		
		protected override OperandState OnOperandFound(string operand)
		{
			Console.WriteLine("Operand "+operand);
			return OperandState.ContinueOptions;
		}
	}
}
