/* Date: 6.11.2017, Time: 13:01 */
using System;
using System.IO;

namespace imgio
{
	public class JpegExtractor : Extractor
	{
		public override string Extension{
			get{
				return "jpg";
			}
		}
		
		public override string Description{
			get{
				return "JPEG";
			}
		}
		
		public JpegExtractor(Stream inputStream) : base(inputStream)
		{
			
		}
		
		public override void Extract(Stream outputStream)
		{
			var writer = new BinaryWriter(outputStream);
			writer.Write((ushort)0xD8FFu);
			
			byte b;
			while((b = ReadToMarker(writer)) != 0xD9)
			{
				switch(b)
				{
					case 0xD8:
						throw new ApplicationException("Unexpected end of image.");
					case 0x01:
					case 0xD0:
					case 0xD1:
					case 0xD2:
					case 0xD3:
					case 0xD4:
					case 0xD5:
					case 0xD6:
					case 0xD7:
						continue;
					default:
						ushort length = Reader.ReadUInt16BE();
						writer.WriteBE(length);
						Reader.CopyTo(writer, length-2, BufferSize);
						break;
				}
			}
		}
		
		private byte ReadToMarker(BinaryWriter writer)
		{
			byte b = 0;
			while(b == 0)
			{
				while((b = Reader.ReadByte()) != 0xFF) writer.Write(b);
				writer.Write(b);
				while((b = Reader.ReadByte()) == 0xFF) writer.Write(b);
				writer.Write(b);
			}
			return b;
		}
	}
}
