using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Operations;
using Inedo.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace Inedo.Otter.Extensions.Operations.Chocolatey
{
    [DisplayName("Install Package (Chocolatey)")]
    [Description("Installs a Chocolatey package on a server.")]
    [ScriptNamespace("Chocolatey")]
    [ScriptAlias("Install-Package")]
    [DefaultProperty(nameof(PackageName))]
    public sealed class InstallPackageOperation : ExecuteOperation
    {
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

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            if (string.IsNullOrEmpty(this.Version))
            {
                return new ExtendedRichDescription(new RichDescription("Install latest version of ", new Hilite(this.PackageName), " from Chocolatey"));
            }
            return new ExtendedRichDescription(new RichDescription("Install version ", new Hilite(this.Version), " of ", new Hilite(this.PackageName), " from Chocolatey"));
        }
        
        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var args = new List<string>();
            args.Add("upgrade");
            args.Add("--yes");
            args.Add("--fail-on-unfound");
            if (context.Simulation)
            {
                args.Add("--what-if");
            }
            if (!string.IsNullOrEmpty(this.Version))
            {
                args.Add("--version");
                args.Add(this.Version);
            }
            args.Add(this.PackageName);

            var output = new StringBuilder();
            var remoteProcs = context.Agent.GetService<IRemoteProcessExecuter>();
            using (var process = remoteProcs.CreateProcess(new RemoteProcessStartInfo
            {
                FileName = "choco",
                Arguments = CommandLine.FromArgs(args),
            }))
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    output.Append(e.Data);
                    this.LogDebug(e.Data);
                };
                process.Start();
                try
                {
                    await process.WaitAsync(context.CancellationToken);
                }
                catch
                {
                    try { process.Terminate(); }
                    catch { }

                    throw;
                }
                if (process.ExitCode != 0)
                {
                    throw new Exception(output.ToString());
                }
            }
        }
    }
}
