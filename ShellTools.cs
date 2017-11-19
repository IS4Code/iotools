/* Date: 19.11.2017, Time: 19:54 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace iotools
{
	public static class ShellTools
	{
		public static void CreateCommandLine(string command, out string fileName, out string arguments, params string[] variables)
		{
			CreateCommandLine(command, out fileName, out arguments, (IEnumerable<string>)variables);
		}
		
		public static void CreateCommandLine(string command, out string fileName, out string arguments, IEnumerable<string> variables)
		{
			bool windows;
			
			fileName = Environment.GetEnvironmentVariable("COMSPEC");
			string cmdArgs;
			if(fileName != null)
			{
				string pattern = String.Format(@"\$(?:(?<v>{0})|\{{(?<v>{1})\}})", String.Join("|", variables.Select(v => Regex.Escape(v)+"\\b")), String.Join("|", variables.Select(v => Regex.Escape(v))));
				
				command = Regex.Replace(command, pattern, "%${v}%");
				cmdArgs = "/C ";
				windows = true;
			}else{
				fileName = Environment.GetEnvironmentVariable("SHELL");
				if(fileName != null)
				{
					cmdArgs = "-c ";
					windows = false;
				}else{
					fileName = "cmd";
					cmdArgs = "/C ";
					windows = true;
				}
			}
			
			if(!windows)
			{
				command = Regex.Replace(command, @"(\\|"")", @"\$1");
			}
			
			arguments = cmdArgs+"\""+command+"\"";
		}
	}
}