using MySerialKey.GiqhDev.Installation;
using MySerialKey.GiqhDev.Storage;
#if NET6_0_OR_GREATER
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("windows")]
#endif

if (args.Length != 3)
{
    Console.Error.WriteLine("Expected: <storage-root> <product-code> <gate-file>.");
    return 2;
}

string storageRoot = args[0];
string productCode = args[1];
string gateFile = args[2];
DateTime deadline = DateTime.UtcNow.AddSeconds(30);

while (!File.Exists(gateFile))
{
    if (DateTime.UtcNow >= deadline)
    {
        Console.Error.WriteLine("Timed out waiting for the test gate.");
        return 3;
    }

    Thread.Sleep(10);
}

var store = new FileProtectedDataStore(storageRoot, new DpapiDataProtector());
var provider = new PersistentInstallationIdProvider(store);
Console.WriteLine(provider.GetOrCreate(productCode));
return 0;
