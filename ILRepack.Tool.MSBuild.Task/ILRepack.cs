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
    /// <summary>
    /// If `DuplicateTypes` is not specified, allow all duplicate types.
    /// See `<ILRepackAllowDuplicateTypes>` below for more information.
    ///
    /// Corresponds to:
    /// `<ILRepackAllowAllDuplicateTypes>` property
    /// `/allowdup` command line argument
    /// </summary>
    public virtual bool AllowAllDuplicateTypes { get; set; }

    /// <summary>
    /// Allows to duplicate resources in output assembly (by default they're ignored).
    ///
    /// Corresponds to:
    /// `<ILRepackAllowDuplicateResources>` property
    /// `/allowduplicateresources` command line argument
    /// </summary>
    public virtual bool AllowDuplicateResources { get; set; }

    /// <summary>
    /// When specified, allows multiple attributes (if type allows).
    ///
    /// Corresponds to:
    /// `<ILRepackAllowMultipleAssemblyLevelAttributes>` property
    /// `/allowMultiple` command line argument
    /// </summary>
    public virtual bool AllowMultipleAssemblyLevelAttributes { get; set; }

    /// <summary>
    /// Allows (and resolves) file wildcards (e.g. *.dll) in input assemblies.
    ///
    /// Corresponds to:
    /// `<ILRepackAllowWildCards>` property
    /// `/wildcards` command line argument
    /// </summary>
    public virtual bool AllowWildCards { get; set; }

    /// <summary>
    /// Allows assemblies with Zero PeKind (but obviously only IL will get merged).
    ///
    /// Corresponds to:
    /// `<ILRepackAllowZeroPeKind>` property
    /// `/zeropekind` command line argument
    /// </summary>
    public virtual bool AllowZeroPeKind { get; set; }

    /// <summary>
    /// NOT IMPLEMENTED.
    ///
    /// Corresponds to:
    /// `<ILRepackClosed>` property
    /// `/closed` command line argument
    /// </summary>
    public virtual bool Closed { get; set; }

    /// <summary>
    /// Copy assembly attributes (by default only the primary assembly attributes are copied).
    ///
    /// Corresponds to:
    /// `<ILRepackCopyAttributes>` property
    /// `/copyattrs` command line argument
    /// </summary>
    public virtual bool CopyAttributes { get; set; }

    /// <summary>
    /// Enables symbol file generation. explcitly set to `false` to disable default behaviour.
    ///
    /// Corresponds to:
    /// `<ILRepackDebugInfo>` property
    /// `/ndebug` command line argument (when omitted)
    /// </summary>
    public virtual bool DebugInfo { get; set; }

    /// <summary>
    /// Set the key, but don't sign the assembly.
    ///
    /// Corresponds to:
    /// `<ILRepackDelaySign>` property
    /// `/delaysign` command line argument
    /// </summary>
    public virtual bool DelaySign { get; set; }

    /// <summary>
    /// Do not internalize types marked as Serializable.
    ///
    /// Corresponds to:
    /// `<ILRepackExcludeInternalizeSerializable>` property
    /// `/excludeinternalizeserializable` command line argument
    /// </summary>
    public virtual bool ExcludeInternalizeSerializable { get; set; }

    /// <summary>
    /// Make all types except in the first assembly 'internal'.
    /// Types in the transitive closure of public API remain public.
    ///
    /// Corresponds to:
    /// `<ILRepackInternalize>` property
    /// `/internalize` command line argument
    /// </summary>
    public virtual bool Internalize { get; set; }

    /// <summary>
    /// Take reference assembly version into account when removing references.
    ///
    /// Corresponds to:
    /// `<ILRepackKeepOtherVersionReferences>` property
    /// `/keepotherversionreferences` command line argument
    /// </summary>
    public virtual bool KeepOtherVersionReferences { get; set; }

    /// <summary>
    /// Stores file:line debug information as type/method attributes (requires PDB).
    ///
    /// Corresponds to:
    /// `<ILRepackLineIndexation>` property
    /// `/index` command line argument
    /// </summary>
    public virtual bool LineIndexation { get; set; }

    /// <summary>
    /// More detailed logging.
    ///
    /// Corresponds to:
    /// `<ILRepackLogVerbose>` property
    /// `/verbose` command line argument
    /// </summary>
    public virtual bool LogVerbose { get; set; }

    /// <summary>
    /// Merge IL Linker files.
    ///
    /// Corresponds to:
    /// `<ILRepackMergeIlLinkerFiles>` property
    /// `/illink` command line argument
    /// </summary>
    public virtual bool MergeIlLinkerFiles { get; set; }

    /// <summary>
    /// Do not add the resource 'ILRepack.List' with all merged assembly names.
    ///
    /// Corresponds to:
    /// `<ILRepackNoRepackRes>` property
    /// `/noRepackRes` command line argument
    /// </summary>
    public virtual bool NoRepackRes { get; set; }

    /// <summary>
    /// Use as many CPUs as possible to merge the assemblies.
    ///
    /// Corresponds to:
    /// `<ILRepackParallel>` property
    /// `/parallel` command line argument
    /// </summary>
    public virtual bool Parallel { get; set; }

    /// <summary>
    /// Pause execution once completed, good for debugging, don't use for CI/CD.
    ///
    /// Corresponds to:
    /// `<ILRepackPauseBeforeExit>` property
    /// `/pause` command line argument
    /// </summary>
    public virtual bool PauseBeforeExit { get; set; }

    /// <summary>
    /// Preserve original file PE timestamp.
    ///
    /// Corresponds to:
    /// `<ILRepackPreserveTimestamp>` property
    /// `/preservetimestamp` command line argument
    /// </summary>
    public virtual bool PreserveTimestamp { get; set; }

    /// <summary>
    /// Undocumented.
    /// </summary>
    public virtual bool PublicKeyTokens { get; set; }

    /// <summary>
    /// Rename each internalized type to a new unique name.
    ///
    /// Corresponds to:
    /// `<ILRepackRenameInternalized>` property
    /// `/renameinternalized` command line argument
    /// </summary>
    public virtual bool RenameInternalized { get; set; }

    /// <summary>
    /// Skips merging config files.
    ///
    /// Corresponds to:
    /// `<ILRepackSkipConfigMerge>` property
    /// `/skipconfig` command line argument
    /// </summary>
    public virtual bool SkipConfigMerge { get; set; }

    /// <summary>
    /// Undocumented.
    /// </summary>
    public virtual bool StrongNameLost { get; set; }

    /// <summary>
    /// Merges types with identical names into one.
    ///
    /// Corresponds to:
    /// `<ILRepackUnionMerge>` property
    /// `/union` command line argument
    /// </summary>
    public virtual bool UnionMerge { get; set; }

    /// <summary>
    /// Merges XML documentation as well.
    ///
    /// Corresponds to:
    /// `<ILRepackXmlDocumentation>` property
    /// `/xmldocs` command line argument
    /// </summary>
    public virtual bool XmlDocumentation { get; set; }

    /// <summary>
    /// NOT IMPLEMENTED.
    ///
    /// Corresponds to:
    /// `<ILRepackFileAlignment>` property
    /// `/align` command line argument
    /// </summary>
    public virtual int FileAlignment { get; set; }

    /// <summary>
    /// Target assembly kind [library|exe|winexe], default is same as primary assembly.
    ///
    /// Corresponds to:
    /// `<ILRepackTargetKind>` property
    /// `/target: command line argument
    /// </summary>
    public virtual string TargetKind { get; set; } = string.Empty;

    /// <summary>
    /// Specify target platform path.
    ///
    /// Corresponds to:
    /// `<ILRepackTargetPlatformDirectory>` property
    /// `/targetplatform: command line argument (first part of P)
    /// </summary>
    public virtual string TargetPlatformDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Specify target platform version (v1, v1.1, v2, v4 supported).
    ///
    /// Corresponds to:
    /// `<ILRepackTargetPlatformVersion>` property
    /// `/targetplatform:P` command line argument (second part of P)
    /// </summary>
    public virtual string TargetPlatformVersion { get; set; } = string.Empty;

    /// <summary>
    /// Target assembly version.
    ///
    /// Corresponds to:
    /// `<ILRepackVersion>` property
    /// `/ver:X.Y.Z` command line argument
    /// </summary>
    public virtual string Version { get; set; } = string.Empty;

    /// <summary>
    /// Key container.
    ///
    /// Corresponds to:
    ///  `<ILRepackKeyContainer>` property
    /// `/keycontainer:<c>` command line argument
    /// </summary>
    public virtual Microsoft.Build.Framework.ITaskItem KeyContainer { get; set; } = default;

    /// <summary>
    /// Keyfile to sign the output assembly.
    ///
    /// Corresponds to:
    ///  `<ILRepackKeyFile>` property
    /// `/keyfile:<path>` command line argument
    /// </summary>
    public virtual Microsoft.Build.Framework.ITaskItem KeyFile { get; set; } = default;

    /// <summary>
    /// Enable logging to the given file (default is disabled).
    ///
    /// Corresponds to:
    ///  `<ILRepackLogFile>` property
    /// `/log:<logfile>` command line argument
    /// </summary>
    public virtual Microsoft.Build.Framework.ITaskItem LogFile { get; set; } = default;

    /// <summary>
    /// Keep duplicates of the specified type, may be specified more than once.
    ///
    /// Corresponds to:
    ///  `<ILRepackAllowedDuplicateTypes>` property
    /// `/allowdup:Type`   command line argument
    /// </summary>
    public virtual Microsoft.Build.Framework.ITaskItem[] AllowedDuplicateTypes { get; set; } = [];

    /// <summary>
    /// Optional list of assemblies (names or regex) to keep among all the input assemblies.
    /// Non-matching ones are discarded from the inputs.
    /// The reason for this is that filtering the sometimes many input assemblies using only MSBuild functions is rather complex,
    /// whereas this simple list simplifies the process extremely.
    ///
    /// Corresponds to:
    /// `<ILRepackFilterAssemblies>`
    ///
    ///
    /// </summary>
    public virtual Microsoft.Build.Framework.ITaskItem[] FilterAssemblies { get; set; } = [];

    /// <summary>
    /// Take assembly attributes from the given assembly file.
    ///
    /// Corresponds to:
    ///  `<ILRepackImportAttributeAssemblies>` property
    /// `/attr:<path>` command line argument
    /// </summary>
    public virtual Microsoft.Build.Framework.ITaskItem[] ImportAttributeAssemblies { get; set; } =
        [];

    /// <summary>
    /// Internalize a specific assembly name (no e propertxtension).
    /// May be specified more than once (one per assembly to internalize).
    /// If specified, no need to also specify `/internalize`.
    ///
    /// Corresponds to:
    /// `<ILRepackInternalizeAssemblies>`
    /// `/internalizeassembly:<path>`
    /// </summary>
    public virtual Microsoft.Build.Framework.ITaskItem[] InternalizeAssemblies { get; set; } = [];

    /// <summary>
    /// Each item is either regex or a full type name not to internalize, or an assembly name not to internalize (.dll extension optional).
    /// Internally, each item is written line-by-line into a `ILRepack.not` file that gets passed as.
    ///
    /// Corresponds to:
    /// `<ILRepackInternalizeExclude>`
    /// `/internalize:<exclude_file>`
    /// </summary>
    public virtual Microsoft.Build.Framework.ITaskItem[] InternalizeExclude { get; set; } = [];

    /// <summary>
    /// Path(s) to search directories used to resolve referenced assemblies (can be specified multiple times).
    /// If you get 'unable to resolve assembly' errors specify a path to a directory where the assembly can be found.
    ///
    /// Corresponds to:
    /// `<ILRepackLibraryPaths>`
    /// `/lib:<path>`
    /// </summary>
    public virtual Microsoft.Build.Framework.ITaskItem[] LibraryPaths { get; set; } = [];

    /// <summary>
    /// Allows dropping members denoted by this attribute name when merging.
    ///
    /// Corresponds to:
    ///  `<ILRepackRepackDropAttributes>` property
    /// `/repackdrop:RepackDropAttribute` command line argument
    /// </summary>
    public virtual Microsoft.Build.Framework.ITaskItem[] RepackDropAttributes { get; set; } = [];

    [Required]
    /// <summary>
    /// Paths to input assemblies in the following order:
    /// - **primary assembly**: gives the name, version to the merged one
    /// - **other assemblies**: other assemblies to merge with the primary one
    ///
    /// Corresponds to:
    /// `<ILRepackInputAssemblies>`
    /// </summary>
    public virtual Microsoft.Build.Framework.ITaskItem[] InputAssemblies { get; set; } = [];

    [Required]
    /// <summary>
    /// Target assembly path, symbol/config/doc files will be written here as well.
    ///
    /// Corresponds to:
    ///  `<ILRepackOutputFile>` property
    /// `/out:<path>` command line argument
    /// </summary>
    public virtual Microsoft.Build.Framework.ITaskItem OutputFile { get; set; } = default;

    /// <summary>
    /// Timeout to end task in seconds.
    /// This is a special value that allows to terminate the `ILRepack` task after the specified time to avoid endless loops eating up precious CI time. |.
    ///
    /// Corresponds to:
    /// `<ILRepackTimeout>` property
    /// </summary>
    public virtual int Timeout { get; set; } = 30;

    /// <summary>
    /// Task return value.
    /// </summary>
    public virtual bool Success { get; set; }

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.Low, "ILRepack: preparing inputs");

        var cmdParams = new List<string>();

        if (AllowAllDuplicateTypes)
            cmdParams.Add("/allowdup");

        if (AllowDuplicateResources)
            cmdParams.Add("/allowduplicateresources");

        if (AllowMultipleAssemblyLevelAttributes)
            cmdParams.Add("/allowMultiple");

        if (CopyAttributes)
            cmdParams.Add("/copyattrs");

        if (Closed)
            cmdParams.Add("/closed");

        if (!DebugInfo)
            cmdParams.Add("/ndebug");

        if (DelaySign)
            cmdParams.Add("/delaysign");

        if (ExcludeInternalizeSerializable)
            cmdParams.Add("/excludeinternalizeserializable");

        if (MergeIlLinkerFiles)
            cmdParams.Add("/illink");

        if (Internalize)
            cmdParams.Add("/internalize");

        if (KeepOtherVersionReferences)
            cmdParams.Add("/keepotherversionreferences");

        if (LineIndexation)
            cmdParams.Add("/index");

        if (NoRepackRes)
            cmdParams.Add("/noRepackRes");

        if (Parallel)
            cmdParams.Add("/parallel");

        if (PauseBeforeExit)
            cmdParams.Add("/pause");

        if (PreserveTimestamp)
            cmdParams.Add("/preservetimestamp");

        if (RenameInternalized)
            cmdParams.Add("/renameinternalized");

        if (SkipConfigMerge)
            cmdParams.Add("/skipconfig");

        if (UnionMerge)
            cmdParams.Add("/union");

        if (LogVerbose)
            cmdParams.Add("/verbose");

        if (AllowWildCards)
            cmdParams.Add("/wildcards");

        if (XmlDocumentation)
            cmdParams.Add("/xmldocs");

        if (AllowZeroPeKind)
            cmdParams.Add("/zeropekind");

        if (FileAlignment != 0)
            cmdParams.Add($"/align:{FileAlignment}");

        if (!string.IsNullOrWhiteSpace(TargetKind))
            cmdParams.Add($"/target:{TargetKind}");

        string targetplatform = string.Empty;
        if (!string.IsNullOrWhiteSpace(TargetPlatformDirectory))
            targetplatform += "${TargetPlatformDirectory},";
        if (!string.IsNullOrWhiteSpace(TargetPlatformVersion))
            targetplatform += TargetPlatformVersion;
        if (!string.IsNullOrWhiteSpace(targetplatform))
            cmdParams.Add($"/targetplatform:{targetplatform}");

        if (!string.IsNullOrWhiteSpace(Version))
            cmdParams.Add($"/ver:{Version}");

        if (KeyContainer is not null && !string.IsNullOrWhiteSpace(KeyContainer.ItemSpec))
            cmdParams.Add($"/keycontainer:\"{KeyContainer.ItemSpec}\"");

        if (KeyFile is not null && !string.IsNullOrWhiteSpace(KeyFile.ItemSpec))
            cmdParams.Add($"/keyfile:\"{KeyFile.ItemSpec}\"");

        if (LogFile is not null && !string.IsNullOrWhiteSpace(LogFile.ItemSpec))
            cmdParams.Add($"/log:\"{LogFile.ItemSpec}\"");

        if (AllowedDuplicateTypes is not null && AllowedDuplicateTypes.Length > 0)
            cmdParams.AddRange(AllowedDuplicateTypes.Select(t => $"/allowdup:\"{t.ItemSpec}\""));

        if (ImportAttributeAssemblies is not null && ImportAttributeAssemblies.Length > 0)
            cmdParams.AddRange(ImportAttributeAssemblies.Select(f => $"/attr:\"{f.ItemSpec}\""));

        if (InternalizeAssemblies is not null && InternalizeAssemblies.Length > 0)
            cmdParams.AddRange(
                InternalizeAssemblies.Select(f => $"/internalizeassembly:\"{f.ItemSpec}\"")
            );

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

        if (RepackDropAttributes is not null && RepackDropAttributes.Length > 0)
            cmdParams.AddRange(
                RepackDropAttributes.Select(attr => $"/repackdrop:\"{attr.ItemSpec}\"")
            );

        Log.LogMessage(
            MessageImportance.Low,
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
            MessageImportance.Low,
            $"ILRepack: InputAssemblies (filtered): {string.Join("\n", InputAssemblies.Distinct().Select(f => f.ItemSpec))}"
        );

        IEnumerable<string> searchDirectories = InputAssemblies
            .Select(item => Path.GetDirectoryName(Path.GetFullPath(item.ItemSpec)))
            .Distinct();
        if (LibraryPaths is not null && LibraryPaths.Length > 0)
            searchDirectories = searchDirectories
                .Concat(LibraryPaths.Select(item => Path.GetFullPath(item.ItemSpec)))
                .Distinct();

        cmdParams.AddRange(
            searchDirectories.Select(item => Path.GetFullPath(item)).Select(l => $"/lib:\"{l}\"")
        );

        if (!string.IsNullOrWhiteSpace(OutputFile.ItemSpec))
            cmdParams.Add($"/out:\"{Path.GetFullPath(OutputFile.ItemSpec)}\"");

        // must come last
        cmdParams.AddRange(InputAssemblies.Select(item => Path.GetFullPath(item.ItemSpec)).Distinct());

        var thisPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var ilrepack = Path.Combine(thisPath, "tools", "ILRepack.exe");
        if (!File.Exists(ilrepack))
            ilrepack = "ilrepack";

        var rspPath = Path.Combine(
            Path.GetDirectoryName(InputAssemblies[0].ItemSpec),
            Path.GetFileNameWithoutExtension(InputAssemblies[0].ItemSpec) + ".rsp");
        File.WriteAllLines(rspPath, cmdParams);
        Log.LogMessage(
            MessageImportance.Normal,
            $"ILRepack: running with response file {rspPath}:\n`{File.ReadAllText(rspPath)}`"
        );

        string command = ilrepack + " @" + rspPath;
        Log.LogMessage(MessageImportance.Normal, $"ILRepack: running as `{command}`");

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
