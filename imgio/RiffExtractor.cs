/* Date: 9.11.2017, Time: 3:21 */
using System;
using System.IO;
using System.Text;

namespace imgio
{
	public class RiffExtractor : Extractor
	{
		public override string Extension{
			get{
				return "riff";
			}
		}
		
		public override string Description{
			get{
				return "RIFF";
			}
		}
		
		public RiffExtractor(Stream inputStream) : base(inputStream)
		{
			
		}
		
		public override void Extract(Stream outputStream)
		{
			var writer = new BinaryWriter(outputStream, Encoding.ASCII);
			writer.Write("RIFF".ToCharArray());
			
			uint length = Reader.ReadUInt32();
			writer.Write(length);
			
			uint type = Reader.ReadUInt32();
			writer.Write(type);
			
			Log("RIFF: found type {0:x} with length {1}", type, length+8);
			
			Reader.CopyTo(writer, length-4, BufferSize);
		}
	}
}
