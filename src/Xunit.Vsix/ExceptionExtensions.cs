using System;
using System.Reflection;

namespace Xunit
{
    internal static class ExceptionExtensions
    {
        public static Exception Unwrap(this Exception ex)
        {
            while (true)
            {
                var aex = ex as AggregateException;
                if (aex != null)
                    ex = aex.GetBaseException();

                var tiex = ex as TargetInvocationException;
                if (tiex == null)
                    return ex;

                ex = tiex.InnerException;
            }
        }
    }
}
