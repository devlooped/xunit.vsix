using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Xunit.Properties;

namespace Xunit
{
	/// <summary>
	/// Processes and loads available VS versions.
	/// </summary>
	static class VsVersions
	{
		static readonly TraceSource tracer = Constants.Tracer;

		static VsVersions ()
		{
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\SxS\VS7"))
            {
                InstalledVersions = key.GetValueNames()
                    .Where(version => Directory.Exists(Path.Combine((string)key.GetValue(version), "VSSDK")))
                    .ToArray();
            }
            
			//InstalledVersions = (from version in Enumerable.Range (10, 20)
			//					// VSSDK100Install
			//					 let varName = "VSSDK" + version + "0Install"
			//					 where !string.IsNullOrEmpty (Environment.GetEnvironmentVariable (varName))
			//					 select version + ".0")
			//					.ToArray ();


			LatestVersion = InstalledVersions.LastOrDefault ();
			tracer.TraceInformation (Strings.VsVersions.InstalledVersions (string.Join (", ", InstalledVersions)));
			tracer.TraceInformation (Strings.VsVersions.LatestVersion (LatestVersion));

			var currentVersion = Environment.GetEnvironmentVariable("VisualStudioVersion");
			if (!string.IsNullOrEmpty (currentVersion) && InstalledVersions.Contains(currentVersion)) {
				CurrentVersion = currentVersion;
				tracer.TraceInformation (Strings.VsVersions.CurrentVersion (currentVersion));
			}
		}

		public static string CurrentVersion { get; private set; }

		public static string LatestVersion { get; private set; }

		public static string[] InstalledVersions { get; private set; }

		/// <summary>
		/// Converts the token values for All, Current and Latest to their actual
		/// values, and returns a distinct list.
		/// </summary>
		public static string[] GetFinalVersions(string[] candidateVersions, string minimumVersion, string maximumVersion)
		{
			// If no version is specified, we default to current or latest.
			if (candidateVersions == null || candidateVersions.Length == 0)
				return new[] { CurrentVersion ?? LatestVersion };

			var vsVersions = candidateVersions.ToList();

			if (vsVersions.Any (vs => vs == VisualStudioVersion.All)) {
				vsVersions.AddRange (InstalledVersions);
				vsVersions.RemoveAll (vs => vs == VisualStudioVersion.All);
			}
			if (vsVersions.Any (vs => vs == VisualStudioVersion.Current)) {
				vsVersions.Add (CurrentVersion ?? LatestVersion);
				vsVersions.RemoveAll (vs => vs == VisualStudioVersion.Current);
			}
			if (vsVersions.Any (vs => vs == VisualStudioVersion.Latest)) {
				vsVersions.Add (LatestVersion);
				vsVersions.RemoveAll (vs => vs == VisualStudioVersion.Latest);
			}

			if (!string.IsNullOrEmpty (minimumVersion))
				vsVersions.RemoveAll (vs => vs.CompareTo(minimumVersion) == -1);
			if (!string.IsNullOrEmpty (maximumVersion))
				vsVersions.RemoveAll (vs => vs.CompareTo (maximumVersion) == 1);

			return vsVersions.Distinct().ToArray ();
		}
	}
}
