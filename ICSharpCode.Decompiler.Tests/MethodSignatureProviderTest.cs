using System;
using System.Linq;
using System.Reflection;

using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;

using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests
{
	[TestFixture, Parallelizable(ParallelScope.All)]
	public class MethodSignatureProviderTest
	{
		[TestCase(nameof(Method1), "uint", new[] { "string", "int", "double", "string[]", "int[,]", "ref ICSharpCode.Decompiler.Disassembler.MethodSignature", "ICSharpCode.Decompiler.Tests.MethodSignatureProviderTest", "System.Func`2<int, double>" })]
		[TestCase("Method1`1", "object", new[] { "string", "int"})]
		public void GetCorrectMethodSignature(string methodName, string returnType, string[] parameterTypes)
		{
			using var module = new PEFile(typeof(MethodSignatureProviderTest).Assembly.Location);

			var method = module.FindMethod(typeof(MethodSignatureProviderTest).FullName, methodName);

			var signature = method.GetMethodSignature(module);

			Assert.AreEqual(methodName, signature.Name);
			Assert.AreEqual(returnType, signature.ReturnType);
			Assert.AreEqual(string.Join(", ", parameterTypes), string.Join(", ", signature.ArgumentTypes));
		}

		[Test]
		public void FindOverloadedMethodByCondition()
		{
			using var module = new PEFile(typeof(MethodSignatureProviderTest).Assembly.Location);

			Assert.Throws<InvalidOperationException>(() => module.FindMethod(typeof(MethodSignatureProviderTest).FullName, "Method2`1"));
			Assert.Throws<InvalidOperationException>(() => module.FindMethod(typeof(MethodSignatureProviderTest).FullName, "Method2`1", signature => signature.ArgumentTypes.Count == 2));

			var method = module.FindMethod(typeof(MethodSignatureProviderTest).FullName, "Method2`1", signature => signature.ArgumentTypes.Count == 2 && signature.ArgumentTypes[1] == "double");

			Assert.NotNull(method);
		}

#pragma warning disable CA1822 // Mark members as static

		private UInt32 Method1(string s, int i, double d, string[] arr, int[,] arr2, ref MethodSignature refStruct, MethodSignatureProviderTest type, Func<int, double> func)
		{
			return default;
		}

		private object Method1<T>(string s, int i)
		{
			return default;
		}
		private object Method2<T>(string s, int i)
		{
			return default;
		}

		private void Method2<T>(string s, double i)
		{
		}

		private void Method2<T>(string s, double i, byte b)
		{
		}
	}
}
