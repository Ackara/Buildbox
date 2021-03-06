using Acklann.Ncrement.Extensions;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace Acklann.Ncrement.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Edit a manifest file.</para>
    /// </summary>
    /// <seealso cref="Acklann.Ncrement.Cmdlets.ManifestCmdletBase" />
    [Cmdlet(VerbsData.Edit, (nameof(Ncrement) + nameof(Manifest)), ConfirmImpact = ConfirmImpact.Medium)]
    public class EditManifestCmdlet : ManifestCmdletBase
    {
        /// <summary>
        /// <para type="description">The absolute path of the manifest file.</para>
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Parameter(Position = 1)]
        public string ManifestPath { get; set; }

        /// <summary>
        /// <para type="description">The [Manifest] to be used to overwritting the file.</para>
        /// </summary>
        [Alias(nameof(PathInfo.Path), nameof(FileInfo.FullName))]
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public PSObject InputObject { get; set; }

        /// <summary>
        /// <para type="description">Determine whether the modified file should be staged in source control.</para>
        /// </summary>
        [Parameter]
        public SwitchParameter Stage { get; set; }

        /// <summary>
        /// Processes the record.
        /// </summary>
        protected override void ProcessRecord()
        {
            Manifest manifest = null; string manifestPath = null;
            InputObject?.GetManifestInfo(out manifest, out manifestPath);

            manifest = Overwrite(manifest ?? Manifest.LoadFrom(ManifestPath = ManifestPath ?? manifestPath));
            string json = Editor.UpdateManifestFile(ManifestPath, manifest);

            using (var file = new FileStream(ManifestPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(file, Encoding.UTF8))
            {
                writer.Write(json);
                writer.Flush();
            }

            if (Stage) Git.Stage(ManifestPath);

            WriteObject(manifest.ToPSObject());
        }
    }
}