using System;
using System.Linq;

namespace Xunit
{
	/// <summary>
	/// Processes and loads available VS versions.
	/// </summary>
	static class VsVersions
	{
		static VsVersions ()
		{
			InstalledVersions = (from version in Enumerable.Range (10, 20)
								// VSSDK100Install
								 let varName = "VSSDK" + version + "0Install"
								 where !string.IsNullOrEmpty (Environment.GetEnvironmentVariable (varName))
								 select version + ".0")
								.ToArray ();

			LatestVersion = InstalledVersions.LastOrDefault ();

			var currentVersion = Environment.GetEnvironmentVariable("VisualStudioVersion");
			if (!string.IsNullOrEmpty (currentVersion) && InstalledVersions.Contains(currentVersion))
				CurrentVersion = currentVersion;
		}

		public static string CurrentVersion { get; private set; }

		public static string LatestVersion { get; private set; }

		public static string[] InstalledVersions { get; private set; }

		/// <summary>
		/// Converts the token values for All, Current and Latest to their actual 
		/// values, and returns a distinct list.
		/// </summary>
		public static string[] GetFinalVersions(string[] candidateVersions)
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

			return vsVersions.Distinct().ToArray ();
		}
	}
}
