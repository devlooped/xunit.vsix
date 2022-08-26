using System;
using System.Diagnostics;

namespace Xunit;

static class RunContext
{
    public static bool DisableTimeout => Debugger.IsAttached ||
        (bool.TryParse(Environment.GetEnvironmentVariable(Constants.DisableTimeoutEnvironmentVariable), out var noTimeout) && noTimeout);

    public static bool EnableTimeout => !DisableTimeout;

    public static bool DebugFramework => bool.TryParse(Environment.GetEnvironmentVariable(Constants.DebugEnvironmentVariable), out var debug) && debug;

    public static bool DebugTests => bool.TryParse(Environment.GetEnvironmentVariable(Constants.DebugRemoteEnvironmentVariable), out var debug) && debug;
}
