// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Ante.Fixture.Proxy;

public static class AttestationKeys
{
    public static void Create()
    {
        const string privatePath = "/run/fixture/keys/attestation.pem";
        const string publicPath = "/run/cratis/secrets/Ante__Invitations__Exchange__Attestation__PublicKeys__0__PublicKeyPem";
        Directory.CreateDirectory(Path.GetDirectoryName(privatePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(publicPath)!);
        using var key = RSA.Create(2048);
        if (File.Exists(privatePath))
        {
            key.ImportFromPem(File.ReadAllText(privatePath));
        }
        else
        {
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            using var stream = new FileStream(privatePath, options);
            using var writer = new StreamWriter(stream);
            writer.Write(key.ExportRSAPrivateKeyPem());
        }

        File.WriteAllText(publicPath, key.ExportSubjectPublicKeyInfoPem());
        Console.WriteLine("Fixture attestation key ready (public key mounted into Ante).");
    }
}
