/* Date: 6.11.2017, Time: 13:01 */
using System;
using System.IO;
using System.Text;

namespace imgio
{
	public abstract class Extractor
	{
		public Stream InputStream{get; private set;}
		protected BinaryReader Reader{get; private set;}
		
		public abstract string Extension{get;}
		
		public abstract string Description{get;}
		
		public int BufferSize{get; set;}
		
		public Action<string> Logger{get; set;}
		
		protected void Log(string msg)
		{
			var logger = Logger;
			if(logger != null)
			{
				logger(msg);
			}
		}
		
		/*protected void Log(string format, params object[] args)
		{
			var logger = Logger;
			if(logger != null)
			{
				logger(String.Format(format, args));
			}
		}*/
		
		protected void Log<T0>(string format, T0 arg0)
		{
			var logger = Logger;
			if(logger != null)
			{
				logger(String.Format(format, arg0));
			}
		}
		
		protected void Log<T0, T1>(string format, T0 arg0, T1 arg1)
		{
			var logger = Logger;
			if(logger != null)
			{
				logger(String.Format(format, arg0, arg1));
			}
		}
		
		public Extractor(Stream inputStream)
		{
			InputStream = inputStream;
			Reader = new BinaryReader(inputStream, Encoding.ASCII);
		}
		
		public abstract void Extract(Stream outputStream);
		
		public static Extractor Create(Stream inputStream)
		{
			int first = inputStream.ReadByte();
			var reader = new BinaryReader(inputStream);
			switch(first)
			{
				case 0xFF:
					if(reader.ReadByte() == 0xD8)
					{
						return new JpegExtractor(inputStream);
					}
					goto default;
				case 0x89:
					if(reader.ReadByte() == 0x50)
					if(reader.ReadByte() == 0x4E)
					if(reader.ReadByte() == 0x47)
					if(reader.ReadByte() == 0x0D)
					if(reader.ReadByte() == 0x0A)
					if(reader.ReadByte() == 0x1A)
					if(reader.ReadByte() == 0x0A)
					{
						return new PngExtractor(inputStream);
					}
					goto default;
				case 'G':
					if(reader.ReadByte() == 'I')
					if(reader.ReadByte() == 'F')
					{
						return new GifExtractor(inputStream);
					}
					goto default;
				case 'F':
					if(reader.ReadByte() == 'O')
					if(reader.ReadByte() == 'R')
					if(reader.ReadByte() == 'M')
					{
						return new IffExtractor(inputStream);
					}
					goto default;
				case 'R':
					if(reader.ReadByte() == 'I')
					if(reader.ReadByte() == 'F')
					if(reader.ReadByte() == 'F')
					{
						return new RiffExtractor(inputStream);
					}
					goto default;
				case 'B':
					switch((int)reader.ReadByte())
					{
						case 'M':
							return new BmpExtractor(inputStream, "BM");
						case 'A':
							return new BmpExtractor(inputStream, "BA");
					}
					goto default;
				case 'C':
					switch((int)reader.ReadByte())
					{
						case 'I':
							return new BmpExtractor(inputStream, "CI");
						case 'L':
							return new BmpExtractor(inputStream, "CL");
					}
					goto default;
				case 'I':
					if(reader.ReadByte() == 'C')
					{
						return new BmpExtractor(inputStream, "IC");
					}
					goto default;
				case 'P':
					if(reader.ReadByte() == 'T')
					{
						return new BmpExtractor(inputStream, "PT");
					}
					goto default;
				case -1:
					return null;
				default:
					throw new Exception("Unknown image format.");
			}
		}
	}
}
