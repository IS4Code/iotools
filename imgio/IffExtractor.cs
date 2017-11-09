/* Date: 9.11.2017, Time: 3:27 */
using System;
using System.IO;
using System.Text;

namespace imgio
{
	public class IffExtractor : Extractor
	{
		public override string Extension{
			get{
				return "form";
			}
		}
		
		public override string Description{
			get{
				return "IFF";
			}
		}
		
		public IffExtractor(Stream inputStream) : base(inputStream)
		{
			
		}
		
		public override void Extract(Stream outputStream)
		{
			var writer = new BinaryWriter(outputStream, Encoding.ASCII);
			writer.Write("FORM".ToCharArray());
			
			uint length = Reader.ReadUInt32BE();
			writer.WriteBE(length);
			
			Reader.CopyTo(writer, length, BufferSize);
		}
	}
}
