using System.CommandLine;
using System.CommandLine.Completions;
using Spectre.Console;

namespace VmChamp;

public class SshCommand : Command
{
  private readonly AppConfig _appConfig;

  public SshCommand(AppConfig appConfig) : base("ssh", "connect to vm via ssh")
  {
    _appConfig = appConfig;

    Argument<string> nameArgument = new("name",
      () => appConfig.DefaultVmName,
      "Name of the new VM");

    this.AddArgument(nameArgument);

    nameArgument.AddCompletions((ctx) =>
      Helper.GetAllVmInDirectory(_appConfig.DataDir)
        .Select(vmName => new CompletionItem(vmName ?? "")));

    nameArgument.AddCompletions((ctx) =>
      Helper.GetAllVmInDirectory(_appConfig.DataDir)
        .Select(vmName => new CompletionItem(vmName ?? "")));

    this.SetHandler((vmName) =>
    {
      var user = _appConfig.DefaultUser;

      var vmDir = Path.Combine(_appConfig.DataDir, vmName);
      var userFromIso = Helper.GetUserFromIso(vmDir);

      if (userFromIso != "N/A" && userFromIso != _appConfig.DefaultUser)
      {
          user = userFromIso;
      }

      AnsiConsole.MarkupLine($"✈️ Connect to: {vmName}. USER: {user}");

      using var libvirtConnection = LibvirtConnection.CreateForSession();

      var vmId = Interop.virDomainLookupByName(libvirtConnection.NativePtr, vmName);
      var vmIp = Interop.GetFirstIpById(vmId);

      if (vmIp == null)
      {
        AnsiConsole.MarkupLine($"No network found for VM: {vmName}");
        return;
      }

      Helper.ConnectViaSsh(user, vmIp)?.WaitForExit();
    }, nameArgument);
  }
}