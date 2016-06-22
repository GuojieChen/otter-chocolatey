using Inedo.Documentation;
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Configurations;
using Inedo.Serialization;
using System;
using System.ComponentModel;

namespace Inedo.Otter.Extensions.Configurations.Chocolatey
{
    [Serializable]
    [DisplayName("Chocolatey Package")]
    public sealed class ChocolateyPackageConfiguration : PersistedConfiguration
    {
        [ConfigurationKey]
        [Persistent]
        [Required]
        [ScriptAlias("Name")]
        [DisplayName("Package Name")]
        public string PackageName { get; set; }

        [Persistent]
        [ScriptAlias("Version")]
        [DisplayName("Version")]
        [Description("The version number of the package to install. Leave blank for the latest version.")]
        public string Version { get; set; }
    }
}