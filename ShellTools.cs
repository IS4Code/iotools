/* Date: 19.11.2017, Time: 19:54 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace iotools
{
	public static class ShellTools
	{
		public static void CreateCommandLine(IList<string> cmdlist, out string fileName, out string arguments, params string[] variables)
		{
			CreateCommandLine(cmdlist, out fileName, out arguments, (IEnumerable<string>)variables);
		}
		
		public static void CreateCommandLine(IList<string> cmdlist, out string fileName, out string arguments, IEnumerable<string> variables)
		{
			bool windows;
			GetSystemShell(out windows, out fileName, out arguments);
			
			if(cmdlist == null) return;
			
			if(windows)
			{
				Regex varRegex = new Regex(String.Format(@"\$(?:(?<v>{0})|\{{(?<v>{1})\}})", String.Join("|", variables.Select(v => Regex.Escape(v)+"\\b")), String.Join("|", variables.Select(v => Regex.Escape(v)))));
				Func<string, string> argReplacer = s => WindowsEscapeArgument(varRegex.Replace(s, "%${v}%"));
				
				arguments += String.Format(" \"{0}\"", String.Join(" ", cmdlist.Select(argReplacer)));
			}else{
				arguments += " "+CreateArgumentsPosix(cmdlist);
			}
		}
		
		public static string CreateArgumentsPosix(IList<string> arguments)
		{
			return String.Join(" ", arguments.Select(PosixEscapeArgument));
		}
		
		public static void CreateCommandLine(string command, out string fileName, out string arguments, params string[] variables)
		{
			CreateCommandLine(command, out fileName, out arguments, (IEnumerable<string>)variables);
		}
		
		public static void CreateCommandLine(string command, out string fileName, out string arguments, IEnumerable<string> variables)
		{
			bool windows;
			GetSystemShell(out windows, out fileName, out arguments);
			
			if(windows)
			{
				Regex varRegex = new Regex(String.Format(@"\$(?:(?<v>{0})|\{{(?<v>{1})\}})", String.Join("|", variables.Select(v => Regex.Escape(v)+"\\b")), String.Join("|", variables.Select(v => Regex.Escape(v)))));
				command = varRegex.Replace(command, "%${v}%");
				
				arguments += String.Format(" \"{0}\"", command);
			}else{
				arguments += String.Format(" {0}", command);
			}
		}
		
		public static string EscapeArgument(string arg)
		{
			bool comspec;
			string fileName, arguments;
			GetSystemShell(out comspec, out fileName, out arguments);
			if(comspec)
			{
				return WindowsEscapeArgument(arg);
			}else{
				return PosixEscapeArgument(arg);
			}
		}
		
		static string WindowsEscapeArgument(string arg)
		{
			if(arg.Any(c => c == '"' || c == ' ' || c == '\t' || c == '\v'))
			{
				return "\""+arg.Replace("\"", "\"\"")+"\"";
			}else{
				return arg;
			}
		}
		
		static string PosixEscapeArgument(string arg)
		{
			return "\""+Regex.Replace(arg, @"(\\|"")", @"\$1")+"\"";
		}
		
		static void GetSystemShell(out bool comspec, out string fileName, out string arguments)
		{
			fileName = Environment.GetEnvironmentVariable("COMSPEC");
			if(fileName != null)
			{
				comspec = true;
			}else{
				fileName = Environment.GetEnvironmentVariable("SHELL");
				if(fileName != null)
				{
					comspec = false;
				}else{
					fileName = "cmd";
					comspec = true;
				}
			}
			
			if(comspec)
			{
				arguments = "/C";
			}else{
				arguments = "-c";
			}
		}
	}
}