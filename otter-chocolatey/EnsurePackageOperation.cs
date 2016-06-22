using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Configurations;
using Inedo.Otter.Extensibility.Operations;
using Inedo.Otter.Extensions.Configurations.Chocolatey;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inedo.Otter.Extensions.Operations.Chocolatey
{
    [DisplayName("Ensure Package (Chocolatey)")]
    [Description("Ensures a Chocolatey package is installed on a server.")]
    [ScriptNamespace("Chocolatey")]
    [ScriptAlias("Ensure-Package")]
    public sealed class EnsurePackageOperation : EnsureOperation<ChocolateyPackageConfiguration>
    {
        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            if (string.IsNullOrEmpty(config[nameof(ChocolateyPackageConfiguration.Version)]))
            {
                return new ExtendedRichDescription(new RichDescription("Ensure latest version of ", new Hilite(config[nameof(ChocolateyPackageConfiguration.PackageName)]), " from Chocolatey is installed"));
            }
            return new ExtendedRichDescription(new RichDescription("Ensure version ", new Hilite(config[nameof(ChocolateyPackageConfiguration.Version)]), " of ", new Hilite(config[nameof(ChocolateyPackageConfiguration.PackageName)]), " from Chocolatey is installed"));
        }

        public override async Task<PersistedConfiguration> CollectAsync(IOperationExecutionContext context)
        {
            var args = new List<string>();
            args.Add("upgrade");
            args.Add("--yes");
            args.Add("--limit-output");
            args.Add("--fail-on-unfound");
            args.Add("--what-if");
            args.Add(this.Template.PackageName);

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

            var split = output.ToString().Trim().Split('|');
            if (split.Length != 4 || split[0] != this.Template.PackageName || split[3] != "false") // FIXME: this assumes packages are never pinned
            {
                this.LogInformation($"Package {this.Template.PackageName} is not installed.");
                return new ChocolateyPackageConfiguration
                {
                    PackageName = this.Template.PackageName,
                    Version = "not-installed",
                };
            }

            this.LogInformation($"Package {this.Template.PackageName} is at version {split[1]}.");

            if (string.IsNullOrEmpty(this.Template.Version) && split[1] == split[2])
            {
                this.LogInformation($"The latest version is {split[2]}.");
                return this.Template;
            }

            return new ChocolateyPackageConfiguration
            {
                PackageName = this.Template.PackageName,
                Version = split[1],
            };
        }

        public override ComparisonResult Compare(PersistedConfiguration other)
        {
            var config = (ChocolateyPackageConfiguration)other;
            if (string.IsNullOrEmpty(config.Version))
                return new ComparisonResult(Enumerable.Empty<Difference>());
            else if (string.IsNullOrEmpty(this.Template.Version))
                return new ComparisonResult(new[] { new Difference("Version", "latest", config.Version) });
            else if (this.Template.Version != config.Version)
                return new ComparisonResult(new[] { new Difference("Version", this.Template.Version, config.Version) });
            else
                return new ComparisonResult(Enumerable.Empty<Difference>());
        }

        public override async Task ConfigureAsync(IOperationExecutionContext context)
        {
            var args = new List<string>();
            args.Add("upgrade");
            args.Add("--yes");
            args.Add("--fail-on-unfound");
            if (context.Simulation)
            {
                args.Add("--what-if");
            }
            if (!string.IsNullOrEmpty(this.Template.Version))
            {
                args.Add("--version");
                args.Add(this.Template.Version);
            }
            args.Add(this.Template.PackageName);

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
