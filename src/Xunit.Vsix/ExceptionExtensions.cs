using System;
using System.Reflection;

namespace Xunit
{
    static class ExceptionExtensions
    {
        public static Exception Unwrap(this Exception ex)
        {
            while (true)
            {
                if (ex is AggregateException aex)
                    ex = aex.GetBaseException();

                if (ex is not TargetInvocationException tiex)
                    return ex;

                ex = tiex.InnerException;
            }
        }
    }
}
