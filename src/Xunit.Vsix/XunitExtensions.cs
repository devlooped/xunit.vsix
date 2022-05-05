using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using NuGet.Versioning;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
    /// <summary>
    /// Helpers to retrieve hierarchically inherited attribute values for tests.
    /// </summary>
    static class XunitExtensions
    {
        /// <summary>
        /// Default timeout is 60 seconds.
        /// </summary>
        public const int DefaultTimeout = 60;

        public static IVsixAttribute GetVsixAttribute(this ITestMethod testMethod, IAttributeInfo vsixAttribute)
        {
            var vsVersions = testMethod.GetComputedProperty<string[]>(vsixAttribute, nameof(IVsixAttribute.VisualStudioVersions));

            // Higher versions always win for MinVersion, since this way we can toggle a minimum version 
            // at the assembly level and override the per-test min version that might be obsolete. It's like 
            // limiting the lower bound of what tests can override
            var minVersion = testMethod.GetComputedMaxProperty<string>(vsixAttribute, nameof(IVsixAttribute.MinimumVisualStudioVersion));
            NuGetVersion minSemVer = default;
            if (!string.IsNullOrEmpty(minVersion) && !NuGetVersion.TryParse(minVersion, out minSemVer))
                throw new NotSupportedException($"{nameof(IVsixAttribute.MinimumVisualStudioVersion)} '{minVersion}' is not a valid semantic version.");

            // Conversely, for max verison we want the opposite, for the upper bound of supportability, we want 
            // to be able to limit it to lower than a certain version (i.e. tests specify Latest, but we want to 
            // say at the assembly-level that we end support at version X).
            var maxVersion = testMethod.GetComputedMinProperty<string>(vsixAttribute, nameof(IVsixAttribute.MaximumVisualStudioVersion));
            NuGetVersion maxSemVer = default;
            if (!string.IsNullOrEmpty(maxVersion) && !NuGetVersion.TryParse(maxVersion, out maxSemVer))
                throw new NotSupportedException($"{nameof(IVsixAttribute.MaximumVisualStudioVersion)} '{maxVersion}' is not a valid semantic version.");

            var finalVersions = VsVersions.Default.GetFinalVersions(vsVersions, minSemVer, maxSemVer);

            // Process VS-specific traits.
            var suffix = testMethod.GetComputedArgument<string>(vsixAttribute, nameof(IVsixAttribute.RootSuffix)) ?? "Exp";
            if (suffix == ".")
                suffix = "";

            var newInstance = testMethod.GetComputedArgument<bool?>(vsixAttribute, nameof(IVsixAttribute.NewIdeInstance));
            var timeout = testMethod.GetComputedArgument<int?>(vsixAttribute, nameof(IVsixAttribute.TimeoutSeconds)).GetValueOrDefault(DefaultTimeout);
            var recycle = testMethod.GetComputedArgument<bool?>(vsixAttribute, nameof(IVsixAttribute.RecycleOnFailure));
            var uiThread = testMethod.GetComputedArgument<bool?>(vsixAttribute, nameof(IVsixAttribute.RunOnUIThread));

            return new VsixAttribute(finalVersions)
            {
                MinimumVisualStudioVersion = minVersion,
                MaximumVisualStudioVersion = maxVersion,
                RootSuffix = suffix,
                NewIdeInstance = newInstance.GetValueOrDefault(),
                TimeoutSeconds = timeout,
                RecycleOnFailure = recycle.GetValueOrDefault(),
                RunOnUIThread = uiThread.GetValueOrDefault()
            };
        }

        public static T GetComputedProperty<T>(this ITestMethod testMethod, string argumentName)
            => GetComputedProperty<T>(testMethod, testMethod.Method.GetCustomAttributes(typeof(IVsixAttribute)).FirstOrDefault(), argumentName);

        public static T GetComputedProperty<T>(this ITestMethod testMethod, IAttributeInfo factAttribute, string argumentName)
        {
            var value = factAttribute == null ? default(T) : factAttribute.GetNamedArgument<T>(argumentName);
            if (!Object.Equals(value, default(T)))
                return value;

            // Go up to the class level.
            var vsixAttr = testMethod.TestClass.Class.GetCustomAttributes(typeof(IVsixAttribute)).FirstOrDefault();
            if (vsixAttr != null)
            {
                value = vsixAttr.GetNamedArgument<T>(argumentName);

                if (!Object.Equals(value, default(T)))
                    return value;
            }

            // Assembly level.
            vsixAttr = testMethod.TestClass.Class.Assembly.GetCustomAttributes(typeof(IVsixAttribute)).FirstOrDefault();
            if (vsixAttr != null)
            {
                value = vsixAttr.GetNamedArgument<T>(argumentName);

                if (!Object.Equals(value, default(T)))
                    return value;
            }

            // Finally, assembly metadata level
            var metadata = testMethod.TestClass.Class.Assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute))
                .FirstOrDefault(x => argumentName.Equals(x.GetConstructorArguments()?.FirstOrDefault()));

            if (metadata?.GetConstructorArguments().Last() is string str &&
                TypeDescriptor.GetConverter(typeof(T)) is TypeConverter converter)
            {
                if (converter.CanConvertFrom(typeof(string)) &&
                    converter.ConvertFromString(str) is T typed)
                {
                    value = typed;

                    if (!Object.Equals(value, default(T)))
                        return value;
                }
                else if (typeof(T).GetElementType() is Type elementType &&
                    TypeDescriptor.GetConverter(elementType) is TypeConverter elementConverter &&
                    elementConverter.CanConvertFrom(typeof(string)))
                {
                    var values = str.Split(',');
                    var elements = values.Select(x => elementConverter.ConvertFromString(x)).ToArray();
                    var result = Array.CreateInstance(elementType, elements.Length);
                    Array.Copy(elements, result, elements.Length);

                    return (T)(object)result;
                }
            }

            return default(T);
        }

        public static T GetComputedMinProperty<T>(this ITestMethod testMethod, IAttributeInfo factAttribute, string argumentName)
        {
            var values = GetAggregatedProperties<T>(testMethod, factAttribute, argumentName);
            values.Sort();

            return values.FirstOrDefault();
        }

        public static T GetComputedMaxProperty<T>(this ITestMethod testMethod, IAttributeInfo factAttribute, string argumentName)
        {
            var values = GetAggregatedProperties<T>(testMethod, factAttribute, argumentName);
            values.Sort();

            return values.LastOrDefault();
        }

        static List<T> GetAggregatedProperties<T>(this ITestMethod testMethod, IAttributeInfo factAttribute, string argumentName)
        {
            var values = new List<T>();
            var value = factAttribute == null ? default(T) : factAttribute.GetNamedArgument<T>(argumentName);
            if (!Object.Equals(value, default(T)))
                values.Add(value);

            // Go up to the class level.
            var vsixAttr = testMethod.TestClass.Class.GetCustomAttributes(typeof(IVsixAttribute)).FirstOrDefault();
            if (vsixAttr != null)
            {
                value = vsixAttr.GetNamedArgument<T>(argumentName);

                if (!Object.Equals(value, default(T)))
                    values.Add(value);
            }

            // Assembly level.
            vsixAttr = testMethod.TestClass.Class.Assembly.GetCustomAttributes(typeof(IVsixAttribute)).FirstOrDefault();
            if (vsixAttr != null)
            {
                value = vsixAttr.GetNamedArgument<T>(argumentName);

                if (!Object.Equals(value, default(T)))
                    values.Add(value);
            }

            // Finally, assembly metadata level
            var metadata = testMethod.TestClass.Class.Assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute))
                .FirstOrDefault(x => argumentName.Equals(x.GetConstructorArguments()?.FirstOrDefault()));

            if (metadata?.GetConstructorArguments().Last() is string str &&
                TypeDescriptor.GetConverter(typeof(T)) is TypeConverter converter &&
                converter.CanConvertFrom(typeof(string)) &&
                converter.ConvertFromString(str) is T typed)
            {
                values.Add(typed);
            }

            return values;
        }

        public static T GetComputedArgument<T>(this ITestMethod testMethod, string argumentName)
            => GetComputedArgument<T>(testMethod, testMethod.Method.GetCustomAttributes(typeof(IVsixAttribute)).FirstOrDefault(), argumentName);

        public static T GetComputedArgument<T>(this ITestMethod testMethod, IAttributeInfo factAttribute, string argumentName)
        {
            var value = factAttribute == null ? default(T) : GetInitializedArgument<T>(factAttribute, argumentName);
            if (!Object.Equals(value, default(T)))
                return value;

            // Go up to the class level.
            var testClass = testMethod.TestClass.Class;
            IAttributeInfo vsixAttr;
            while (testClass != null && testClass.Name != typeof(object).FullName)
            {
                vsixAttr = testClass.GetCustomAttributes(typeof(IVsixAttribute)).FirstOrDefault();
                if (vsixAttr != null)
                {
                    value = GetInitializedArgument<T>(vsixAttr, argumentName);

                    if (!Object.Equals(value, default(T)))
                        return value;
                }
                testClass = testClass.BaseType;
            }

            // Assembly level.
            vsixAttr = testMethod.TestClass.Class.Assembly.GetCustomAttributes(typeof(IVsixAttribute)).FirstOrDefault();
            if (vsixAttr != null)
            {
                value = GetInitializedArgument<T>(vsixAttr, argumentName);

                if (!Object.Equals(value, default(T)))
                    return value;
            }

            // Finally, assembly metadata level
            var metadata = testMethod.TestClass.Class.Assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute))
                .FirstOrDefault(x => argumentName.Equals(x.GetConstructorArguments()?.FirstOrDefault()));

            if (metadata?.GetConstructorArguments().Last() is string str &&
                TypeDescriptor.GetConverter(typeof(T)) is TypeConverter converter &&
                converter.CanConvertFrom(typeof(string)) &&
                converter.ConvertFromString(str) is T typed)
            {
                value = typed;

                if (!Object.Equals(value, default(T)))
                    return value;
            }

            return default(T);
        }

        /// <summary>
        /// Gets an explicitly initialized attribute argument, using named argument
        /// syntax for attributes (not constructor arguments). Behaves like you
        /// would expect <see cref="IAttributeInfo.GetNamedArgument{TValue}(string)"/>
        /// to behave.
        /// </summary>
        public static T GetInitializedArgument<T>(this IAttributeInfo attribute, string argumentName)
        {
            if (attribute is not ReflectionAttributeInfo reflected)
                throw new NotSupportedException("Non reflection-based attribute information is not supported.");

            if (!reflected.AttributeData.NamedArguments.Any(x => x.MemberName == argumentName))
                return default(T);

            var argument = reflected.AttributeData.NamedArguments.First(x => x.MemberName == argumentName);

            if (argument.TypedValue.ArgumentType.IsEnum)
                return (T)Enum.ToObject(argument.TypedValue.ArgumentType, argument.TypedValue.Value);

            return (T)argument.TypedValue.Value;
        }
    }
}
