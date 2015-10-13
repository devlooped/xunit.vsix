using System;
using System.Reflection;
using Xunit.Sdk;

namespace Xunit
{
	/// <summary>
	/// Exposes the built-in internal serialization helper in Xunit execution desktop assembly.
	/// </summary>
	static class SerializationHelper
	{
		static readonly MethodInfo isSerializableMethod;
		static readonly MethodInfo getTypeMethod;

		static SerializationHelper ()
		{
			var helperType = typeof(MessageBus).Assembly.GetType("Xunit.Sdk.SerializationHelper", true);
			isSerializableMethod = helperType.GetMethod ("IsSerializable", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof (object) }, null);
			getTypeMethod = helperType.GetMethod ("GetType", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof (string), typeof (string) }, null);
		}


		public static bool IsSerializable (object value)
		{
			return (bool)isSerializableMethod.Invoke (null, new object[] { value });
		}

		public static Type GetType (string assemblyName, string typeName)
		{
			return (Type)getTypeMethod.Invoke (null, new object[] { assemblyName, typeName });
		}
	}
}
