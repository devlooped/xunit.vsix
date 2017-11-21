using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Xunit.Properties;

namespace Xunit
{
	internal class VsixInstaller
	{
		public static void Initialize (string devEnvDir, string visualStudioVersion, string rootSuffix)
		{
			// Loading just the current path into the VS binding paths causes all
			// other deps to fail.
			//var probingPaths = new [] { GetType().Assembly.ManifestModule.FullyQualifiedName };

			// Add all currently loaded assemblies paths to the resolve paths.
			var probingPaths = AppDomain.CurrentDomain.GetAssemblies ()
				.Select (x => Path.GetDirectoryName (x.ManifestModule.FullyQualifiedName))
				.Where (x => x.StartsWith (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData)))
				.Distinct ()
				.ToArray ();
			var pkgDef = PkgTemplate + string.Join (
				Environment.NewLine,
				probingPaths.Select (path => string.Format ("\"{0}\"=\"\"", path)));
			var extensionsPath = Path.Combine (
				Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData),
				@"Microsoft\VisualStudio",
				visualStudioVersion + rootSuffix,
				"Extensions");

			// Touch file so that configuration is refreshed.
			var pkgFile = Directory.EnumerateFiles(extensionsPath, "xunit.vsix.pkgdef", SearchOption.AllDirectories).FirstOrDefault();
			if (!File.Exists (pkgFile)) {
				Constants.Tracer.TraceInformation (Strings.VsixInstaller.InstallingExtension ("xunit.vsix.pkgdef", extensionsPath));

				var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
				Directory.CreateDirectory (outDir);
				try {
					File.WriteAllText (Path.Combine (outDir, "extension.vsixmanifest"), VsixTemplate);
					File.WriteAllText (Path.Combine (outDir, "xunit.vsix.pkgdef"), pkgDef);

					var vsixFile = Path.GetTempFileName();
					try {
						using (var package = Package.Open (vsixFile, FileMode.Create)) {
							foreach (var item in Directory.EnumerateFiles (outDir)) {
								var info = new FileInfo (item);
								var partUri = PackUriHelper.CreatePartUri (new Uri (info.Name, UriKind.Relative));
								if (!package.PartExists (partUri)) {
									var part = package.CreatePart (partUri, info.Extension == ".pkgdef" ? "text/plain" : "text/xml");
									using (var stream = File.OpenRead (info.FullName)) {
										stream.WriteTo (part.GetStream ());
									}
								}
							}
						}
						Install (vsixFile, devEnvDir, visualStudioVersion, rootSuffix);
					} finally {
						try {
							File.Delete (vsixFile);
						} catch (IOException) { }
					}
				} finally {
					try {
						Directory.Delete (outDir, true);
					} catch (IOException) { }
				}
			} else {
				File.WriteAllText (pkgFile, pkgDef);

				// Notify VS of the configuration changes
				File.WriteAllText (Path.Combine (extensionsPath, "extensions.configurationchanged"), "");
				using (var key = Registry.CurrentUser.OpenSubKey (@"Software\Microsoft\VisualStudio\" + visualStudioVersion + rootSuffix, true)) {
					var changedDate = DateTime.UtcNow.ToFileTimeUtc ();
					key.SetValue ("ConfigurationChanged", changedDate, RegistryValueKind.QWord);
				}
			}
		}

		/// <summary>
		/// Ensures the xunit.vsix is installed.
		/// </summary>
		static void Install (string vsixFile, string devEnvDir, string visualStudioVersion, string rootSuffix)
		{
			var extensionManagerFile = Path.Combine(devEnvDir, "PrivateAssemblies", "Microsoft.VisualStudio.ExtensionManager.Implementation.dll");
			if (!File.Exists (extensionManagerFile))
				throw new ArgumentException (Strings.VsixInstaller.MissingExtensionManager (extensionManagerFile));

			// C:\Program Files (x86)\Microsoft Visual Studio 14.0\VSSDK\VisualStudioIntegration\Common\Assemblies\v4.0\Microsoft.VisualStudio.Settings.14.0.dll
			var settingsFile = Path.GetFullPath ( Path.Combine (devEnvDir, @"..\..\VSSDK\VisualStudioIntegration\\Common\Assemblies\v4.0", "Microsoft.VisualStudio.Settings." + visualStudioVersion + ".dll"));
			if (!Directory.Exists (Path.GetDirectoryName (settingsFile)))
				throw new ArgumentException (Strings.VsixInstaller.MissingVSSDK (visualStudioVersion));

			if (!File.Exists (settingsFile))
				throw new ArgumentException (Strings.VsixInstaller.MissingSettingsManager (settingsFile));

			var extensionManagerAsm = Assembly.LoadFrom (extensionManagerFile);
			var settingsManagerAsm = Assembly.LoadFrom(settingsFile);

			var extensionManagerService = extensionManagerAsm.GetType("Microsoft.VisualStudio.ExtensionManager.ExtensionManagerService", true);
			var settingsManager = settingsManagerAsm.GetType("Microsoft.VisualStudio.Settings.ExternalSettingsManager", true);

			var createInstallable = extensionManagerService.GetMethod("CreateInstallableExtension", BindingFlags.Static | BindingFlags.Public, null, new [] { typeof(string) }, null);
			var createSettings = settingsManager.GetMethod("CreateForApplication", BindingFlags.Static | BindingFlags.Public, null, new [] { typeof(string), typeof(string) }, null);

			dynamic extension = createInstallable.Invoke(null, new object[] { vsixFile });
			dynamic settings = createSettings.Invoke(null, new object[] { Path.Combine(devEnvDir, "devenv.exe"), rootSuffix, });

			dynamic service = Activator.CreateInstance(extensionManagerService, settings);
			dynamic reason = service.Install (extension, false);
		}

		const string PkgTemplate = @"[$RootKey$\BindingPaths\{FFFFFFFF-EEEE-DDDD-CCCC-BBBBBBAAAAAA}]
";
		const string VsixTemplate = @"<?xml version='1.0' encoding='utf-8'?>
<PackageManifest Version='2.0.0' xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011' xmlns:d='http://schemas.microsoft.com/developer/vsx-schema-design/2011'>
  <Metadata>
    <Identity Id='xunit.vsix' Version='1.0' Language='en-US' Publisher='kzu' />
    <DisplayName>xUnit.net [VsixFact and VsixTheory]</DisplayName>
    <Description xml:space='preserve'>Provides VSIX runtime testing support for xunit.</Description>
  </Metadata>
  <Installation InstalledByMsi='false'>
    <InstallationTarget Id='Microsoft.VisualStudio.Pro' Version='[11.0,12.0]' />
    <InstallationTarget Id='Microsoft.VisualStudio.Community' Version='[14.0,)' />
  </Installation>
  <Dependencies>
  </Dependencies>
  <Assets>
    <Asset Type='Microsoft.VisualStudio.VsPackage' d:Source='File' Path='xunit.vsix.pkgdef' />
  </Assets>
</PackageManifest>";
	}
}
