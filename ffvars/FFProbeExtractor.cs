/* Date: 27.12.2017, Time: 0:09 */
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using iotools;

namespace ffvars
{
	public class FFProbeExtractor
	{
		public int BufferSize{get; set;}
		public Action<string> Logger{get; set;}
		
		public bool Exit;
		public bool CopyInput;
		public bool LiteralLine;
		
		protected void Log(string msg)
		{
			var logger = Logger;
			if(logger != null)
			{
				logger(msg);
			}
		}
		
		public FFProbeExtractor()
		{
			
		}
		
		public void Run(string inputFile, IList<string> entries, string streamSpec, IList<string> command)
		{
			Process ffprobe = CreateFFProbe(entries, streamSpec);
			ffprobe.Start();
			
			Process proc;
			
			Stream input = (inputFile == null || inputFile == "-") ? Console.OpenStandardInput(BufferSize) : new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);
			if(CopyInput)
			{
				using(input)
				{
					var inputBuffer = new MemoryStream();
					CopyToProcessBuffered(input, ffprobe, inputBuffer, BufferSize);
					
					proc = CreateCommand(ffprobe.StandardOutput, command);
					proc.Start();
					
					inputBuffer.Position = 0;
					CopyToProcessBuffered(inputBuffer, proc, null, BufferSize);
					CopyToProcessBuffered(input, proc, null, BufferSize);
				}
			}else{
				using(input)
				{
					CopyToProcessBuffered(input, ffprobe, null, BufferSize);
				}
				
				proc = CreateCommand(ffprobe.StandardOutput, command);
				proc.Start();
			}
			proc.StandardInput.Close();
			
			if(!Exit)
			{
				proc.WaitForExit();
			}
		}
		
		
		void CopyToProcessBuffered(Stream input, Process proc, Stream bufferStream, int bufferSize)
		{
			var pin = proc.StandardInput.BaseStream;
			
			byte[] buffer = new byte[bufferSize];
			while(!proc.HasExited)
			{
				int read = input.Read(buffer, 0, buffer.Length);
				if(read == 0) break;
				pin.Write(buffer, 0, read);
				
				if(bufferStream != null)
				{
					bufferStream.Write(buffer, 0, read);
				}
			}
		}
		
		Process CreateFFProbe(IList<string> entriesList, string stream)
		{
			string entries = String.Join(":", entriesList);
			
			var ffprobeargs = new List<string>();
			ffprobeargs.Add("ffprobe");
			
			ffprobeargs.Add("-v");
			ffprobeargs.Add("error");
			ffprobeargs.Add("-of");
			ffprobeargs.Add("flat");
			if(entriesList.Count > 0)
			{
				ffprobeargs.Add("-show_entries");
				ffprobeargs.Add(String.Join(":", entries));
			}
			if(stream != null)
			{
				ffprobeargs.Add("-select_streams");
				ffprobeargs.Add(stream);
			}
			
			ffprobeargs.Add("-");
			
			string fileName, arguments;
			ShellTools.CreateCommandLine(ffprobeargs, out fileName, out arguments);
			
			var start = new ProcessStartInfo(fileName, arguments);
			start.UseShellExecute = false;
			start.RedirectStandardOutput = true;
			start.RedirectStandardInput = true;
			var proc = new Process{StartInfo = start};
			
			Log("Running ffprobe...");
			Log(fileName+" "+arguments);
			return proc;
		}
		
		//static readonly Regex colonRegex = new Regex(@"[ \t\v]+\:[ \t\v]+", RegexOptions.Compiled);
		Process CreateCommand(StreamReader entryReader, IList<string> command)
		{
			string fileName, arguments;
			
			if(LiteralLine)
			{
				string cmdline = Environment.CommandLine;
				
				int pos = cmdline.IndexOf(':');
				if(pos == -1) throw new ApplicationException("Cannot find ':' in the command line.");
				
				ShellTools.CreateCommandLine(cmdline.Substring(pos+1), out fileName, out arguments);
			}else{
				ShellTools.CreateCommandLine(command, out fileName, out arguments);
			}
			
			var cmdstart = new ProcessStartInfo(fileName, arguments);
			cmdstart.UseShellExecute = false;
			cmdstart.RedirectStandardInput = true;
			
			string entry;
			while((entry = entryReader.ReadLine()) != null)
			{
				Log(entry);
				ParseEntry(entry, cmdstart.EnvironmentVariables);
			}
			
			var proc = new Process{StartInfo = cmdstart};
			Log("Running command...");
			Log(fileName+" "+arguments);
			return proc;
		}
		
		static readonly Regex entryRegex = new Regex(@"^(?<name>.+?)=(?:""(?<value>.*)""|(?<value>.*))$", RegexOptions.Compiled);
		bool ParseEntry(string entry, StringDictionary environment)
		{
			var match = entryRegex.Match(entry);
			if(!match.Success) return false;
			
			string name = match.Groups["name"].Value;
			string value = match.Groups["value"].Value;
			
			environment[name] = RemoveBackslashes(value);
			
			return true;
		}
		
		static readonly Regex backslashRegex = new Regex(@"\\+", RegexOptions.Compiled);
		string RemoveBackslashes(string str)
		{
			return backslashRegex.Replace(str, m => new String('\\', m.Value.Length/2));
		}
	}
}
