using System;
using System.IO;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Frosting;
using Cake.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public const string ProjectName = "Botanism";
    public string BuildConfiguration { get; set; }
    public string Version { get; }
    public string Name { get; }
    public bool SkipJsonValidation { get; set; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        BuildConfiguration = context.Argument("configuration", "Release");
        SkipJsonValidation = context.Argument("skipJsonValidation", false);

        var modInfo = context.DeserializeJsonFromFile<ModInfo>($"../{BuildContext.ProjectName}/modinfo.json");

        Version = modInfo.Version;
        Name = modInfo.ModID;
    }
}

[TaskName("ValidateJson")]
public sealed class ValidateJsonTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (context.SkipJsonValidation)
        {
            return;
        }

        var jsonFiles = context.GetFiles($"../{BuildContext.ProjectName}/assets/**/*.json");

        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file.FullPath);
                JToken.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Validation failed for JSON file: {file.FullPath}{Environment.NewLine}{ex.Message}", ex);
            }
        }
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(ValidateJsonTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetClean($"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj",
            new DotNetCleanSettings
            {
                Configuration = context.BuildConfiguration
            });

        context.DotNetPublish($"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj",
            new DotNetPublishSettings
            {
                Configuration = context.BuildConfiguration
            });
    }
}

[TaskName("Package")]
[IsDependentOn(typeof(BuildTask))]
public sealed class PackageTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.EnsureDirectoryExists("../Releases");
        context.CleanDirectory("../Releases");

        var releaseDirectory = $"../Releases/{context.Name}";

        context.EnsureDirectoryExists(releaseDirectory);

        context.CopyFiles(
            $"../{BuildContext.ProjectName}/bin/{context.BuildConfiguration}/Mods/mod/publish/*",
            releaseDirectory
        );

        // .deps.json, PDB and XML files are not needed for the mod to function and only increase the size of the final zip, so we can safely delete them.
        context.DeleteFiles($"{releaseDirectory}/*.deps.json");
        context.DeleteFiles($"{releaseDirectory}/*.pdb");
        context.DeleteFiles($"{releaseDirectory}/*.xml");

        context.CopyDirectory(
            $"../{BuildContext.ProjectName}/assets",
            $"{releaseDirectory}/assets"
        );

        context.CopyFile(
            $"../{BuildContext.ProjectName}/modinfo.json",
            $"{releaseDirectory}/modinfo.json"
        );

        if (context.FileExists($"../{BuildContext.ProjectName}/modicon.png"))
        {
            context.CopyFile(
                $"../{BuildContext.ProjectName}/modicon.png",
                $"{releaseDirectory}/modicon.png"
            );
        }

        context.Zip(
            releaseDirectory,
            $"../Releases/{context.Name}_{context.Version}.zip"
        );
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(PackageTask))]
public class DefaultTask : FrostingTask
{
}