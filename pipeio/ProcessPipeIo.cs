/* Date: 4.11.2017, Time: 13:27 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pipeio
{
	public class ProcessPipeIo
	{
		public string Name{get; set;}
		public string Arguments{get; set;}
		public int BufferSize{get; set;}
		
		public string InName{get; set;}
		public string OutName{get; set;}
		
		public StandardPipe StandardOutputRedirect{get; set;}
		public StandardPipe StandardErrorRedirect{get; set;}
		
		public List<string> InnerPipes{get; private set;}
		
		public Action<string> Logger{get; set;}
		
		protected void Log(string msg)
		{
			var logger = Logger;
			if(logger != null)
			{
				logger(msg);
			}
		}
		
		public ProcessPipeIo(string name, string arguments="", int bufferSize=4096)
		{
			Name = name;
			Arguments = arguments;
			BufferSize = bufferSize;
			InnerPipes = new List<string>();
		}
		
		public Task Start()
		{
			var start = new ProcessStartInfo(Name, Arguments);
			start.UseShellExecute = false;
			if(InName != null)
			{
				start.RedirectStandardInput = true;
			}
			if(StandardOutputRedirect != StandardPipe.None)
			{
				start.RedirectStandardOutput = true;
			}
			if(StandardErrorRedirect != StandardPipe.None)
			{
				start.RedirectStandardError = true;
			}
			
			var cancelToken = new CancellationTokenSource();
			
			var tasks = new List<Task>();
			
			if(InName != null)
			{
				string inPipeName = GenPipeName();
				string inPipePath = PipePathFromName(inPipeName);
				start.EnvironmentVariables[InName] = inPipePath;
				var inPipe = ConnectInPipe(InName, inPipeName, cancelToken.Token);
			
				tasks.Add(inPipe);
				Log(InName+" = "+inPipeName);
			}
			
			if(OutName != null)
			{
				string outPipeName = GenPipeName();
				string outPipePath = PipePathFromName(outPipeName);
				start.EnvironmentVariables[OutName] = outPipePath;
				var outPipe = ConnectOutPipe(OutName, outPipeName, cancelToken.Token);
				
				tasks.Add(outPipe);
				Log(OutName+" = "+outPipeName);
			}
			
			foreach(var pipe in InnerPipes)
			{
				string inVar = pipe+"_IN";
				string outVar = pipe+"_OUT";
				
				string name = GenPipeName();
				
				string inName = name+"\\in";
				string outName = name+"\\out";
				
				string inPath = PipePathFromName(inName);
				string outPath = PipePathFromName(outName);
				
				start.EnvironmentVariables[inVar] = inPath;
				start.EnvironmentVariables[outVar] = outPath;
				
				tasks.Add(ConnectPipe(pipe, inName, outName, cancelToken.Token));
				
				Log(inVar+" = "+inName);
				Log(outVar+" = "+outName);
			}
			
			var proc = new Process{StartInfo = start};
			
			proc.EnableRaisingEvents = true;
			
			proc.Exited += delegate{ cancelToken.Cancel(); };
			
			proc.Start();
			if(StandardOutputRedirect != StandardPipe.None)
			{
				tasks.Add(RedirectStandardOutput(proc));
			}
			if(StandardErrorRedirect != StandardPipe.None)
			{
				tasks.Add(RedirectStandardError(proc));
			}
			
			return Task.WhenAll(tasks);
		}
		
		private static string GenPipeName()
		{
			return Guid.NewGuid().ToString("B").ToUpperInvariant();
		}
		
		private static string PipePathFromName(string name)
		{
			return @"\\.\pipe\"+name;
		}
		
		private Stream OpenRedirect(StandardPipe pipe)
		{
			switch(pipe)
			{
				case StandardPipe.None:
				case StandardPipe.Null:
					return null;
				case StandardPipe.Output:
					return Console.OpenStandardOutput(BufferSize);
				case StandardPipe.Error:
					return Console.OpenStandardError(BufferSize);
				default:
					throw new NotImplementedException();
			}
		}
		
		private async Task RedirectStandardOutput(Process process)
		{
			using(var streamOut = OpenRedirect(StandardOutputRedirect))
			{
				if(streamOut == null) return;
				
				var streamIn = process.StandardOutput.BaseStream;
				var cancel = new CancellationTokenSource();
				process.EnableRaisingEvents = true;
				process.Exited += delegate{cancel.Cancel();};
				await RedirectStandardStream(streamIn, streamOut, cancel.Token);
			}
		}
		
		private async Task RedirectStandardError(Process process)
		{
			using(var streamOut = OpenRedirect(StandardErrorRedirect))
			{
				if(streamOut == null) return;
				
				var streamIn = process.StandardError.BaseStream;
				var cancel = new CancellationTokenSource();
				process.EnableRaisingEvents = true;
				process.Exited += delegate{cancel.Cancel();};
				await RedirectStandardStream(streamIn, streamOut, cancel.Token);
			}
		}
		
		private Task ConnectPipe(string dispname, string inname, string outname, CancellationToken cancellationToken)
		{
			return Task.Run(()=>PipeTask(dispname, inname, outname, cancellationToken));
		}
		
		private void PipeTask(string dispname, string inname, string outname, CancellationToken cancellationToken)
		{
			using(var output = new NamedPipeServerStream(outname, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous))
			using(var input = new NamedPipeServerStream(inname, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous))
			{
				try{
					Log("Waiting for "+dispname+"...");
					Task.WaitAll(new[]{output.WaitForConnectionAsync(cancellationToken), input.WaitForConnectionAsync(cancellationToken)}, cancellationToken);
					Log("Pipe "+dispname+" connected.");
				}catch(AggregateException ag)
				{
					foreach(var e in ag.InnerExceptions)
					{
						Log("Pipe "+dispname+" was closed. "+e.Message);
					}
					return;
				}catch(Exception e)
				{
					Log("Pipe "+dispname+" was closed. "+e.Message);
					return;
				}
				
				byte[] buffer = new byte[BufferSize];
				int len;
				try{
					while((len = input.Read(buffer, 0, buffer.Length)) != 0)
					{
						output.Write(buffer, 0, len);
						output.WaitForPipeDrain();
					}
				}catch(IOException e)
				{
					Log("Pipe "+dispname+" was terminated. "+e.Message);
				}
			}
		}
		
		private Task ConnectInPipe(string dispname, string name, CancellationToken cancellationToken)
		{
			return Task.Run(()=>InPipeTask(dispname, name, cancellationToken));
		}
		
		private void InPipeTask(string dispname, string name, CancellationToken cancellationToken)
		{
			using(var stream = new NamedPipeServerStream(name, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous))
			{
				try{
					stream.WaitForConnectionAsync(cancellationToken).Wait();
				}catch(AggregateException ag)
				{
					foreach(var e in ag.InnerExceptions)
					{
						Log("Pipe "+dispname+" was closed. "+e.Message);
					}
					return;
				}catch(Exception e)
				{
					Log("Pipe "+dispname+" was closed. "+e.Message);
					return;
				}
				using(var stdin = Console.OpenStandardInput(BufferSize))
				{
					byte[] buffer = new byte[BufferSize];
					int len;
					try{
						while((len = stdin.Read(buffer, 0, buffer.Length)) != 0)
						{
							stream.Write(buffer, 0, len);
							stream.WaitForPipeDrain();
						}
					}catch(IOException e)
					{
						Log("Pipe "+dispname+" was terminated. "+e.Message);
					}
				}
			}
		}
		
		private Task ConnectOutPipe(string dispname, string name, CancellationToken cancellationToken)
		{
			return Task.Run(()=>OutPipeTask(dispname, name, cancellationToken));
		}
		
		private void OutPipeTask(string dispname, string name, CancellationToken cancellationToken)
		{
			using(var stream = new NamedPipeServerStream(name, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous))
			{
				try{
					stream.WaitForConnectionAsync(cancellationToken).Wait();
				}catch(AggregateException ag)
				{
					foreach(var e in ag.InnerExceptions)
					{
						Log("Pipe "+dispname+" was closed. "+e.Message);
					}
					return;
				}catch(Exception e)
				{
					Log("Pipe "+dispname+" was closed. "+e.Message);
					return;
				}
				using(var stdout = Console.OpenStandardOutput(BufferSize))
				{
					byte[] buffer = new byte[BufferSize];
					int len;
					try{
						while((len = stream.Read(buffer, 0, buffer.Length)) != 0)
						{
							stdout.Write(buffer, 0, len);
						}
					}catch(IOException e)
					{
						Log("Pipe "+dispname+" was terminated. "+e.Message);
					}
				}
			}
		}
		
		private async Task RedirectStandardStream(Stream streamIn, Stream streamOut, CancellationToken cancellationToken)
		{
			byte[] buffer = new byte[BufferSize];
			try{
				while(!cancellationToken.IsCancellationRequested)
				{
					int len = await streamIn.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
					//Console.WriteLine("Read "+len+" from "+streamIn);
					if(len != 0)
					{
						streamOut.Write(buffer, 0, len);
					}else{
						break;
					}
				}
			}catch(TaskCanceledException)
			{
				
			}catch(IOException e)
			{
				Log(e.Message);
			}
		}
	}
	
	public enum StandardPipe
	{
		None,
		Null,
		Output,
		Error
	}
}
