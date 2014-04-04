using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SLaks.Ref12.Services {
	class IndexIdTranslator {

		// This is copy-pasted from Kirill's source to ensure correct hashes
		public static ISymbol GetTargetSymbol(ISymbol symbol) {
			if (IsThisParameter(symbol)) {
				var paramType = ((IParameterSymbol)symbol).Type;
				if (paramType != null)
					return paramType;
			} else if (IsFunctionValue(symbol)) {
				var method = symbol.ContainingSymbol as IMethodSymbol;
				if (method != null) {
					if (method.AssociatedSymbol != null) {
						return method.AssociatedSymbol;
					} else {
						return method;
					}
				}
			}

			if (symbol.Kind == SymbolKind.Method)
				return ((IMethodSymbol)symbol).ReducedFrom ?? symbol;

			symbol = ResolveAccessorParameter(symbol);

			return symbol;
		}

		private static ISymbol ResolveAccessorParameter(ISymbol symbol) {
			if (symbol == null || !symbol.IsImplicitlyDeclared) {
				return symbol;
			}

			var parameterSymbol = symbol as IParameterSymbol;
			if (parameterSymbol == null) {
				return symbol;
			}

			var accessorMethod = parameterSymbol.ContainingSymbol as IMethodSymbol;
			if (accessorMethod == null) {
				return symbol;
			}

			var property = accessorMethod.AssociatedSymbol as IPropertySymbol;
			if (property == null) {
				return symbol;
			}

			int ordinal = parameterSymbol.Ordinal;
			if (property.Parameters.Length <= ordinal) {
				return symbol;
			}

			return property.Parameters[ordinal];
		}

		private static bool IsFunctionValue(ISymbol symbol) {
			return symbol is ILocalSymbol && ((ILocalSymbol)symbol).IsFunctionValue;
		}

		private static bool IsThisParameter(ISymbol symbol) {
			return symbol != null && symbol.Kind == SymbolKind.Parameter && ((IParameterSymbol)symbol).IsThis;
		}

		private static string GetDocumentationCommentId(ISymbol symbol) {
			string result = null;
			if (!symbol.IsDefinition) {
				symbol = symbol.OriginalDefinition;
			}

			result = symbol.GetDocumentationCommentId();

			result = result.Replace("#ctor", "ctor");

			return result;
		}
		public static string GetId(ISymbol symbol) {
			string result = null;

			if (symbol.Kind == SymbolKind.Parameter ||
			symbol.Kind == SymbolKind.Local) {
				string parent = GetDocumentationCommentId(symbol.ContainingSymbol);
				result = parent + ":" + symbol.MetadataName;
			} else {
				result = GetDocumentationCommentId(symbol);
			}

			return GetId(result);
		}
		public static string GetId(string result) {
			result = GetMD5Hash(result, 16);
			return result;
		}
		public static string GetMD5Hash(string input, int digits) {
			using (var md5 = MD5.Create()) {
				var bytes = Encoding.UTF8.GetBytes(input);
				var hashBytes = md5.ComputeHash(bytes);
				return ByteArrayToHexString(hashBytes, digits);
			}
		}
		public static string ByteArrayToHexString(byte[] bytes, int digits = 0) {
			if (digits == 0) {
				digits = bytes.Length * 2;
			}

			char[] c = new char[digits];
			byte b;
			for (int i = 0; i < digits / 2; i++) {
				b = ((byte)(bytes[i] >> 4));
				c[i * 2] = (char)(b > 9 ? b + 87 : b + 0x30);
				b = ((byte)(bytes[i] & 0xF));
				c[i * 2 + 1] = (char)(b > 9 ? b + 87 : b + 0x30);
			}

			return new string(c);
		}
	}
}
