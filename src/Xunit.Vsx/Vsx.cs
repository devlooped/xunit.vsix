﻿using System;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	/// <summary>
	/// Helpers to retrieve hierarchically inherited attribute values for tests.
	/// </summary>
	static class Vsx
	{
		/// <summary>
		/// Default timeout is 60 seconds.
		/// </summary>
		public const int DefaultTimeout = 60;

		public static T GetComputedProperty<T> (this ITestMethod testMethod, string argumentName)
		{
			return GetComputedProperty<T> (testMethod, testMethod.Method.GetCustomAttributes (typeof (IVsxAttribute)).FirstOrDefault (), argumentName);
		}

		public static T GetComputedProperty<T> (this ITestMethod testMethod, IAttributeInfo factAttribute, string argumentName)
		{
			var value = factAttribute == null ? default(T) : factAttribute.GetNamedArgument<T>(argumentName);
			if (!Object.Equals (value, default (T)))
				return value;

			// Go up to the class level.
			var vsxAttr = testMethod.TestClass.Class.GetCustomAttributes (typeof(IVsxAttribute)).FirstOrDefault ();
			if (vsxAttr != null) {
				value = vsxAttr.GetNamedArgument<T> (argumentName);

				if (!Object.Equals (value, default (T)))
					return value;
			}

			// Finally assembly level.
			vsxAttr = testMethod.TestClass.Class.Assembly.GetCustomAttributes (typeof(IVsxAttribute)).FirstOrDefault ();
			if (vsxAttr != null) {
				value = vsxAttr.GetNamedArgument<T> (argumentName);

				if (!Object.Equals (value, default (T)))
					return value;
			}

			return default (T);
		}

		public static T GetComputedArgument<T> (this ITestMethod testMethod, string argumentName)
		{
			return GetComputedArgument<T> (testMethod, testMethod.Method.GetCustomAttributes (typeof (IVsxAttribute)).FirstOrDefault (), argumentName);
		}

		public static T GetComputedArgument<T> (this ITestMethod testMethod, IAttributeInfo factAttribute, string argumentName)
		{
			var value = factAttribute == null ? default(T) : GetNamedArgument<T>(factAttribute, argumentName);
			if (!Object.Equals (value, default (T)))
				return value;

			// Go up to the class level.
			var vsxAttr = testMethod.TestClass.Class.GetCustomAttributes (typeof(IVsxAttribute)).FirstOrDefault ();
			if (vsxAttr != null) {
				value = GetNamedArgument<T> (vsxAttr, argumentName);

				if (!Object.Equals (value, default (T)))
					return value;
			}

			// Finally assembly level.
			vsxAttr = testMethod.TestClass.Class.Assembly.GetCustomAttributes (typeof(IVsxAttribute)).FirstOrDefault ();
			if (vsxAttr != null) {
				value = GetNamedArgument<T> (vsxAttr, argumentName);

				if (!Object.Equals (value, default (T)))
					return value;
			}

			return default (T);
		}

		private static T GetNamedArgument<T>(IAttributeInfo attribute, string argumentOrProperty)
		{
			var reflected = attribute as ReflectionAttributeInfo;
			if (reflected == null)
				throw new NotSupportedException ("Non reflection-based attribute information is not supported.");

			var argument = reflected.AttributeData.NamedArguments.FirstOrDefault (x => x.MemberName == argumentOrProperty);
			if (argument == null)
				return default (T);

			return (T)argument.TypedValue.Value;
		}
	}
}
