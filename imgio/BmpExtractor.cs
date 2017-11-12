/* Date: 8.11.2017, Time: 22:53 */
using System;
using System.IO;
using System.Text;

namespace imgio
{
	public class BmpExtractor : Extractor
	{
		public override string Extension{
			get{
				return "bmp";
			}
		}
		
		public override string Description{
			get{
				return "BMP";
			}
		}
		
		public string Signature{get; private set;}
		
		public BmpExtractor(Stream inputStream, string sig) : base(inputStream)
		{
			Signature = sig;
		}
		
		public override void Extract(Stream outputStream)
		{
			var writer = new BinaryWriter(outputStream, Encoding.ASCII);
			writer.Write(Signature.ToCharArray());
			
			long length = Reader.ReadUInt32();
			writer.Write((uint)length);
			
			Log("BMP: found {0} file with length {1}", Signature, length);
			
			Reader.CopyTo(writer, length-Signature.Length-4, BufferSize);
		}
	}
}
