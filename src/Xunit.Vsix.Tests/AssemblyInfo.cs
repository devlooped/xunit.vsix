using System.Diagnostics;
using Xunit;

[assembly: VsixRunner(WarmupMilliseconds = 10000, TraceLevel = SourceLevels.Information)]
[assembly: Vsix(TimeoutSeconds = 60)]