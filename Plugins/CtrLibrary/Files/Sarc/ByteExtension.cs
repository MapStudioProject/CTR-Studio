using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core.IO;

namespace CtrLibrary
{
	public static class ByteExtension
    {
		public static bool Matches(this byte[] arr, string magic) =>
			arr.Matches(0, magic.ToCharArray());
		public static bool Matches(this byte[] arr, uint startIndex, string magic) =>
			arr.Matches(startIndex, magic.ToCharArray());

		public static bool Matches(this byte[] arr, uint startIndex, params char[] magic)
		{
			if (arr.Length < magic.Length + startIndex) return false;
			for (uint i = 0; i < magic.Length; i++)
			{
				if (arr[i + startIndex] != magic[i]) return false;
			}
			return true;
		}

		public static uint GetAlignment(this byte[] arr, uint offset)
        {
            using (var reader = new FileReader(arr)) {
				reader.SetByteOrder(true);
                reader.SeekBegin(offset);
                var alignment = reader.ReadUInt16();
				return alignment != 0 ? alignment : (uint)32;
            }
		}
	}
}
