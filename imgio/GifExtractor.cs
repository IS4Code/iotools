/* Date: 8.11.2017, Time: 23:33 */
using System;
using System.IO;
using System.Text;

namespace imgio
{
	public class GifExtractor : Extractor
	{
		public override string Extension{
			get{
				return "gif";
			}
		}
		
		public override string Description{
			get{
				return "GIF";
			}
		}
		
		public GifExtractor(Stream inputStream) : base(inputStream)
		{
			
		}
		
		public override void Extract(Stream outputStream)
		{
			var writer = new BinaryWriter(outputStream, Encoding.ASCII);
			writer.Write("GIF".ToCharArray());
			
			byte[] buffer = new byte[BufferSize];
			
			Reader.CopyTo(writer, 7, buffer);
			
			byte flags = Reader.ReadByte();
			writer.Write(flags);
			
			short hdr1 = Reader.ReadInt16();
			writer.Write(hdr1);
			
			if((flags & 0x80) != 0)
			{
				long palsize = 3 * (1L << ((flags & 7) + 1));
				Reader.CopyTo(writer, palsize, buffer);
			}
			
			byte indicator;
			while((indicator = Reader.ReadByte()) != 0x3B)
			{
				writer.Write(indicator);
				if(indicator == 0x2C)
				{
					Reader.CopyTo(writer, 8, buffer);
					
					flags = Reader.ReadByte();
					writer.Write(flags);
					
					if((flags & 0x80) != 0)
					{
						long palsize = 3 * (1L << ((flags & 7) + 1));
						Reader.CopyTo(writer, palsize, buffer);
					}
					
					byte code = Reader.ReadByte();
					writer.Write(code);
					
					byte size;
					while((size = Reader.ReadByte()) != 0)
					{
						writer.Write(size);
						Reader.CopyTo(writer, size, buffer);
					}
					writer.Write(size);
				}else if(indicator == 0x21)
				{
					byte label = Reader.ReadByte();
					writer.Write(label);
					
					if(label != 0xFE)
					{
						byte size = Reader.ReadByte();
						writer.Write(size);
						Reader.CopyTo(writer, size, buffer);
					}
					
					byte terminator;
					while((terminator = Reader.ReadByte()) != 0) writer.Write(terminator);
					writer.Write(terminator);
				}else{
					throw new ApplicationException("Unknown GIF segment found!");
				}
			}
			writer.Write(indicator);
		}
	}
}
