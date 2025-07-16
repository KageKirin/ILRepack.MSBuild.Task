using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace KageKirin.ILRepack.Tool.MSBuild.Task;

public class ILRepack : Microsoft.Build.Utilities.Task
{
    private bool _Parallel;

    public virtual bool Parallel
    {
        get { return _Parallel; }
        set { _Parallel = value; }
    }

    private bool _DebugInfo;

    public virtual bool DebugInfo
    {
        get { return _DebugInfo; }
        set { _DebugInfo = value; }
    }

    private bool _Verbose;

    public virtual bool Verbose
    {
        get { return _Verbose; }
        set { _Verbose = value; }
    }

    private bool _Internalize;

    public virtual bool Internalize
    {
        get { return _Internalize; }
        set { _Internalize = value; }
    }

    private bool _RenameInternalized;

    public virtual bool RenameInternalized
    {
        get { return _RenameInternalized; }
        set { _RenameInternalized = value; }
    }

    private string _TargetKind = string.Empty;

    public virtual string TargetKind
    {
        get { return _TargetKind; }
        set { _TargetKind = value; }
    }

    private bool _Wildcards;

    public virtual bool Wildcards
    {
        get { return _Wildcards; }
        set { _Wildcards = value; }
    }

    private bool _DelaySign;

    public virtual bool DelaySign
    {
        get { return _DelaySign; }
        set { _DelaySign = value; }
    }

    private bool _ExcludeInternalizeSerializable;

    public virtual bool ExcludeInternalizeSerializable
    {
        get { return _ExcludeInternalizeSerializable; }
        set { _ExcludeInternalizeSerializable = value; }
    }

    private bool _Union;

    public virtual bool Union
    {
        get { return _Union; }
        set { _Union = value; }
    }

    private bool _AllowDup;

    public virtual bool AllowDup
    {
        get { return _AllowDup; }
        set { _AllowDup = value; }
    }

    private bool _AllowDuplicateResources;

    public virtual bool AllowDuplicateResources
    {
        get { return _AllowDuplicateResources; }
        set { _AllowDuplicateResources = value; }
    }

    private bool _NoRepackRes;

    public virtual bool NoRepackRes
    {
        get { return _NoRepackRes; }
        set { _NoRepackRes = value; }
    }

    private bool _CopyAttrs;

    public virtual bool CopyAttrs
    {
        get { return _CopyAttrs; }
        set { _CopyAttrs = value; }
    }

    private bool _AllowMultiple;

    public virtual bool AllowMultiple
    {
        get { return _AllowMultiple; }
        set { _AllowMultiple = value; }
    }

    private bool _KeepOtherVersionReferences;

    public virtual bool KeepOtherVersionReferences
    {
        get { return _KeepOtherVersionReferences; }
        set { _KeepOtherVersionReferences = value; }
    }

    private bool _PreserveTimestamp;

    public virtual bool PreserveTimestamp
    {
        get { return _PreserveTimestamp; }
        set { _PreserveTimestamp = value; }
    }

    private bool _SkipConfig;

    public virtual bool SkipConfig
    {
        get { return _SkipConfig; }
        set { _SkipConfig = value; }
    }

    private bool _ILLink;

    public virtual bool ILLink
    {
        get { return _ILLink; }
        set { _ILLink = value; }
    }

    private bool _XmlDocs;

    public virtual bool XmlDocs
    {
        get { return _XmlDocs; }
        set { _XmlDocs = value; }
    }

    private bool _ZeroPEKind;

    public virtual bool ZeroPEKind
    {
        get { return _ZeroPEKind; }
        set { _ZeroPEKind = value; }
    }

    private string _Version = string.Empty;

    public virtual string Version
    {
        get { return _Version; }
        set { _Version = value; }
    }

    private Microsoft.Build.Framework.ITaskItem[] _InputAssemblies = [];

    public virtual Microsoft.Build.Framework.ITaskItem[] InputAssemblies
    {
        get { return _InputAssemblies; }
        set { _InputAssemblies = value; }
    }

    private Microsoft.Build.Framework.ITaskItem[] _LibraryPaths = [];

    public virtual Microsoft.Build.Framework.ITaskItem[] LibraryPaths
    {
        get { return _LibraryPaths; }
        set { _LibraryPaths = value; }
    }

    private Microsoft.Build.Framework.ITaskItem[] _InternalizeExclude = [];

    public virtual Microsoft.Build.Framework.ITaskItem[] InternalizeExclude
    {
        get { return _InternalizeExclude; }
        set { _InternalizeExclude = value; }
    }

    private Microsoft.Build.Framework.ITaskItem _OutputFile;

    public virtual Microsoft.Build.Framework.ITaskItem OutputFile
    {
        get { return _OutputFile; }
        set { _OutputFile = value; }
    }

    private Microsoft.Build.Framework.ITaskItem _LogFile;

    public virtual Microsoft.Build.Framework.ITaskItem LogFile
    {
        get { return _LogFile; }
        set { _LogFile = value; }
    }

    private Microsoft.Build.Framework.ITaskItem[] _FilterAssemblies = [];

    public virtual Microsoft.Build.Framework.ITaskItem[] FilterAssemblies
    {
        get { return _FilterAssemblies; }
        set { _FilterAssemblies = value; }
    }

    private Microsoft.Build.Framework.ITaskItem[] _ImportAttributeAssemblies;

    public virtual Microsoft.Build.Framework.ITaskItem[] ImportAttributeAssemblies
    {
        get { return _ImportAttributeAssemblies; }
        set { _ImportAttributeAssemblies = value; }
    }

    private Microsoft.Build.Framework.ITaskItem[] _InternalizeAssemblies;

    public virtual Microsoft.Build.Framework.ITaskItem[] InternalizeAssemblies
    {
        get { return _InternalizeAssemblies; }
        set { _InternalizeAssemblies = value; }
    }

    private Microsoft.Build.Framework.ITaskItem[] _RepackDropAttributes;

    public virtual Microsoft.Build.Framework.ITaskItem[] RepackDropAttributes
    {
        get { return _RepackDropAttributes; }
        set { _RepackDropAttributes = value; }
    }

    private Microsoft.Build.Framework.ITaskItem[] _AllowedDuplicateTypes;

    public virtual Microsoft.Build.Framework.ITaskItem[] AllowedDuplicateTypes
    {
        get { return _AllowedDuplicateTypes; }
        set { _AllowedDuplicateTypes = value; }
    }

    private Microsoft.Build.Framework.ITaskItem _KeyFile;

    public virtual Microsoft.Build.Framework.ITaskItem KeyFile
    {
        get { return _KeyFile; }
        set { _KeyFile = value; }
    }

    private Microsoft.Build.Framework.ITaskItem _KeyContainer;

    public virtual Microsoft.Build.Framework.ITaskItem KeyContainer
    {
        get { return _KeyContainer; }
        set { _KeyContainer = value; }
    }

    private int _Timeout;

    public virtual int Timeout
    {
        get { return _Timeout; }
        set { _Timeout = value; }
    }

    private bool _Success = true;

    public virtual bool Success
    {
        get { return _Success; }
        set { _Success = value; }
    }

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

        // TODO: handle
        //InternalizeExclude

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
                .ToArray();
        }
        Log.LogMessage(
            MessageImportance.High,
            $"ILRepack: InputAssemblies (filtered): {string.Join("\n", InputAssemblies.Select(f => f.ItemSpec))}"
        );

        if (LibraryPaths is not null && LibraryPaths.Length > 0)
            cmdParams.AddRange(
                LibraryPaths.Select(item => item.ItemSpec).Distinct().Select(l => $"/lib:\"{l}\"")
            );
        else
            cmdParams.AddRange(
                InputAssemblies
                    .Select(item => Path.GetDirectoryName(item.ItemSpec))
                    .Distinct()
                    .Select(l => $"/lib:\"{l}\"")
            );

        // must come last
        cmdParams.AddRange(InputAssemblies.Distinct().Select(item => $"\"{item.ItemSpec}\""));

        string command = "ilrepack " + string.Join(" ", cmdParams);
        Log.LogMessage(MessageImportance.High, $"ILRepack: running `{command}`");

        Process process = new Process();
        process.StartInfo = new ProcessStartInfo()
        {
            FileName = "dotnet",
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
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
}
