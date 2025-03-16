using System;
using System.Text;

namespace SecureChat.ClassExtensions;

public static class StringExtension {
	public static string Crop(this string str, int length) {
		if (str == null)
			throw new NullReferenceException();
		
		return str.Length > length ? $"{str[..(length - 3)]}..." : str;
	}

	public static string Wrap(this string str, int length) {
		if (str == null)
			throw new NullReferenceException();

		StringBuilder sb = new ();
		for (int i = 0; i < str.Length; i++) {
			if (i % length == 0 && i != 0)
				sb.Append('\n');

			sb.Append(str[i]);
		}

		return sb.ToString();
	}
}