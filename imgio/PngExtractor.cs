/* Date: 6.11.2017, Time: 13:02 */
using System;
using System.IO;

namespace imgio
{
	public class PngExtractor : Extractor
	{
		public override string Extension{
			get{
				return "png";
			}
		}
		
		public override string Description{
			get{
				return "PNG";
			}
		}
		
		public PngExtractor(Stream inputStream) : base(inputStream)
		{
			
		}
		
		public override void Extract(Stream outputStream)
		{
			var writer = new BinaryWriter(outputStream);
			writer.Write(0x0A1A0A0D474E5089);
			
			uint type;
			while((type = ExtractChunk(writer)) != 0x49454E44);
		}
		
		private uint ExtractChunk(BinaryWriter writer)
		{
			uint length = Reader.ReadUInt32BE();
			uint type = Reader.ReadUInt32BE();
			
			Log("PNG: found chunk {0:x} with length {1}", type, length);
			
			writer.WriteBE(length);
			writer.WriteBE(type);
			
			Reader.CopyTo(writer, length, BufferSize);
			
			writer.Write(Reader.ReadInt32());
			
			return type;
		}
	}
}
