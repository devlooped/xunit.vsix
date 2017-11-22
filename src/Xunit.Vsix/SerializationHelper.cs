using System;
using System.Reflection;
using Xunit.Sdk;

namespace Xunit
{
    /// <summary>
    /// Exposes the built-in internal serialization helper in Xunit execution desktop assembly.
    /// </summary>
    internal static class SerializationHelper
    {
        private static readonly MethodInfo s_isSerializableMethod;
        private static readonly MethodInfo s_getTypeMethod;

        static SerializationHelper()
        {
            var helperType = typeof(MessageBus).Assembly.GetType("Xunit.Sdk.SerializationHelper", true);
            s_isSerializableMethod = helperType.GetMethod("IsSerializable", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(object) }, null);
            s_getTypeMethod = helperType.GetMethod("GetType", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string) }, null);
        }


        public static bool IsSerializable(object value)
        {
            return (bool)s_isSerializableMethod.Invoke(null, new object[] { value });
        }

        public static Type GetType(string assemblyName, string typeName)
        {
            return (Type)s_getTypeMethod.Invoke(null, new object[] { assemblyName, typeName });
        }
    }
}
