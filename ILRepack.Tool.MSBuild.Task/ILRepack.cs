using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#nullable enable

namespace KageKirin.ILRepack.Tool.MSBuild.Task;

public class ILRepack : Microsoft.Build.Utilities.Task, IDisposable
{
    public virtual bool Parallel { get; set; }

    public virtual bool DebugInfo { get; set; }

    public virtual bool Verbose { get; set; }

    public virtual bool Internalize { get; set; }

    public virtual bool RenameInternalized { get; set; }

    public virtual string TargetKind { get; set; } = string.Empty;

    public virtual bool Wildcards { get; set; }

    public virtual bool DelaySign { get; set; }

    public virtual bool ExcludeInternalizeSerializable { get; set; }

    public virtual bool Union { get; set; }

    public virtual bool AllowDup { get; set; }

    public virtual bool AllowDuplicateResources { get; set; }

    public virtual bool NoRepackRes { get; set; }

    public virtual bool CopyAttrs { get; set; }

    public virtual bool AllowMultiple { get; set; }

    public virtual bool KeepOtherVersionReferences { get; set; }

    public virtual bool PreserveTimestamp { get; set; }

    public virtual bool SkipConfig { get; set; }

    public virtual bool ILLink { get; set; }

    public virtual bool XmlDocs { get; set; }

    public virtual bool ZeroPEKind { get; set; }

    public virtual string Version { get; set; } = string.Empty;

    [Required]
    public virtual Microsoft.Build.Framework.ITaskItem[] InputAssemblies { get; set; } = [];

    public virtual Microsoft.Build.Framework.ITaskItem[] LibraryPaths { get; set; } = [];

    public virtual Microsoft.Build.Framework.ITaskItem[] InternalizeExclude { get; set; } = [];

    [Required]
    public virtual Microsoft.Build.Framework.ITaskItem OutputFile { get; set; } = default;

    public virtual Microsoft.Build.Framework.ITaskItem LogFile { get; set; } = default;

    public virtual Microsoft.Build.Framework.ITaskItem[] FilterAssemblies { get; set; } = [];

    public virtual Microsoft.Build.Framework.ITaskItem[] ImportAttributeAssemblies { get; set; } =
        [];

    public virtual Microsoft.Build.Framework.ITaskItem[] InternalizeAssemblies { get; set; } = [];

    public virtual Microsoft.Build.Framework.ITaskItem[] RepackDropAttributes { get; set; } = [];

    public virtual Microsoft.Build.Framework.ITaskItem[] AllowedDuplicateTypes { get; set; } = [];

    public virtual Microsoft.Build.Framework.ITaskItem KeyFile { get; set; } = default;

    public virtual Microsoft.Build.Framework.ITaskItem KeyContainer { get; set; } = default;

    public virtual int Timeout { get; set; } = 30;

    public virtual bool Success { get; set; }

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.High, "ILRepack: preparing inputs");

        var cmdParams = new List<string>();

        if (Parallel)
            cmdParams.Add("/parallel");

        if (!DebugInfo)
            cmdParams.Add("/ndebug");

        if (Verbose)
            cmdParams.Add("/verbose");

        if (Internalize)
            cmdParams.Add("/internalize");

        if (RenameInternalized)
            cmdParams.Add("/renameinternalized");

        if (Wildcards)
            cmdParams.Add("/wildcards");

        if (DelaySign)
            cmdParams.Add("/delaysign");

        if (ExcludeInternalizeSerializable)
            cmdParams.Add("/excludeinternalizeserializable");

        if (Union)
            cmdParams.Add("/union");

        if (AllowDup)
            cmdParams.Add("/allowdup");

        if (AllowDuplicateResources)
            cmdParams.Add("/allowduplicateresources");

        if (NoRepackRes)
            cmdParams.Add("/noRepackRes");

        if (CopyAttrs)
            cmdParams.Add("/copyattrs");

        if (AllowMultiple)
            cmdParams.Add("/allowMultiple");

        if (KeepOtherVersionReferences)
            cmdParams.Add("/keepotherversionreferences");

        if (PreserveTimestamp)
            cmdParams.Add("/preservetimestamp");

        if (SkipConfig)
            cmdParams.Add("/skipconfig");

        if (ILLink)
            cmdParams.Add("/illink");

        if (XmlDocs)
            cmdParams.Add("/xmldocs");

        if (ZeroPEKind)
            cmdParams.Add("/zeropekind");

        if (!string.IsNullOrWhiteSpace(TargetKind))
            cmdParams.Add($"/target:{TargetKind}");

        if (!string.IsNullOrWhiteSpace(Version))
            cmdParams.Add($"/ver:{Version}");

        if (!string.IsNullOrWhiteSpace(OutputFile.ItemSpec))
            cmdParams.Add($"/out:\"{OutputFile.ItemSpec}\"");

        if (LogFile is not null && !string.IsNullOrWhiteSpace(LogFile.ItemSpec))
            cmdParams.Add($"/log:\"{LogFile.ItemSpec}\"");

        if (KeyFile is not null && !string.IsNullOrWhiteSpace(KeyFile.ItemSpec))
            cmdParams.Add($"/keyfile:\"{KeyFile.ItemSpec}\"");

        if (KeyContainer is not null && !string.IsNullOrWhiteSpace(KeyContainer.ItemSpec))
            cmdParams.Add($"/keycontainer:\"{KeyContainer.ItemSpec}\"");

        if (ImportAttributeAssemblies is not null && ImportAttributeAssemblies.Length > 0)
            cmdParams.AddRange(ImportAttributeAssemblies.Select(f => $"/attr:\"{f.ItemSpec}\""));

        if (InternalizeAssemblies is not null && InternalizeAssemblies.Length > 0)
            cmdParams.AddRange(
                InternalizeAssemblies.Select(f => $"/internalizeassembly:\"{f.ItemSpec}\"")
            );

        if (RepackDropAttributes is not null && RepackDropAttributes.Length > 0)
            cmdParams.AddRange(
                RepackDropAttributes.Select(attr => $"/repackdrop:\"{attr.ItemSpec}\"")
            );

        if (AllowedDuplicateTypes is not null && AllowedDuplicateTypes.Length > 0)
            cmdParams.AddRange(AllowedDuplicateTypes.Select(t => $"/allowdup:\"{t.ItemSpec}\""));

        if (InternalizeExclude is not null && InternalizeExclude.Length > 0)
        {
            string excludeFile = Path.Combine(
                Path.GetDirectoryName(OutputFile.ItemSpec),
                "ILRepack.not"
            );
            using StreamWriter writer = new(
                path: excludeFile,
                append: false,
                encoding: Encoding.UTF8
            );

            foreach (var asm in InternalizeExclude)
            {
                writer.WriteLine(
                    asm.ItemSpec.EndsWith(".dll")
                        ? Path.GetFileNameWithoutExtension(asm.ItemSpec)
                        : Path.GetFileName(asm.ItemSpec)
                );
            }

            cmdParams.Add($"/internalize:\"{excludeFile}\"");
        }

        Log.LogMessage(
            MessageImportance.High,
            $"ILRepack: InputAssemblies (unfiltered): {string.Join("\n", InputAssemblies.Select(f => f.ItemSpec))}"
        );
        if (FilterAssemblies.Length > 0)
        {
            InputAssemblies = InputAssemblies
                .Where(item =>
                    FilterAssemblies.Any(x =>
                        item.GetMetadata("Filename").ToLower().StartsWith(x.ItemSpec.ToLower())
                        || Regex.IsMatch(
                            item.GetMetadata("Filename"),
                            x.ItemSpec,
                            RegexOptions.IgnoreCase
                        )
                    )
                )
                .Distinct()
                .ToArray();
        }
        Log.LogMessage(
            MessageImportance.High,
            $"ILRepack: InputAssemblies (filtered): {string.Join("\n", InputAssemblies.Distinct().Select(f => f.ItemSpec))}"
        );

        if (LibraryPaths is not null && LibraryPaths.Length > 0)
            cmdParams.AddRange(
                LibraryPaths.Select(item => item.ItemSpec).Select(l => $"/lib:\"{l}\"").Distinct()
            );
        else
            cmdParams.AddRange(
                InputAssemblies
                    .Select(item => Path.GetDirectoryName(item.ItemSpec))
                    .Select(l => $"/lib:\"{l}\"")
                    .Distinct()
            );

        // must come last
        cmdParams.AddRange(InputAssemblies.Select(item => $"\"{item.ItemSpec}\"").Distinct());

        var thisPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var ilrepack = Path.Combine(thisPath, "tools", "ILRepack.exe");
        if (!File.Exists(ilrepack))
            ilrepack = "ilrepack";

        string command = ilrepack + " " + string.Join(" ", cmdParams);
        Log.LogMessage(MessageImportance.High, $"ILRepack: running `{command}`");

        Process process = new()
        {
            StartInfo = new()
            {
                FileName = "dotnet",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        Log.LogMessage(MessageImportance.High, $"ILRepack[output]: {output}");

        string error = process.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(error))
            Log.LogError($"ILRepack[error]: {error}");

        process.WaitForExit(Timeout > 0 ? Timeout * 1000 : 300000);
        Log.LogMessage(MessageImportance.High, $"ILRepack: exit code {process.ExitCode}");

        Success = process.ExitCode == 0;

        return Success;
    }

    public virtual void Dispose() { }
}
