using System;
using System.Linq;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.Editor;
using Microsoft.VisualBasic.Semantics;
using Microsoft.VisualBasic.Syntax;
using Microsoft.VisualStudio.Text;

namespace SLaks.Ref12.Services {
	public class VBResolver : ISymbolResolver {
		static VBResolver() {
			AssemblyRedirector.Register();
		}

		public SymbolInfo GetSymbolAt(string sourceFileName, SnapshotPoint point) {
			//Debugger.Launch();
			var file = (SourceFile)point.Snapshot.TextBuffer.MaybeGetSourceFileData().Value.SourceFile;

			if (!file.IsBound)
				return null;
			return Walk(file.RootNode, point.ToPosition());
		}

		private static SymbolInfo Walk(SyntaxNode node, Position location) {

			foreach (var c in node.Children) {
				// VariableGroupWithNewNodes can have
				// out-of-order children, so I cannot
				// exit the loop early.
				if (c.Span.End < location || c.Span.Start > location)
					continue;

				if (c.Children.Any())
					return Walk(c, location);
				else
					return GetSymbolInfo(c);
			}
			return null;
		}

		private static string ToIndexId(Symbol symbol, ISourceFile file) {
			VBParameter vBParameter = symbol as VBParameter;
			if (vBParameter != null) {
				symbol = vBParameter.ParameterType;
			}
			VBMember vBMember = symbol as VBMember;
			if (vBMember != null) {
				if (vBMember.IsFunctionResultLocal) {
					return null;
				}
				while (vBMember.ContainingMember != null) {
					vBMember = vBMember.ContainingMember;
				}
				if (vBMember.IsProperty && vBMember.Name == "Value" && vBMember.ContainingType != null && vBMember.ContainingType.Name == "InternalXmlHelper") {
					return null; // "vb.XmlPropertyExtensionValue";
				}
				if (vBMember.IsProperty && vBMember.SourceFile != null && vBMember.SourceFile.IsSolutionExtension) {
					if (vBMember.Name == "Computer") {
						return "T:Microsoft.VisualBasic.Devices." + vBMember.Name;
					}
					if (vBMember.Name == "User") {
						return "T:Microsoft.VisualBasic.ApplicationServices." + vBMember.Name;
					}
					if (vBMember.Name == "Application") {
						return null; //"My." + vBMember.Name;
					}
					if (vBMember.ReturnType != null && vBMember.ReturnType.BaseClass != null) {
						return "T:Microsoft.VisualBasic.ApplicationServices." + vBMember.ReturnType.BaseClass.Name;
					}
					return null; //"My." + vBMember.Name;
				} else {
					if (vBMember.IsSynthetic || (vBMember.IsField && vBMember.SourceFile != null)) {
						symbol = vBMember.ReturnType;
					} else {
						VBType containingType = vBMember.ContainingType;
						if (containingType != null && (containingType.IsAnonymousType || containingType.IsAnonymousDelegate)) {
							return null; // "vb.AnonymousType";
						}
					}
				}
			}
			VBType vBType = symbol as VBType;
			while (vBType != null) {
				if (vBType.IsArray || vBType.IsPointer) {
					vBType = vBType.ElementType;
				} else {
					if (vBType.DefiningType == null) {
						break;
					}
					vBType = vBType.DefiningType;
					if (vBType == file.Host.RuntimeInfo.NullableType) {
						return null; //"vb.Nullable";
					}
				}
			}
			if (vBType != null) {
				if (vBType.IsPrimitive) {
					return "T:System." + vBType.TypeCode.ToString(); // "vb." + vBType.Name;
				}
				if (vBType.IsAnonymousType || vBType.IsAnonymousDelegate) {
					return null; //"vb.AnonymousType";
				}
				if (vBType == file.Binder.ResolveType("System.Runtime.CompilerServices.ExtensionAttribute", 0)) {
					return null; //"vb.ExtensionMethods";
				}
			}
			if (symbol is VBNamespace)
				return null;

			if (symbol != null) {
				// CodeBuilder adds unavoidable spaces; IndexIds never have spaces.
				return symbol.ToString(new IndexIdSymbolFormatter()).Replace(" ", "");
			}
			return null;
		}

		static TypeNode ImmediateTypeParent(SyntaxNode node) {
			while (node is NameNode)
				node = node.Parent;
			return node as TypeNode;
		}
		private static SymbolInfo GetSymbolInfo(SyntaxNode node) {
			if (node is IdentifierNode) {
				node = node.Parent;
			}
			//if (node.Parent is NamedTypeNode) {
			//	node = node.Parent;
			//}

			QualifiedNameNode qualifiedNameNode = node.Parent as QualifiedNameNode;
			if (qualifiedNameNode != null && qualifiedNameNode.Name == node) {
				node = qualifiedNameNode;
			}
			if (node.Parent is NameExpressionNode) {
				node = node.Parent;
			}
			if (node.Parent is GenericQualifiedNode) {
				node = node.Parent;
			}
			QualifiedNode qualifiedNode = node.Parent as QualifiedNode;
			while (qualifiedNode != null && qualifiedNode.Value == node) {
				node = qualifiedNode;
				qualifiedNode = (node.Parent as QualifiedNode);
			}

			// If the current symbol is the type immediately
			// in a New() invocation (as opposed to a nested
			// generic type), use the constructor.
			var typeNode = ImmediateTypeParent(node);
			if (typeNode != null)
				node = typeNode.Parent as NewNode ?? node;

			if (node.Parent is ICallSiteNode || node.Parent is NewNode) {
				node = node.Parent;
			}
			Symbol symbol = null;
			NameNode name = node as NameNode;
			if (name != null) {
				if (name.Parent is DeclaratorNode && name.Parent.Parent is ParameterNode) {
					LambdaNode lambdaNode = name.Parent.Parent.Parent as LambdaNode;
					if (lambdaNode != null) {
						LambdaExpression lambdaExpression = node.Tree.SourceFile.Binder.CompileExpression(lambdaNode) as LambdaExpression;
						if (lambdaExpression != null) {
							symbol = lambdaExpression.Parameters
								.FirstOrDefault(p => p.Name.EqualsNoCase(((IdentifierNode)name).Name));
						}
					}
				}
				if (symbol == null) {
					symbol = node.Tree.SourceFile.Binder.ResolveName(name);
				}
			} else {
				var expression = node.Tree.SourceFile.Binder.CompileExpression(node);
				var boundCtor = expression as NewExpression;

				// ExtractSymbol() resovles ctor calls to the type, not the ctor. (no clue why)
				if (boundCtor != null)
					symbol = boundCtor.ConstructorCall.ExtractSymbol();
				else if (expression != null)
					symbol = expression.ExtractSymbol();
			}
			if (symbol == null)
				return null;

			string indexId = ToIndexId(symbol, node.Tree.SourceFile);
			if (indexId == null)
				return null;
			return new SymbolInfo(indexId, symbol.Assembly.BinaryFileName);
		}

		sealed class IndexIdSymbolFormatter : SymbolFormatter {
			protected override void VisitAnonymousDelegate(VBType node) {
				Code.Append("vb#AnonymousType");
			}
			protected override void VisitAnonymousType(VBType node) {
				Code.Append("vb#AnonymousType");
			}
			protected override void VisitArray(VBType node) {
				VisitIfNotNull(node.ElementType);
				Code.Append("[");
				for (int i = 0; i < node.Rank; i++) {
					if (i > 0)
						Code.Append(",");
					Code.Append("0:");	// I think VBType can only be an SZArray
				}
				Code.Append("]");
			}
			protected override void VisitNamespace(VBNamespace node) {
				if (!node.Name.Equals("My", StringComparison.OrdinalIgnoreCase)) {
					VisitBaseQualifier(node.ContainingNamespace);
				}
				Code.Append(node.Name);
			}
			protected override void VisitPointer(VBType node) {
				VisitIfNotNull(node.ElementType);
				Code.Append("*");
			}
			#region Stubs
			protected override void VisitAddHandlerAccessor(VBMember node) {
				VisitIfNotNull(node.ContainingMember);
			}
			protected override void VisitRaiseEventAccessor(VBMember node) {
				VisitIfNotNull(node.ContainingMember);
			}
			protected override void VisitRemoveHandlerAccessor(VBMember node) {
				VisitIfNotNull(node.ContainingMember);
			}
			protected override void VisitGetAccessor(VBMember node) {
				VisitIfNotNull(node.ContainingMember);
			}
			protected override void VisitSetAccessor(VBMember node) {
				VisitIfNotNull(node.ContainingMember);
			}
			#endregion

			#region Prefixes
			void Prefix(string prefix) {
				if (Code.ToString().Length == 0)
					Code.Append(prefix);
			}
			protected override void VisitStructure(VBType node) {
				base.VisitStructure(node);
			}
			protected override void VisitEnumMember(VBMember node) {
				Prefix("F:");
				base.VisitEnumMember(node);
			}
			protected override void VisitField(VBMember node) {
				Prefix("F:");
				base.VisitField(node);
			}
			protected override void VisitProperty(VBMember node) {
				Prefix("P:");
				base.VisitProperty(node);
			}
			protected override void VisitConstructor(VBMember node) {
				Prefix("M:");
				base.VisitConstructor(node);
			}
			protected override void VisitMethod(VBMember node) {
				Prefix("M:");
				base.VisitMethod(node);
			}
			protected override void VisitEvent(VBMember node) {
				Prefix("E:");
				base.VisitEvent(node);
			}
			#endregion

			protected override void VisitParameter(VBParameter node) {
				VisitIfNotNull(node.ParameterType);
				if (node.IsByRef)
					Code.Append("@");
			}

			// The outer type in any reference is an unconstructed type; 
			// types in parameters lists are constructed types.
			bool inCoreType = true;
			protected override void VisitMember(VBMember node) {
				VisitBaseQualifier(node.ContainingType);
				string text;
				if (node.IsConstructor) {
					text = "ctor";		// IndexId uses "ctor", not "#ctor"
				} else if (node.IsProperty && node.IsDefault) {
					text = "Item";	// TODO: Test indexer
				} else {
					text = node.Name;
					int count = node.TypeParameters.Count;
					if (count > 0) {
						text = text + "``" + count;
					}
				}
				Code.Append(text);
				if (node.Parameters.Any() && !node.IsEvent)	// Events cannot be overloaded, so they don't need signatures
					VisitParameters(node.Parameters);
			}
			protected override void VisitType(VBType node) {
				Prefix("T:");
				VisitBaseQualifier(node.ContainingNamespace);
				VisitBaseQualifier(node.ContainingType);
				if (node.IsPrimitive) {
					Code.Append(node.TypeCode.ToString());
					return;
				}
				Code.Append(node.Name);

				if (inCoreType) {
					inCoreType = false;
					if (node.TypeParameters.Count > 0)
						Code.Append("`" + node.TypeParameters.Count);
				} else {
					// If we're in a parameter, include its concrete type arguments
					if (node.TypeArguments.Count > 0) {
						Code.Append("{");
						VisitCommaList(node.TypeArguments);
						Code.Append("}");
					}
				}
			}
			protected override void VisitTypeParameter(VBType node) {
				// Method type parameters get two backticks
				if (node.ContainingType == null)
					Code.Append("`");
				Code.Append("`");
				// Type type parameter indices include parameters from the type's outer types.
				Code.Append((PreTypeParamCount(node) + node.TypeParameterPosition).ToString());
			}
			///<summary>Gets the total number of type parameters in this type's outer generic types.</summary>
			static int PreTypeParamCount(VBType type) {
				if (type.DefiningType == null)
					return 0;
				return PreTypeParamCount(type.DefiningType) + type.DefiningType.TypeParameters.Count;
			}
		}
	}
}
