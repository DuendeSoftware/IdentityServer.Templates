using System;
using System.IO;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace build
{
    internal static class Program
    {
        private const string solution = "Duende.IdentityServer.Templates.sln";
        private const string packOutput = "./artifacts";
        private const string feedOutput = "./artifacts";
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

            Target(Targets.Copy, DependsOn(Targets.Build), () =>
            {
            //CreateDirectory("./feed/content");
            var directory = Directory.CreateDirectory(feedOutput).FullName;




    // copy the single csproj templates
 //   var files = GetFiles("./src/**/*.*");
//    CopyFiles(files, "./feed/content", true);

    // copy the UI files
   // files = GetFiles("./ui/**/*.*");
    //CopyFiles(files, "./feed/content/ui", true);
            });

            Target(Targets.Pack, DependsOn(Targets.Build, Targets.CleanPackOutput), () =>
            {
                var directory = Directory.CreateDirectory(packOutput).FullName;
                
                Run("dotnet", $"pack ./src/Storage/Duende.IdentityServer.Storage.csproj -c Release -o {directory} --no-build --nologo");
                Run("dotnet", $"pack ./src/IdentityServer/Duende.IdentityServer.csproj -c Release -o {directory} --no-build --nologo");
                
                Run("dotnet", $"pack ./src/EntityFramework.Storage/Duende.IdentityServer.EntityFramework.Storage.csproj -c Release -o {directory} --no-build --nologo");
                Run("dotnet", $"pack ./src/EntityFramework/Duende.IdentityServer.EntityFramework.csproj -c Release -o {directory} --no-build --nologo");
                
                Run("dotnet", $"pack ./src/AspNetIdentity/Duende.IdentityServer.AspNetIdentity.csproj -c Release -o {directory} --no-build --nologo");
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
                        );
            }
        }
    }
}