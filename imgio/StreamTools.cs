/* Date: 6.11.2017, Time: 13:02 */
using System;
using System.Diagnostics;
using System.IO;

namespace imgio
{
	public static class StreamTools
	{
		public static void CopyTo(this BinaryReader source, BinaryWriter destination, long length, byte[] buffer)
		{
			CopyTo(source.BaseStream, destination.BaseStream, length, buffer);
		}
		
		public static void CopyTo(this Stream source, Stream destination, long length, byte[] buffer)
		{
			while(length > 0)
			{
				int count;
				if(length > buffer.Length)
				{
					count = source.Read(buffer, 0, buffer.Length);
					if(count == 0) throw new EndOfStreamException();
					destination.Write(buffer, 0, count);
				}else{
					count = source.Read(buffer, 0, (int)length);
					if(count == 0) throw new EndOfStreamException();
					destination.Write(buffer, 0, count);
				}
				length -= (uint)count;
			}
		}
		
		public static void CopyTo(this BinaryReader source, BinaryWriter destination, long length, int bufferSize)
		{
			CopyTo(source.BaseStream, destination.BaseStream, length, bufferSize);
		}
		
		public static void CopyTo(this Stream source, Stream destination, long length, int bufferSize)
		{
			CopyTo(source, destination, length, new byte[bufferSize]);
		}
		
		private static short ConvertBE(short num)
		{
			if(BitConverter.IsLittleEndian)
			{
				unchecked{
					return (short)(
						(ushort)((num & 0xFF00) >> 8) |
						((num & 0x00FF) << 8)
					);
				}
			}else{
				return num;
			}
		}
		
		private static int ConvertBE(int num)
		{
			if(BitConverter.IsLittleEndian)
			{
				unchecked{
					return (
						(int)((num & 0xFF000000) >> 24) |
						((num & 0x00FF0000) >> 08) |
						((num & 0x0000FF00) << 08) |
						((num & 0x000000FF) << 24)
					);
				}
			}else{
				return num;
			}
		}
		
		[DebuggerStepThrough]
		public static short ReadInt16BE(this BinaryReader reader)
		{
			return ConvertBE(reader.ReadInt16());
		}
		
		[DebuggerStepThrough]
		public static ushort ReadUInt16BE(this BinaryReader reader)
		{
			return unchecked((ushort)ConvertBE(reader.ReadInt16()));
		}
		
		[DebuggerStepThrough]
		public static int ReadInt32BE(this BinaryReader reader)
		{
			return ConvertBE(reader.ReadInt32());
		}
		
		[DebuggerStepThrough]
		public static uint ReadUInt32BE(this BinaryReader reader)
		{
			return unchecked((uint)ConvertBE(reader.ReadInt32()));
		}
		
		
		[DebuggerStepThrough]
		public static void WriteBE(this BinaryWriter writer, short value)
		{
			writer.Write(ConvertBE(value));
		}
		
		[DebuggerStepThrough]
		public static void WriteBE(this BinaryWriter writer, ushort value)
		{
			writer.Write(ConvertBE(unchecked((short)value)));
		}
		
		[DebuggerStepThrough]
		public static void WriteBE(this BinaryWriter writer, int value)
		{
			writer.Write(ConvertBE(value));
		}
		
		[DebuggerStepThrough]
		public static void WriteBE(this BinaryWriter writer, uint value)
		{
			writer.Write(ConvertBE(unchecked((int)value)));
		}
	}
}
