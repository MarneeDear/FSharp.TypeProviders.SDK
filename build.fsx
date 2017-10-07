// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#I "packages/FAKE/tools"

#r "packages/FAKE/tools/FakeLib.dll"

open System
open System.IO
open Fake
open Fake.DotNetCli

// --------------------------------------------------------------------------------------
// Information about the project to be used at NuGet
// --------------------------------------------------------------------------------------

let project = "FSharp.TypeProviders.SDK"
let authors = ["Tomas Petricek"; "Gustavo Guerra"; "Michael Newton"; "Don Syme" ]
let summary = "Helper code and examples for getting started with Type Providers"
let description = """
  The F# Type Provider SDK provides utilities for authoring type providers."""
let tags = "F# fsharp typeprovider"

let gitHome = "https://github.com/fsprojects"
let gitName = "FSharp.TypeProviders.SDK"

// Read release notes & version info from RELEASE_NOTES.md
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let release =
    File.ReadLines "RELEASE_NOTES.md"
    |> ReleaseNotesHelper.parseReleaseNotes

let pullRequest =
    match getBuildParamOrDefault "APPVEYOR_PULL_REQUEST_NUMBER" "" with
    | "" ->
        trace "Master build detected"
        None
    | a ->
        trace "Pull Request build detected"
        Some <| int a

let buildNumber =
    int (getBuildParamOrDefault "APPVEYOR_BUILD_VERSION" "0")

let version =
    match pullRequest with
    | None ->
        sprintf "%s.%d" release.AssemblyVersion buildNumber
    | Some num ->
        sprintf "%s-pull-%d-%05d" release.AssemblyVersion num buildNumber
let releaseNotes = release.Notes |> String.concat "\n"
let srcDir = "src"

let sources =
    [srcDir @@ "ProvidedTypes.fsi"
     srcDir @@ "ProvidedTypes.fs"
     srcDir @@ "AssemblyReader.fs"
     srcDir @@ "AssemblyReaderReflection.fs"
     srcDir @@ "ProvidedTypesContext.fs"
     srcDir @@ "ProvidedTypesTesting.fs" ]

Target "Clean" (fun _ ->
    CleanDirs []
)

Target "Restore" (fun _ ->
  // We don't use Fake.DotNetCli.Restore because of https://github.com/dotnet/sdk/issues/335
    Fake.MSBuildHelper.build  (fun pp -> { pp with Targets= ["Restore"]} ) "src/FSharp.TypeProviders.SDK.fsproj"
    Fake.MSBuildHelper.build  (fun pp -> { pp with Targets= ["Restore"]} ) "tests/FSharp.TypeProviders.SDK.Tests.fsproj"
)
Target "Compile" (fun _ ->
  // We don't use Fake.DotNetCli.Build because of https://github.com/dotnet/sdk/issues/335
    Fake.MSBuildHelper.build  (fun pp -> { pp with Targets= ["Build"]} ) "src/FSharp.TypeProviders.SDK.fsproj"
)


//#if EXAMPLES
//            { Name = "StaticProperty"; ProviderSourceFiles = ["StaticProperty.fsx"]; TestSourceFiles = ["StaticProperty.Tests.fsx"]}
//            { Name = "ErasedWithConstructor"; ProviderSourceFiles = ["ErasedWithConstructor.fsx"]; TestSourceFiles = ["ErasedWithConstructor.Tests.fsx"]}
//#endif

Target "RunTests" (fun _ ->
#if !MONO
  // We don't do this on Linux/OSX because of https://github.com/dotnet/sdk/issues/335
    Fake.DotNetCli.Test (fun pp -> { pp with Project="tests/FSharp.TypeProviders.SDK.Tests.fsproj" })
#endif
    ()
)

Target "NuGet" (fun _ ->
#if !MONO
  // We don't do this on Linux/OSX because of https://github.com/dotnet/sdk/issues/335
    Fake.DotNetCli.Pack (fun pp -> { pp with Project="src/FSharp.TypeProviders.SDK.fsproj" })
#endif
    ()
)

"Clean"
    ==> "NuGet"

"Restore"
    ==> "Compile"
//#if EXAMPLES
//    ==> "Examples"
//#endif
    ==> "RunTests"
    ==> "NuGet"

RunTargetOrDefault "RunTests"
