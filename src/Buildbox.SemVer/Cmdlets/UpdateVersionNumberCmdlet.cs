﻿using Ackara.Buildbox.SemVer.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Ackara.Buildbox.SemVer.Cmdlets
{
    [Cmdlet(VerbsData.Update, "VersionNumber")]
    public class UpdateVersionNumberCmdlet : CmdletBase
    {
        [Parameter(Position = 1, ValueFromPipeline = true)]
        public string ProjectDirectory { get; set; }

        [Parameter]
        public string ReleaseTag { get; set; }

        [Parameter]
        [Alias(new string[] { "m", "message" })]
        public string CommitMessage { get; set; }

        [Parameter]
        public SwitchParameter TagCommit { get; set; }

        [Parameter]
        public SwitchParameter CommitChanges { get; set; }

        [Parameter]
        public SwitchParameter CommitAddUnstagedFiles { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            var factory = new FileHandlerFactory();
            foreach (var handler in Config.Handlers)
            {
                var instance = factory.Create(handler);
                _handlers.Add(instance);
            }
        }

        protected override void ProcessRecord()
        {
            if (Directory.Exists(ProjectDirectory) == false) ProjectDirectory = Environment.CurrentDirectory;
            var git = new Git(ProjectDirectory);
            Config.Version.ReleaseTag = ReleaseTag ?? GetReleaseTag(git);

            var modifiedFiles = new List<FileInfo>();
            foreach (var handler in _handlers)
            {
                foreach (var file in handler.FindTargets(ProjectDirectory))
                {
                    handler.Update(file, Config.Version);
                    modifiedFiles.Add(file);
                }
            }

            if (CommitChanges.IsPresent || Config.ShouldCommitChanges)
            {
                if (CommitAddUnstagedFiles.IsPresent || Config.ShouldAddUnstagedFilesWhenCommitting)
                { git.Add(); }
                else
                { git.Add(modifiedFiles.Select(x => x.FullName).ToArray()); }

                if (string.IsNullOrWhiteSpace(CommitMessage))
                {
                    var defaultMessage = new StringBuilder();
                    defaultMessage.AppendLine($"Increment the project's version number to {Config.Version.ToString(withoutTag: true)}");
                    CommitMessage = defaultMessage.ToString();
                }

                git.Commit(CommitMessage);
                if (TagCommit.IsPresent || Config.ShouldTagCommit) { git.CreateTag($"v{Config.Version}"); }
            }

            WriteObject(modifiedFiles, enumerateCollection: true);
        }

        #region Private Members

        private ICollection<IFileHandler> _handlers = new List<IFileHandler>();

        private string GetReleaseTag(Git git)
        {
            if (Config.ReleaseTags.Count == 0) return string.Empty;
            else try
                {
                    string branchName = git.GetCurrentBranch();
                    branchName = (Config.ReleaseTags.ContainsKey("*") && (Config.ReleaseTags.ContainsKey(branchName) == false)) ? "*" : branchName;
                    return Config.ReleaseTags[branchName];
                }
                catch (KeyNotFoundException)
                {
                    return Config.Version.ReleaseTag ?? string.Empty;
                }
        }

        #endregion Private Members
    }
}