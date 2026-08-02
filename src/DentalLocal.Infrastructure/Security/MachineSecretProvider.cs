using System.Runtime.Versioning;
using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;

namespace DentalLocal.Infrastructure.Security;

[SupportedOSPlatform("windows")]
public sealed class MachineSecretProvider(IHostEnvironment env) : IMachineSecretProvider
{
    private static readonly object Lock = new();

    public byte[] GetOrCreateAutoBackupSecret()
    {
        var secretPath = GetSecretPath();

        lock (Lock)
        {
            if (File.Exists(secretPath))
            {
                var protectedBytes = File.ReadAllBytes(secretPath);
                return ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.LocalMachine);
            }

            var secret = RandomNumberGenerator.GetBytes(32);
            var protectedNew = ProtectedData.Protect(secret, optionalEntropy: null, DataProtectionScope.LocalMachine);

            Directory.CreateDirectory(Path.GetDirectoryName(secretPath)!);
            File.WriteAllBytes(secretPath, protectedNew);

            return secret;
        }
    }

    private string GetSecretPath()
    {
        var root = FindProjectRootDir(env.ContentRootPath);
        return Path.Combine(root, "Data", "Secrets", "backup.secret");
    }

    private static string FindProjectRootDir(string start)
    {
        var dir = start;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "DentalLocal.slnx")) || Directory.Exists(Path.Combine(dir, "src")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return start;
    }
}
