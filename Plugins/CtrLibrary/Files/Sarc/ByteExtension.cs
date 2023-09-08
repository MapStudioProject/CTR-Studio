using System;
using System.Collections.Generic;
using System.Text;

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

		public static uint GetAlignment(this byte[] arr, uint startIndex, Type dataType)
        {
            return ((uint)arr[startIndex] << 8 | (uint)arr[startIndex - 1]);

            return ((uint)arr[arr.Length - 8] << 8 | (uint)arr[arr.Length - 7]);
		}
	}
}
