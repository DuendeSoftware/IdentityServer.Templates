using System;
using System.IO;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace build
{
    internal static class Program
    {
        private const string NugetPackageVersion = "6.1.1";
        
        private const string packOutput = "./artifacts";
        private const string envVarMissing = " environment variable is missing. Aborting.";

        private static class Targets
        {
            public const string CleanPackOutput = "clean-pack-output";
            public const string Copy = "copy";
            public const string Build = "build";
            public const string Pack = "pack";
            public const string SignPackage = "sign-package";
        }

        internal static void Main(string[] args)
        {
            Target(Targets.Build, () =>
            {
                Run("dotnet", $"build -c Release --nologo");
            });

            Target(Targets.CleanPackOutput, () =>
            {
                if (Directory.Exists(packOutput))
                {
                    Directory.Delete(packOutput, true);
                }
            });

            Target(Targets.Copy, () =>
            {
                DirectoryCopy("./src", "./feed/content", true);
                DirectoryCopy("./ui", "./feed/content/ui", true);
            });

            Target(Targets.Pack, DependsOn(Targets.Copy, Targets.CleanPackOutput), () =>
            {
                var directory = Directory.CreateDirectory(packOutput).FullName;
                
                Run("./tools/nuget.exe", $"pack ./feed/Duende.IdentityServer.Templates.nuspec -OutputDirectory {directory} -Version {NugetPackageVersion}");
            });

            Target(Targets.SignPackage, DependsOn(Targets.Pack), () =>
            {
                SignNuGet();
            });

            Target("default", DependsOn(Targets.Build));

            Target("sign", DependsOn(Targets.SignPackage));

            RunTargetsAndExit(args, ex => ex is SimpleExec.NonZeroExitCodeException || ex.Message.EndsWith(envVarMissing));
        }

        private static void SignNuGet()
        {
            var signClientSecret = Environment.GetEnvironmentVariable("SignClientSecret");

            if (string.IsNullOrWhiteSpace(signClientSecret))
            {
                throw new Exception($"SignClientSecret{envVarMissing}");
            }

            foreach (var file in Directory.GetFiles(packOutput, "*.nupkg", SearchOption.AllDirectories))
            {
                Console.WriteLine($"  Signing {file}");

                Run("dotnet",
                        "NuGetKeyVaultSignTool " +
                        $"sign {file} " +
                        "--file-digest sha256 " +
                        "--timestamp-rfc3161 http://timestamp.digicert.com " +
                        "--azure-key-vault-url https://duendecodesigning.vault.azure.net/ " +
                        "--azure-key-vault-client-id 18e3de68-2556-4345-8076-a46fad79e474 " +
                        "--azure-key-vault-tenant-id ed3089f0-5401-4758-90eb-066124e2d907 " +
                        $"--azure-key-vault-client-secret {signClientSecret} " +
                        "--azure-key-vault-certificate CodeSigning"
                        ,noEcho: true);
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
    }
}
