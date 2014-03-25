using Microsoft.VisualStudio.TestTools.UnitTesting;
using SLaks.Ref12.Services;

namespace Ref12.Tests {
	[TestClass]
	public class RQNameTranslatorTests {
		[TestMethod]
		public void IndexIdTestCases() {
			Assert.AreEqual("E:System.AppDomain.AssemblyLoad", RQNameTranslator.ToIndexId("Event(Agg(NsName(System),AggName(AppDomain,TypeVarCnt(0))),EventName(AssemblyLoad))"));
			Assert.AreEqual("F:System.Environment.SpecialFolderOption.Create", RQNameTranslator.ToIndexId("Membvar(Agg(NsName(System),AggName(Environment,TypeVarCnt(0)),AggName(SpecialFolderOption,TypeVarCnt(0))),MembvarName(Create))"));
			Assert.AreEqual("F:System.Runtime.InteropServices.BIND_OPTS.cbStruct", RQNameTranslator.ToIndexId("Membvar(Agg(NsName(System),NsName(Runtime),NsName(InteropServices),AggName(BIND_OPTS,TypeVarCnt(0))),MembvarName(cbStruct))"));
			Assert.AreEqual("M:System.Collections.Generic.HashSet`1.ctor", RQNameTranslator.ToIndexId("Meth(Agg(NsName(System),NsName(Collections),NsName(Generic),AggName(HashSet,TypeVarCnt(1))),MethName(.ctor),TypeVarCnt(0),Params())"));
			Assert.AreEqual("M:System.Int32.ToString", RQNameTranslator.ToIndexId("Meth(Agg(NsName(System),AggName(Int32,TypeVarCnt(0))),MethName(ToString),TypeVarCnt(0),Params())"));
			Assert.AreEqual("P:System.Collections.Generic.List`1.Enumerator.Current", RQNameTranslator.ToIndexId("Prop(Agg(NsName(System),NsName(Collections),NsName(Generic),AggName(List,TypeVarCnt(1)),AggName(Enumerator,TypeVarCnt(0))),PropName(Current),TypeVarCnt(0),Params())"));
			Assert.AreEqual("T:S.X`1.Y`1", RQNameTranslator.ToIndexId("Agg(AggName(S,TypeVarCnt(0)),AggName(X,TypeVarCnt(1)),AggName(Y,TypeVarCnt(1)))"));
			Assert.AreEqual("T:System.Collections.Generic.List`1", RQNameTranslator.ToIndexId("Agg(NsName(System),NsName(Collections),NsName(Generic),AggName(List,TypeVarCnt(1)))"));
			Assert.AreEqual("T:System.Collections.Generic.List`1.Enumerator", RQNameTranslator.ToIndexId("Agg(NsName(System),NsName(Collections),NsName(Generic),AggName(List,TypeVarCnt(1)),AggName(Enumerator,TypeVarCnt(0)))"));
			Assert.AreEqual("T:System.Environment.SpecialFolderOption", RQNameTranslator.ToIndexId("Agg(NsName(System),AggName(Environment,TypeVarCnt(0)),AggName(SpecialFolderOption,TypeVarCnt(0)))"));
			Assert.AreEqual("T:System.Int32", RQNameTranslator.ToIndexId("Agg(NsName(System),AggName(Int32,TypeVarCnt(0)))"));
			Assert.AreEqual("T:System.ValueType", RQNameTranslator.ToIndexId("Agg(NsName(System),AggName(ValueType,TypeVarCnt(0)))"));
		}
		[TestMethod]
		public void IndexIdMethodParameterTestCases() {
			Assert.AreEqual("M:System.Environment.SetEnvironmentVariable(System.String,System.String,System.EnvironmentVariableTarget)", RQNameTranslator.ToIndexId("Meth(Agg(NsName(System),AggName(Environment,TypeVarCnt(0))),MethName(SetEnvironmentVariable),TypeVarCnt(0),Params(Param(AggType(Agg(NsName(System),AggName(String,TypeVarCnt(0))),TypeParams())),Param(AggType(Agg(NsName(System),AggName(String,TypeVarCnt(0))),TypeParams())),Param(AggType(Agg(NsName(System),AggName(EnvironmentVariableTarget,TypeVarCnt(0))),TypeParams()))))"));
			Assert.AreEqual("M:System.Linq.ParallelEnumerable.Any``1(System.Linq.ParallelQuery{``0})", RQNameTranslator.ToIndexId("Meth(Agg(NsName(System),NsName(Linq),AggName(ParallelEnumerable,TypeVarCnt(0))),MethName(Any),TypeVarCnt(1),Params(Param(AggType(Agg(NsName(System),NsName(Linq),AggName(ParallelQuery,TypeVarCnt(1))),TypeParams(TyVar(TSource))))))"));
			Assert.AreEqual("M:System.Linq.Queryable.Aggregate``1(System.Linq.IQueryable{``0},System.Linq.Expressions.Expression{System.Func{``0,``0,``0}})", RQNameTranslator.ToIndexId("Meth(Agg(NsName(System),NsName(Linq),AggName(Queryable,TypeVarCnt(0))),MethName(Aggregate),TypeVarCnt(1),Params(Param(AggType(Agg(NsName(System),NsName(Linq),AggName(IQueryable,TypeVarCnt(1))),TypeParams(TyVar(TSource)))),Param(AggType(Agg(NsName(System),NsName(Linq),NsName(Expressions),AggName(Expression,TypeVarCnt(1))),TypeParams(AggType(Agg(NsName(System),AggName(Func,TypeVarCnt(3))),TypeParams(TyVar(TSource),TyVar(TSource),TyVar(TSource))))))))"));
			Assert.AreEqual("M:System.Linq.ParallelEnumerable.AsParallel``1(System.Collections.Generic.IEnumerable{``0})", RQNameTranslator.ToIndexId("Meth(Agg(NsName(System),NsName(Linq),AggName(ParallelEnumerable,TypeVarCnt(0))),MethName(AsParallel),TypeVarCnt(1),Params(Param(AggType(Agg(NsName(System),NsName(Collections),NsName(Generic),AggName(IEnumerable,TypeVarCnt(1))),TypeParams(TyVar(TSource))))))"));
			Assert.AreEqual("M:System.Collections.Generic.List`1.ConvertAll``1(System.Converter{`0,``0})", RQNameTranslator.ToIndexId("Meth(Agg(NsName(System),NsName(Collections),NsName(Generic),AggName(List,TypeVarCnt(1))),MethName(ConvertAll),TypeVarCnt(1),Params(Param(AggType(Agg(NsName(System),AggName(Converter,TypeVarCnt(2))),TypeParams(TyVar(T),TyVar(TOutput))))))"));
			Assert.AreEqual("M:S.X`1.Y`1.Z``1(`0,`1,``0,System.Tuple{``0,`1,`0})", RQNameTranslator.ToIndexId("Meth(Agg(AggName(S,TypeVarCnt(0)),AggName(X,TypeVarCnt(1)),AggName(Y,TypeVarCnt(1))),MethName(Z),TypeVarCnt(1),Params(Param(TyVar(T)),Param(TyVar(U)),Param(TyVar(V)),Param(AggType(Agg(NsName(System),AggName(Tuple,TypeVarCnt(3))),TypeParams(TyVar(V),TyVar(U),TyVar(T))))))"));

			// TODO: ref parameters

			// The generated RQName from native language services incorrectly omits the first type argument, making this test unpassable.
			//Assert.AreEqual("M:S.Complex``1(S.X{System.Int32}.Y{``0},S.X{``0}.Y{System.Int32},System.Char**[][0:,0:,0:])", RQNameTranslator.ToIndexId("Meth(Agg(AggName(S,TypeVarCnt(0))),MethName(Complex),TypeVarCnt(1),Params(Param(AggType(Agg(AggName(S,TypeVarCnt(0)),AggName(X,TypeVarCnt(1)),AggName(Y,TypeVarCnt(1))),TypeParams(TyVar(T)))),Param(AggType(Agg(AggName(S,TypeVarCnt(0)),AggName(X,TypeVarCnt(1)),AggName(Y,TypeVarCnt(1))),TypeParams(AggType(Agg(NsName(System),AggName(Int32,TypeVarCnt(0))),TypeParams())))),Param(Array(3,Array(1,Ptr(Ptr(AggType(Agg(NsName(System),AggName(Char,TypeVarCnt(0))),TypeParams()))))))))"));
		}
	}
}