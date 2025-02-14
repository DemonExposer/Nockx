using System;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace SecureChat.ClassExtensions;

public static class RsaKeyParametersExtension {
	public static string ToBase64String(this RsaKeyParameters key) {
		if (key.IsPrivate)
			throw new FormatException("Cannot convert a private key to Base64");
			
		SubjectPublicKeyInfo publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(key);
		return Convert.ToBase64String(publicKeyInfo.GetEncoded());
	}

	public static RsaKeyParameters FromBase64String(string base64Key) {
		SubjectPublicKeyInfo publicKeyInfo = SubjectPublicKeyInfo.GetInstance(Convert.FromBase64String(base64Key));
		return (RsaKeyParameters) PublicKeyFactory.CreateKey(publicKeyInfo);
	}
}