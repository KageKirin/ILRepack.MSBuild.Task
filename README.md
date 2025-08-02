<img
  src="Icon.png"
  alt="ILRepack.MSBuild.Task icon"
  height="200"
  width="200"
  align="right" />

# ILRepack.MSBuild.Task

`KageKirin.ILRepack.MSBuild.Task` contains tasks for MSBuild to run `ILRepack` over a freshly compiled assembly,
in order to infuse it with select IL code of its dependencies.

## 📦 Package information

### `KageKirin.ILRepack.Tool.MSBuild.Task`

This is the tool variant. It requires having ILRepack installed.

### `KageKirin.ILRepack.Lib.MSBuild.Task`

This is the lib variant. It's standalone and ILRepack.Lib has been merged into it.

### 🔧 Installation

```bash
dotnet add package KageKirin.ILRepack.Tool.MSBuild.Task ## tool version
dotnet add package KageKirin.ILRepack.Lib.MSBuild.Task  ## lib version
```

### 📦 Package reference

```xml
<Project>
  <ItemGroup>
    <!-- either -->
    <PackageReference Include="KageKirin.ILRepack.Tool.MSBuild.Task" Version="0.3.0" />
    <!-- or -->
    <PackageReference Include="KageKirin.ILRepack.Lib.MSBuild.Task" Version="0.3.0" />
  </ItemGroup>
</Project>
```

## ⚙️ Configuration

`ILRepack.Tool.MSBuild.Task` and `ILRepack.Lib.MSBuild.Task` can be used interchangeably as both use the same properties and items.
Therefore it is **not recommended** to use _both_ in the same project.

The task can be used in 2 manners:
- using the default target that relies on configuration only
- use=ing a custom target that calls the `ILRepack` task.

### Configuration only default target

Global configuration goes into a solution-wide `Directory.Build.props` as usual,
per-project configuration can go into each `.csproj` project file, or preferrably
into `ILRepack.Configuration.props`.

I do recommend using a per-project `ILRepack.Configuration.props` residing next to the project.

### Custom target using the `ILRepack` task

Custom targets ought to be handled per-project and go into a `ILRepack.targets` file residing next to the project.

### Options and description

The `ILRepack` exposes a number of parameters that have their equivalent in the form of `ILRepackXXX` properties or items
(which then get internally forward to the aforementioned task).
Most of those parameters/properties correspond directly to the command line arguments that `ILRepack.exe` can take, respectively,
to the fields of `ILRepacking.RepackOptions` used internally by the `ILRepack.dll` library.

#### Properties

For the record, properties go into a (read one or more) `<PropertyGroup>` container.

```xml
<Project>
  <PropertyGroup>
    <!-- properties go here -->
  </PropertyGroup>
</Project>
```

The properties are as follows, in the form of `<ILRepackProperty>` `ILRepack task parameter`
and the actual `ILRepack.exe` argument this corresponds to:

##### Boolean flags

- `<ILRepackParallel>` `Parallel` `/parallel`: use as many CPUs as possible to merge the assemblies
- `<ILRepackDebugInfo>` `DebugInfo` `/ndebug` (when omitted): enables symbol file generation. explcitly set to `false` to disable default behaviour.
- `<ILRepackLogVerbose>` `LogVerbose` `/verbose`: more detailed logging
- `<ILRepackInternalize>` `Internalize` `/internalize`: make all types except in the first assembly 'internal'. Types in the transitive closure of public API remain public.
- `<ILRepackRenameInternalized>` `RenameInternalized` `/renameinternalized`: rename each internalized type to a new unique name
- `<ILRepackAllowWildCards>` `AllowWildCards` `/wildcards`: allows (and resolves) file wildcards (e.g. *.dll) in input assemblies
- `<ILRepackDelaySign>` `DelaySign` `/delaysign`: set the key, but don't sign the assembly
- `<ILRepackExcludeInternalizeSerializable>` `ExcludeInternalizeSerializable` `/excludeinternalizeserializable`: do not internalize types marked as Serializable
- `<ILRepackUnionMerge>` `UnionMerge` `/union`: merges types with identical names into one
- `<ILRepackAllowAllDuplicateTypes>` `AllowAllDuplicateTypes` `/allowdup` if no other `/allowdup` arguments specified, allow all duplicate types. (see `<ILRepackAllowDuplicateTypes>` below for more information)
- `<ILRepackAllowDuplicateResources>` `AllowDuplicateResources` `/allowduplicateresources`:  allows to duplicate resources in output assembly (by default they're ignored)
- `<ILRepackNoRepackRes>` `NoRepackRes` `/noRepackRes`: do not add the resource 'ILRepack.List' with all merged assembly names
- `<ILRepackCopyAttributes>` `CopyAttributes` `/copyattrs`: copy assembly attributes (by default only the primary assembly attributes are copied)
- `<ILRepackAllowMultipleAssemblyLevelAttributes>` `AllowMultipleAssemblyLevelAttributes` `/allowMultiple`: when copyattrs is specified, allows multiple attributes (if type allows)
- `<ILRepackKeepOtherVersionReferences>` `KeepOtherVersionReferences` `/keepotherversionreferences`: take reference assembly version into account when removing references
- `<ILRepackPreserveTimestamp>` `PreserveTimestamp` `/preservetimestamp`: preserve original file PE timestamp
- `<ILRepackSkipConfigMerge>` `SkipConfigMerge` `/skipconfig`: skips merging config files
- `<ILRepackMergeIlLinkerFiles>` `MergeIlLinkerFiles` `/illink`: merge IL Linker files
- `<ILRepackXmlDocumentation>` `XmlDocumentation` `/xmldocs`: merges XML documentation as well
- `<ILRepackAllowZeroPeKind>` `AllowZeroPeKind` `/zeropekind`: allows assemblies with Zero PeKind (but obviously only IL will get merged)

- `<ILRepackLineIndexation>` `LineIndexation` `/index`: stores file:line debug information as type/method attributes (requires PDB)
- `<ILRepackPauseBeforeExit>` `PauseBeforeExit` `/pause`: pause execution once completed (good for debugging)

- `<ILRepackClosed>` `Closed` `/closed`: NOT IMPLEMENTED

##### Single string values

- `<ILRepackVersion>` `Version` `/ver:M.X.Y.Z`: target assembly version
- `<ILRepackTargetKind>` `TargetKind` `/target:kind`       target assembly kind [library|exe|winexe], default is same as primary assembly
- `<ILRepackTargetPlatformDirectory>` `TargetPlatformDirectory`
  `<ILRepackTargetPlatformVersion>` `TargetPlatformVersion`
  `/targetplatform:P`  specify target platform (v1, v1.1, v2, v4 supported)

##### Integer values

- `<ILRepackTimeout>` `Timeout`: timeout to end task in seconds. This is a special value that allows to terminate the `ILRepack` task after the specified time to avoid endless loops eating up precious CI time.

- `<ILRepackFileAlignment>` `FileAlignment` `/align`: NOT IMPLEMENTED

#### Items

For the record, items go into a (read one or more) `<ItemGroup>` container.

```xml
<Project>
  <ItemGroup>
    <!-- items go here -->
  </ItemGroup>
</Project>
```

The items are as follows, in the form of `<ILRepackItem>` `ILRepack task parameter`
and the actual `ILRepack.exe` argument this corresponds to:

##### Required item inputs

- `<ILRepackOutputFile>` `OutputFile` `/out:<path>` target assembly path, symbol/config/doc files will be written here as well
- `<ILRepackInputAssemblies>` `InputAssemblies`: paths to input assemblies in the following order:
  - **primary assembly**: gives the name, version to the merged one
  - **other assemblies**: other assemblies to merge with the primary one

##### Further item inputs

- `<ILRepackLogFile>` `LogFile` `/log:<logfile>`: enable logging to the given file (default is disabled)

- `<ILRepackLibraryPaths>` `LibraryPaths`　`/lib:<path>`: path(s) to search directories to resolve referenced assemblies (can be specified multiple times).
  If you get 'unable to resolve assembly' errors specify a path to a directory where the assembly can be found.

- `<ILRepackInternalizeExclude>` `InternalizeExclude`: each item is either a regex or a full type name not to internalize,
  or an assembly name not to internalize (.dll extension optional)
  Internally, each item is written line-by-line into a `ILRepack.not` file that gets passed as
  `/internalize:<exclude_file>`

- `<ILRepackInternalizeAssemblies>` `InternalizeAssemblies` `/internalizeassembly:<path>`: Internalize a specific assembly name (no extension).
  May be specified more than once (one per assembly to internalize).
  If specified, no need to also specify /internalize.

- `<ILRepackKeyFile>` `KeyFile` `/keyfile:<path>`: keyfile to sign the output assembly
- `<ILRepackKeyContainer>` `KeyContainer` `/keycontainer:<c>`: key container

- `<ILRepackAllowedDuplicateTypes>` `AllowedDuplicateTypes` `/allowdup:Type`: keep duplicates of the specified type, may be specified more than once

- `<ILRepackRepackDropAttributes>` `DropAttributes` `/repackdrop:RepackDropAttribute`: allows dropping members denoted by this attribute name when merging

- `<ILRepackImportAttributeAssemblies>` `ImportAttributeAssemblies` `/attr:<path>`: take assembly attributes from the given assembly file

- `<ILRepackFilterAssemblies>` `FilterAssemblies`: optional list of assemblies (partial names or regex) to keep among all the input assemblies. Non-matching ones are discarded from the inputs.
  The reason for this is that filtering the sometimes many input assemblies using only MSBuild functions is rather complex, whereas this simple list simplifies the process extremely.

## 🤝 Collaborate with My Project

Please refer to [COLLABORATION.md](COLLABORATION.md).
