#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.Decompiler.Disassembler
{
	public struct MethodSignature
	{
		public string Name;
		public string ReturnType;
		public IReadOnlyList<string> ArgumentTypes;
	}

	public static class MethodSignatureProvider
	{
		public static TypeDefinitionHandle FindType(this PEFile file, string typeName)
		{
			var type = file.Metadata.TypeDefinitions
				.SingleOrDefault(handle => {
					var fullName = handle.GetFullTypeName(file.Metadata);
					var ilName = fullName.ToILNameString();
					var friendlyName = ilName.Replace("/", ".");
					return friendlyName == typeName;
				});
			if (type == default)
			{
				throw new($"Could not find `{typeName}` in `{file.FileName}`");
			}

			return type;
		}

		public static PropertyDefinitionHandle FindProperty(this PEFile file, string typeName, string propertyName)
		{
			var type = file.FindType(typeName);
			var metadata = file.Metadata;
			var typeDefinition = metadata.GetTypeDefinition(type);

			foreach (var handle in typeDefinition.GetProperties())
			{
				var definition = metadata.GetPropertyDefinition(handle);
				var s = metadata.GetString(definition.Name);
				if (s == propertyName)
				{
					return handle;
				}
			}

			throw new($"Could not find `{typeName}.{propertyName}` in `{file.FileName}`");
		}

		public static MethodDefinitionHandle FindMethod(this PEFile file, string typeName, string methodName, Func<MethodSignature, bool>? predicate = null)
		{
			var type = file.FindType(typeName);

			return FindMethod(file, type, methodName, predicate);
		}

		private static MethodDefinitionHandle FindMethod(this PEFile file, TypeDefinitionHandle type, string methodName, Func<MethodSignature, bool>? predicate = null)
		{
			var metadata = file.Metadata;
			var typeDefinition = metadata.GetTypeDefinition(type);

			var method = typeDefinition.GetMethods().SingleOrDefault(handle => {
				var definition = metadata.GetMethodDefinition(handle);
				var name = metadata.GetString(definition.Name);
				var genericParameterCount = definition.GetGenericParameters().Count;
				if (genericParameterCount > 0)
				{
					name += $"`{genericParameterCount}";
				}

				return name == methodName && predicate?.Invoke(GetMethodSignature(handle, file)) != false;
			});

			if (method == default)
			{
				throw new($"Could not find `{typeDefinition.Name}.{methodName}` in `{file.FileName}`");
			}

			return method;
		}

		public static MethodSignature GetMethodSignature(this MethodDefinitionHandle handle, PEFile module)
		{
			var definition = module.Metadata.GetMethodDefinition(handle);
			var name = module.Metadata.GetString(definition.Name);

			var signatureProvider = new CSharpSignatureTypeProvider(module);
			var signature = definition.DecodeSignature(signatureProvider, new MetadataGenericContext(handle, module));

			int genericParameterCount = signature.GenericParameterCount;
			if (genericParameterCount > 0)
			{
				name += $"`{genericParameterCount}";
			}

			var returnType = new PlainTextOutput();
			signature.ReturnType(returnType);

			var parameters = new List<string>();
			foreach (var parameterType in signature.ParameterTypes)
			{
				var output = new PlainTextOutput();
				parameterType(output);
				parameters.Add(output.ToString());
			}

			return new MethodSignature {
				Name = name,
				ReturnType = returnType.ToString(),
				ArgumentTypes = parameters.ToImmutableArray()
			};
		}
	}

	internal class MethodSignatureTypeProvider : ISignatureTypeProvider<Action<string>, MetadataGenericContext>
	{
		private readonly PEFile module;
		private readonly List<string> parameters;

		public MethodSignatureTypeProvider(PEFile module, List<string> parameters)
		{
			this.module = module;
			this.parameters = parameters;
		}

		public Action<string> GetPrimitiveType(PrimitiveTypeCode typeCode)
		{
			switch (typeCode)
			{
				case PrimitiveTypeCode.SByte:
					return syntax => parameters.Add("int8");
				case PrimitiveTypeCode.Int16:
					return syntax => parameters.Add("int16");
				case PrimitiveTypeCode.Int32:
					return syntax => parameters.Add("int32");
				case PrimitiveTypeCode.Int64:
					return syntax => parameters.Add("int64");
				case PrimitiveTypeCode.Byte:
					return syntax => parameters.Add("uint8");
				case PrimitiveTypeCode.UInt16:
					return syntax => parameters.Add("uint16");
				case PrimitiveTypeCode.UInt32:
					return syntax => parameters.Add("uint32");
				case PrimitiveTypeCode.UInt64:
					return syntax => parameters.Add("uint64");
				case PrimitiveTypeCode.Single:
					return syntax => parameters.Add("float32");
				case PrimitiveTypeCode.Double:
					return syntax => parameters.Add("float64");
				case PrimitiveTypeCode.Void:
					return syntax => parameters.Add("void");
				case PrimitiveTypeCode.Boolean:
					return syntax => parameters.Add("bool");
				case PrimitiveTypeCode.String:
					return syntax => parameters.Add("string");
				case PrimitiveTypeCode.Char:
					return syntax => parameters.Add("char");
				case PrimitiveTypeCode.Object:
					return syntax => parameters.Add("object");
				case PrimitiveTypeCode.IntPtr:
					return syntax => parameters.Add("native int");
				case PrimitiveTypeCode.UIntPtr:
					return syntax => parameters.Add("native uint");
				case PrimitiveTypeCode.TypedReference:
					return syntax => parameters.Add("typedref");
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public Action<string> GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
		{
			throw new NotImplementedException();
		}

		public Action<string> GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
		{
			throw new NotImplementedException();
		}

		public Action<string> GetSZArrayType(Action<string> elementType)
		{
			return syntax => {
				elementType("");
				parameters.Add("[]");
			};
		}

		public Action<string> GetGenericInstantiation(Action<string> genericType, ImmutableArray<Action<string>> typeArguments)
		{
			throw new NotImplementedException();
		}

		public Action<string> GetArrayType(Action<string> elementType, ArrayShape shape)
		{
			throw new NotImplementedException();
			//output.Write('[');
			//for (int i = 0; i < shape.Rank; i++)
			//{
			//	if (i > 0)
			//		output.Write(", ");
			//	if (i < shape.LowerBounds.Length || i < shape.Sizes.Length)
			//	{
			//		int lower = 0;
			//		if (i < shape.LowerBounds.Length)
			//		{
			//			lower = shape.LowerBounds[i];
			//			output.Write(lower.ToString());
			//		}
			//		output.Write("...");
			//		if (i < shape.Sizes.Length)
			//			output.Write((lower + shape.Sizes[i] - 1).ToString());
			//	}
			//}
		}

		public Action<string> GetByReferenceType(Action<string> elementType)
		{
			throw new NotImplementedException();
		}

		public Action<string> GetPointerType(Action<string> elementType)
		{
			throw new NotImplementedException();
		}

		public Action<string> GetFunctionPointerType(MethodSignature<Action<string>> signature)
		{
			throw new NotImplementedException();
		}

		public Action<string> GetGenericMethodParameter(MetadataGenericContext genericContext, int index)
		{
			throw new NotImplementedException();
		}

		public Action<string> GetGenericTypeParameter(MetadataGenericContext genericContext, int index)
		{
			throw new NotImplementedException();
		}

		public Action<string> GetModifiedType(Action<string> modifier, Action<string> unmodifiedType, bool isRequired)
		{
			throw new NotImplementedException();
		}

		public Action<string> GetPinnedType(Action<string> elementType)
		{
			throw new NotImplementedException();
		}

		public Action<string> GetTypeFromSpecification(MetadataReader reader, MetadataGenericContext genericContext,
			TypeSpecificationHandle handle, byte rawTypeKind)
		{
			throw new NotImplementedException();
		}
	}
}
