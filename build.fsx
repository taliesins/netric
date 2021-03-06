﻿#r @"src/packages/FAKE/tools/fakelib.dll"

open Fake
open System
let buildDir = "./build/"
let testDir = "./test/"

let appProjects = 
    !! "src/**/Netric.Agent.Service.csproj"
    ++ "src/**/Netric.Configuration.Console.csproj"
    ++ "src/**/Netric.Intercept.csproj"
let testProjects = !! "src/**/*.Tests.csproj"

let installerProject = !!"src/Netric.Installer/Netric.Installer.wixproj"

Target "Clean" (fun _ ->
    CleanDir buildDir
    CleanDir testDir
)

Target "BuildApp" (fun _ ->    
    appProjects      
      |> MSBuildRelease buildDir "Build"
      |> Log "AppBuild-Output: "
    !! "src/Netric.Profiler*.dll"
      |> CopyFiles buildDir
)

Target "RestorePackages" (fun _ -> 
     "./src/Netric.sln"
     |> RestoreMSSolutionPackages (fun p ->
         { p with Retries = 4
                  OutputPath = "src/packages"
             })
 )

Target "BuildTest" (fun _ ->
    MSBuildDebug testDir "Build" testProjects
        |> Log "TestBuild-Output: "
)

Target "BuildInstaller" (fun _ ->    
    let buildOnPlatform platform = 
        MSBuildReleaseExt buildDir [("Platform",platform);("PackagesPath",Environment.CurrentDirectory @@ "/src/packages/")] "Build" installerProject 
            |> Log "InstallerBuild-Output: "
        ()
    buildOnPlatform "x86"
    buildOnPlatform "x64"    
)

open Fake.Testing
Target "RunTests" (fun _ ->  
    !! (testDir + "/*.Tests.dll")
        |> xUnit2 (fun p -> 
            {p with 
               ToolPath = "src/packages/xunit.runner.console.2.1.0/tools/xunit.console.exe"
               HtmlOutputPath = Some(testDir + "result.html")
               })
)

Target "Default" (fun _ -> 
    trace "Staring full build"
)

"Clean" 
    ==> "RestorePackages"    
    ==> "BuildApp"      
    ==> "BuildTest"
    ==> "RunTests"
    ==> "BuildInstaller"
    ==> "Default"

RunTargetOrDefault "Default"