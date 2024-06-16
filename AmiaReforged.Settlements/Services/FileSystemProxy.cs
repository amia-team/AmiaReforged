using System.IO.Abstractions;
using Anvil.Services;

namespace AmiaReforged.Settlements.Services;

/// <summary>
///  Allows safely injecting file system dependencies into services
/// </summary>
[ServiceBinding(typeof(IFileSystem))]
public class FileSystemProxy : FileSystem
{
    // Just a proxy class to allow for easier testing of services that rely on file system operations...
}