using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ILRepacking;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#nullable enable

namespace KageKirin.ILRepack.Lib.MSBuild.Task;

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
        Log.LogMessage(MessageImportance.High, "ILRepackLib: preparing inputs");

        if (OutputFile is null || string.IsNullOrWhiteSpace(OutputFile.ItemSpec))
        {
            Log.LogError("ILRepackLib: OutputFile must be set");
            return false;
        }

        if (
            InputAssemblies is null
            || InputAssemblies.Length == 0
            || string.IsNullOrWhiteSpace(InputAssemblies[0].ItemSpec)
        )
        {
            Log.LogError("ILRepackLib: InputAssemblies must at least contain one item");
            return false;
        }

        bool needTempOutputAssembly = InputAssemblies[0].ItemSpec == OutputFile.ItemSpec;
        string outputAssembly = needTempOutputAssembly
            ? Path.Combine(
                Path.GetDirectoryName(OutputFile.ItemSpec),
                Path.GetFileNameWithoutExtension(OutputFile.ItemSpec)
                    + ".Repack"
                    + Path.GetExtension(OutputFile.ItemSpec)
            )
            : OutputFile.ItemSpec;

        RepackOptions repackOptions = new()
        {
            AllowAllDuplicateTypes = AllowAllDuplicateTypes,
            AllowDuplicateResources = AllowDuplicateResources,
            AllowMultipleAssemblyLevelAttributes = AllowMultipleAssemblyLevelAttributes,
            AllowWildCards = AllowWildCards,
            AllowZeroPeKind = AllowZeroPeKind,
            Closed = Closed,
            CopyAttributes = CopyAttributes,
            DebugInfo = DebugInfo,
            DelaySign = DelaySign,
            ExcludeInternalizeSerializable = ExcludeInternalizeSerializable,
            FileAlignment = FileAlignment,
            Internalize = Internalize,
            KeepOtherVersionReferences = KeepOtherVersionReferences,
            LineIndexation = LineIndexation,
            LogVerbose = LogVerbose,
            MergeIlLinkerFiles = MergeIlLinkerFiles,
            NoRepackRes = NoRepackRes,
            Parallel = Parallel,
            PauseBeforeExit = PauseBeforeExit,
            PreserveTimestamp = PreserveTimestamp,
            PublicKeyTokens = PublicKeyTokens,
            RenameInternalized = RenameInternalized,
            SkipConfigMerge = SkipConfigMerge,
            StrongNameLost = StrongNameLost,
            UnionMerge = UnionMerge,
            XmlDocumentation = XmlDocumentation,
        };

        if (!string.IsNullOrWhiteSpace(TargetKind))
            repackOptions.TargetKind = (ILRepacking.ILRepack.Kind)
                Enum.Parse(typeof(ILRepacking.ILRepack.Kind), TargetKind);

        if (!string.IsNullOrWhiteSpace(TargetPlatformDirectory))
            repackOptions.TargetPlatformDirectory = TargetPlatformDirectory;

        if (!string.IsNullOrWhiteSpace(TargetPlatformVersion))
            repackOptions.TargetPlatformVersion = TargetPlatformVersion;

        if (!string.IsNullOrWhiteSpace(Version))
            repackOptions.Version = System.Version.Parse(Version);

        if (LogFile is not null && !string.IsNullOrWhiteSpace(LogFile.ItemSpec))
            repackOptions.LogFile = LogFile.ItemSpec;

        if (KeyFile is not null && !string.IsNullOrWhiteSpace(KeyFile.ItemSpec))
            repackOptions.KeyFile = KeyFile.ItemSpec;

        if (KeyContainer is not null && !string.IsNullOrWhiteSpace(KeyContainer.ItemSpec))
            repackOptions.KeyContainer = KeyContainer.ItemSpec;

        if (ImportAttributeAssemblies is not null && ImportAttributeAssemblies.Length > 0)
            repackOptions.AttributeFile = ImportAttributeAssemblies[0].ItemSpec; // !!!

        if (InternalizeAssemblies is not null && InternalizeAssemblies.Length > 0)
            repackOptions.InternalizeAssemblies =
            [
                .. InternalizeAssemblies.Select(f => f.ItemSpec),
            ];

        if (RepackDropAttributes is not null && RepackDropAttributes.Length > 0)
            repackOptions.RepackDropAttribute = string.Join(
                ";",
                RepackDropAttributes.Select(attr => attr.ItemSpec)
            );

        if (AllowedDuplicateTypes is not null)
        {
            foreach (var type in AllowedDuplicateTypes)
            {
                repackOptions.AllowDuplicateType(type.ItemSpec);
            }
        }

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

            repackOptions.ExcludeFile = excludeFile;
        }

        Log.LogMessage(
            MessageImportance.High,
            $"ILRepackLib: InputAssemblies (unfiltered): {string.Join("\n", InputAssemblies.Select(f => f.ItemSpec))}"
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
            $"ILRepackLib: InputAssemblies (filtered): {string.Join("\n", InputAssemblies.Distinct().Select(f => f.ItemSpec))}"
        );

        foreach (var inputAssembly in InputAssemblies.Select(f => f.ItemSpec))
        {
            if (string.IsNullOrEmpty(inputAssembly))
            {
                Log.LogError("ILRepackLib: InputAssemblies contains null/empty input assembly");
                continue;
            }

            if (File.Exists(inputAssembly))
            {
                Log.LogMessage(
                    MessageImportance.High,
                    $"ILRepackLib: InputAssemblies `{inputAssembly}` exists"
                );
            }
            else
            {
                Log.LogError($"ILRepackLib: InputAssemblies `{inputAssembly}` does not exist!");
            }
        }

        if (LibraryPaths is not null && LibraryPaths.Length > 0)
            repackOptions.SearchDirectories = LibraryPaths.Select(item => Path.GetFullPath(item.ItemSpec)).Distinct();
        else
            repackOptions.SearchDirectories = InputAssemblies
                .Select(item => Path.GetDirectoryName(Path.GetFullPath(item.ItemSpec)))
                .Distinct();

        repackOptions.OutputFile = outputAssembly;
        repackOptions.InputAssemblies = [.. InputAssemblies.Select(f => Path.GetFullPath(f.ItemSpec)).Distinct()];

        Log.LogMessage(
            MessageImportance.High,
            $"ILRepackLib: running `{repackOptions.ToCommandLine()}`"
        );
        Log.LogMessage(MessageImportance.High, $"ILRepackLib: repackOptions {repackOptions}");

        // create output dir
        string outputPath = Path.GetDirectoryName(Path.GetFullPath(OutputFile.ItemSpec));
        if (outputPath != null && !Directory.Exists(outputPath))
        {
            try
            {
                Directory.CreateDirectory(outputPath);
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
        }

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        var repacker = new ILRepacking.ILRepack(repackOptions);
        try
        {
            var task = System.Threading.Tasks.Task.Run(() => repacker.Repack());
            if (task.Wait(TimeSpan.FromSeconds(Timeout > 0 ? Timeout : 30)))
            {
                Success = true;
                Log.LogMessage(MessageImportance.High, $"ILRepackLib: success");
            }
            else
            {
                Log.LogError($"ILRepackLib: timeout");
                Success = false;
                throw new TimeoutException();
            }
        }
        catch (Exception exception)
        {
            Log.LogMessage(MessageImportance.High, $"ILRepackLib: error: {exception.Message}");
            Log.LogErrorFromException(exception, showStackTrace: true);
            Success = false;
        }

        if (needTempOutputAssembly)
        {
            File.Copy(outputAssembly, OutputFile.ItemSpec, overwrite: true);
            File.Delete(outputAssembly);
        }

        stopWatch.Stop();

        Log.LogMessage(
            MessageImportance.High,
            $"ILRepackLib: {(Success ? "succeeded" : "failed")} in {stopWatch.ElapsedMilliseconds} ms"
        );

        return Success;
    }

    public virtual void Dispose() { }
}
