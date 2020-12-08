#r "paket:
    nuget FSharp.Core ~> 4 prerelease
    nuget Fake.Core.Target ~> 5 prerelease
    nuget Fake.IO.FileSystem ~> 5 prerelease
    nuget BlackFox.CommandLine ~> 1 prerelease //"

#if !FAKE
#load ".fake/build.fsx/intellisense.fsx"
#endif

open BlackFox.CommandLine
open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators

Target.initEnvironment()

let workingDir = __SOURCE_DIRECTORY__ </> "tmp"

let buildTypes = [|"Debug"; "Release"|]
//let buildTypes = [|"Debug"|]

Target.create "Rebuild" ignore

Target.create "Clean" <| fun _ ->
    Shell.rm_rf workingDir

"Clean" ==> "Rebuild"

Target.create "EnsureWorkingDir" <| fun _ ->
    Directory.ensure workingDir

"Clean" ?=> "EnsureWorkingDir"

Target.create "Source" <| fun _ ->
    CmdLine.empty
    |> CmdLine.append "source"
    |> CmdLine.append __SOURCE_DIRECTORY__
    |> CmdLine.appendPrefix "--source-folder" (workingDir </> "source")
    |> CmdLine.toArray
    |> CreateProcess.fromRawCommand "conan"
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

"EnsureWorkingDir" ==> "Source"

Target.create "Install" ignore

let installTask (buildType: string) =
    let taskName = sprintf "Install.%s" buildType
    let installDir = workingDir </> (sprintf "Build.%s" buildType)
    Target.create taskName <| fun _ ->
        CmdLine.empty
        |> CmdLine.append "install"
        |> CmdLine.append __SOURCE_DIRECTORY__
        |> CmdLine.appendPrefix "--profile" "vs2019-preview"
        |> CmdLine.appendPrefix "--install-folder" installDir
        |> CmdLine.appendPrefix "--settings" (sprintf "build_type=%s" buildType)
        |> CmdLine.toArray
        |> CreateProcess.fromRawCommand "conan"
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore
    "Source" ?=> taskName |> ignore
    taskName ==> "Install" |> ignore

buildTypes |> Seq.iter installTask

Target.create "Build" ignore

let buildTask (buildType: string) =
    let taskName = sprintf "Build.%s" buildType
    let sourceDir = workingDir </> "source"
    let buildDir = workingDir </> taskName
    Target.create taskName <| fun _ ->
        CmdLine.empty
        |> CmdLine.append "build"
        |> CmdLine.append __SOURCE_DIRECTORY__
        |> CmdLine.appendPrefix "--source-folder" sourceDir
        |> CmdLine.appendPrefix "--build-folder" buildDir
        |> CmdLine.toArray
        |> CreateProcess.fromRawCommand "conan"
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore
    (sprintf "Install.%s" buildType) ==> taskName |> ignore
    taskName ==> "Build" |> ignore

buildTypes |> Seq.iter buildTask

Target.create "Package" ignore

let packageTask (buildType: string) =
    let taskName = sprintf "Package.%s" buildType
    let sourceDir = workingDir </> "source"
    let buildDir = workingDir </> (sprintf "Build.%s" buildType)
    let packageDir = workingDir </> (sprintf "Package.%s" buildType)
    Target.create taskName <| fun _ ->
        CmdLine.empty
        |> CmdLine.append "package"
        |> CmdLine.append __SOURCE_DIRECTORY__
        |> CmdLine.appendPrefix "--source-folder" sourceDir
        |> CmdLine.appendPrefix "--build-folder" buildDir
        |> CmdLine.appendPrefix "--package-folder" packageDir
        |> CmdLine.toArray
        |> CreateProcess.fromRawCommand "conan"
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore
    (sprintf "Build.%s" buildType) ==> taskName |> ignore
    taskName ==> "Package" |> ignore

buildTypes |> Seq.iter packageTask

Target.create "Export" ignore

let exportTask (buildType: string) =
    let taskName = sprintf "Export.%s" buildType
    let sourceDir = workingDir </> "source"
    let buildDir = workingDir </> (sprintf "Build.%s" buildType)
    let packageDir = workingDir </> (sprintf "Package.%s" buildType)
    Target.create taskName <| fun _ ->
        CmdLine.empty
        |> CmdLine.append "export-pkg"
        |> CmdLine.append "--force"
        |> CmdLine.append __SOURCE_DIRECTORY__
        |> CmdLine.append "objectx/testing"
        // |> CmdLine.appendPrefix "--source-folder" sourceDir
        // |> CmdLine.appendPrefix "--build-folder" buildDir
        |> CmdLine.appendPrefix "--package-folder" packageDir
        |> CmdLine.appendPrefix "--profile" "vs2019-preview"
        |> CmdLine.appendPrefix "--settings" (sprintf "build_type=%s" buildType)
        |> CmdLine.toArray
        |> CreateProcess.fromRawCommand "conan"
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore
    (sprintf "Package.%s" buildType) ==> taskName |> ignore
    taskName ==> "Export" |> ignore


buildTypes |> Seq.iter exportTask

Target.create "Test" ignore

let testTask (buildType: string) =
    let taskName = sprintf "Test.%s" buildType
    Target.create taskName <| fun _ ->
        CmdLine.empty
        |> CmdLine.append "test"
        |> CmdLine.append "test_package"
        |> CmdLine.append "spdlog/20201126@objectx/testing"
        |> CmdLine.appendPrefix "--settings" (sprintf "build_type=%s" buildType)
        |> CmdLine.toArray
        |> CreateProcess.fromRawCommand "conan"
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore
    (sprintf "Export.%s" buildType) ==> taskName |> ignore
    taskName ==> "Test" |> ignore

buildTypes |> Seq.iter testTask

Target.create "Create" ignore

let createTask (buildType: string) =
    let taskName = sprintf "Create.%s" buildType
    Target.create taskName <| fun _ ->
        CmdLine.empty
        |> CmdLine.append "create"
        |> CmdLine.append __SOURCE_DIRECTORY__
        |> CmdLine.append "objectx/testing"
        |> CmdLine.appendPrefix "--settings" (sprintf "build_type=%s" buildType)
        |> CmdLine.toArray
        |> CreateProcess.fromRawCommand "conan"
        |> CreateProcess.ensureExitCode
        |> Proc.run
        |> ignore
    taskName ==> "Create" |> ignore
// "Test" ==> "Create" |> ignore

buildTypes |> Seq.iter createTask
"Source" ==> "Rebuild"
"Create" ==> "Rebuild"

Target.runOrDefaultWithArguments "Rebuild"
