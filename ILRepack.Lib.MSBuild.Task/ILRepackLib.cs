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

    public virtual Microsoft.Build.Framework.ITaskItem[] InputAssemblies { get; set; } = [];

    public virtual Microsoft.Build.Framework.ITaskItem[] LibraryPaths { get; set; } = [];

    public virtual Microsoft.Build.Framework.ITaskItem[] InternalizeExclude { get; set; } = [];

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
            Parallel = Parallel,
            DebugInfo = DebugInfo,
            LogVerbose = Verbose,
            Internalize = Internalize,
            RenameInternalized = RenameInternalized,
            AllowWildCards = Wildcards,
            DelaySign = DelaySign,
            ExcludeInternalizeSerializable = ExcludeInternalizeSerializable,
            UnionMerge = Union,
            AllowAllDuplicateTypes = AllowDup,
            AllowDuplicateResources = AllowDuplicateResources,
            NoRepackRes = NoRepackRes,
            CopyAttributes = CopyAttrs,
            AllowMultipleAssemblyLevelAttributes = AllowMultiple,
            KeepOtherVersionReferences = KeepOtherVersionReferences,
            PreserveTimestamp = PreserveTimestamp,
            SkipConfigMerge = SkipConfig,
            MergeIlLinkerFiles = ILLink,
            XmlDocumentation = XmlDocs,
            AllowZeroPeKind = ZeroPEKind,
        };

        if (!string.IsNullOrWhiteSpace(TargetKind))
            repackOptions.TargetKind = (ILRepacking.ILRepack.Kind)
                Enum.Parse(typeof(ILRepacking.ILRepack.Kind), TargetKind);

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
                writer.WriteLine(asm.ItemSpec);

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
            repackOptions.SearchDirectories = LibraryPaths.Select(item => item.ItemSpec).Distinct();
        else
            repackOptions.SearchDirectories = InputAssemblies
                .Select(item => Path.GetDirectoryName(item.ItemSpec))
                .Distinct();

        repackOptions.OutputFile = outputAssembly;
        repackOptions.InputAssemblies = [.. InputAssemblies.Select(f => f.ItemSpec).Distinct()];

        Log.LogMessage(
            MessageImportance.High,
            $"ILRepackLib: running `{repackOptions.ToCommandLine()}`"
        );
        Log.LogMessage(MessageImportance.High, $"ILRepackLib: repackOptions {repackOptions}");

        // create output dir
        string outputPath = Path.GetDirectoryName(OutputFile.ItemSpec);
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
