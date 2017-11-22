using System;
using System.Runtime.InteropServices;

internal sealed class ErrorHandler
{
    private ErrorHandler()
    {
    }

    public static bool Failed(int hr)
    {
        return (hr < 0);
    }

    public static bool Succeeded(int hr)
    {
        return (hr >= 0);
    }

    public static int ThrowOnFailure(int hr)
    {
        return ThrowOnFailure(hr, null);
    }

    public static int ThrowOnFailure(int hr, params int[] expectedHRFailure)
    {
        if (Failed(hr) && ((expectedHRFailure == null) || (Array.IndexOf<int>(expectedHRFailure, hr) < 0)))
        {
            Marshal.ThrowExceptionForHR(hr);
        }
        return hr;
    }
}