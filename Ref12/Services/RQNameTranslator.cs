using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.CSharp.Services.Language.Refactoring;

namespace SLaks.Ref12.Services {
	public static class RQNameTranslator {
		static RQNameTranslator() {
			AssemblyRedirector.Register();
		}

		public static string ToIndexId(string rqName) {
			var parsed = new RQNameParser().Parse(rqName);

			var type = parsed as RQUnconstructedType;
			if (type != null)
				return "T:" + type.ClrName();

			var member = parsed as RQMember;
			if (member != null)
				return ToIndexId(member);

			// IndexIds use the parameter name, which we can't get.  (plus, it doesn't work anyway)
			var memberParam = parsed as RQMemberParameterIndex;
			if (memberParam != null)
				return ToIndexId(memberParam.ContainingMember);

			return null;
		}
		static string ToIndexId(RQMember member) {
			var sb = new StringBuilder();
			if (member is RQMemberVariable) // RQKeyword for fields is Membvar
				sb.Append("F:");
			else
				sb.Append(Methods.RQKeyword(member)[0]).Append(':');

			sb.Append(member.ContainingType.ClrName());
			sb.Append('.');
			sb.Append(member.MemberName.TrimStart('.'));    // Don't add a second dot from ".ctor"

			var method = member as RQMethodOrProperty;
			if (method == null)
				return sb.ToString();

			if (method != null && method.TypeParameterCount > 0)
				sb.Append("``").Append(method.TypeParameterCount);

			if (method.Parameters.Any())
				AppendParameters(sb, method);

			return sb.ToString();
		}

		private static void AppendParameters(StringBuilder sb, RQMethodOrProperty method) {
			sb.Append('(');
			var converter = new ParameterNameConverter(sb, method.ContainingType.TypeInfos.Sum(t => t.TypeVariableCount), method.TypeParameterCount);
			foreach (var param in method.Parameters) {
				if (param != method.Parameters.First())
					sb.Append(',');
				converter.AppendClrName(param.Type);

				if (param is RQOutParameter || param is RQRefParameter)
					sb.Append('@');
			}
			sb.Append(')');
		}

		static string ClrName(this RQUnconstructedType type) {
			return string.Concat(type.NamespaceNames.Select(n => n + ".")) + string.Join(".", type.TypeInfos.Select(ClrName));
		}
		static string ClrName(RQUnconstructedTypeInfo t) {
			if (t.TypeVariableCount == 0)
				return t.TypeName;
			return t.TypeName + "`" + t.TypeVariableCount;
		}

		// Run the outer type initializer before trying to load RQNode.
		static class Methods {
			public static Func<RQNode, string> RQKeyword = (Func<RQNode, string>)Delegate.CreateDelegate(typeof(Func<RQNode, string>), typeof(RQNode).GetProperty("RQKeyword", BindingFlags.Instance | BindingFlags.NonPublic).GetMethod);
		}


		class ParameterNameConverter {
			readonly ReadOnlyCollection<string> clrTypeParamRefs;
			readonly Dictionary<string, string> boundTypeParamNames = new Dictionary<string, string>();
			readonly StringBuilder sb;
			public ParameterNameConverter(StringBuilder sb, int typeTypeParamCount, int memberTypeParamCount) {
				clrTypeParamRefs = new[]
				{
					Enumerable.Range(0, typeTypeParamCount).Select(i => "`" + i),
					Enumerable.Range(0, memberTypeParamCount).Select(i => "``" + i)
				}.SelectMany(a => a).ToList().AsReadOnly();
				this.sb = sb;
			}
			public void AppendClrName(RQType type) {
				bool isKnownType =
					Call<RQTypeVariableType>(Append, type)
				 || Call<RQConstructedType>(Append, type)
				 || Call<RQArrayType>(Append, type)
				 || Call<RQPointerType>(t => { AppendClrName(t.ElementType); sb.Append('*'); }, type)
				 || Call<RQVoidType>(t => sb.Append("Void"), type)
				 ;
				if (!isKnownType)
					throw new ArgumentException("Unknown RQType " + type, "type");
			}

			private void Append(RQArrayType t) {
				AppendClrName(t.ElementType);
				sb.Append('[');
				for (int i = 0; i < t.Rank; i++) {
					if (i > 0)
						sb.Append(',');
					sb.Append("0:");    // RQNames can only represent SZArrays
				}
				sb.Append(']');
			}
			private void Append(RQTypeVariableType t) {
				string clrName;
				if (!boundTypeParamNames.TryGetValue(t.Name, out clrName))
					boundTypeParamNames.Add(t.Name, clrName = clrTypeParamRefs[boundTypeParamNames.Count]);
				sb.Append(clrName);
			}
			private void Append(RQConstructedType t) {
				foreach (var ns in t.DefiningType.NamespaceNames) {
					sb.Append(ns);
					sb.Append('.');
				}

				// Append type parameter values after each parameterized type
				int typeParamIndex = 0;
				bool firstType = true;
				foreach (var type in t.DefiningType.TypeInfos) {
					if (!firstType)
						sb.Append('.');
					sb.Append(type.TypeName);
					firstType = false;

					if (type.TypeVariableCount == 0)
						continue;
					sb.Append('{');
					for (int i = 0; i < type.TypeVariableCount; i++) {
						if (i > 0)
							sb.Append(',');
						AppendClrName(t.TypeArguments[i + typeParamIndex]);
					}
					typeParamIndex += type.TypeVariableCount;
					sb.Append('}');
				}
			}
		}

		static bool Call<T>(Action<T> method, object obj) where T : class {
			var co = obj as T;
			if (co == null)
				return false;
			method(co);
			return true;
		}
	}
}
