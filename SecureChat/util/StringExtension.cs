using System;

namespace SecureChat.util;

public static class StringExtension {
	public static string Crop(this string str, int length) {
		if (str == null)
			throw new NullReferenceException();
		
		return str.Length > length ? $"{str[..(length - 3)]}..." : str;
	}
}