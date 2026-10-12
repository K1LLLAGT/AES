using System.Runtime.CompilerServices;
using System.Threading.Channels;
using AES.Contracts.Models;
using CliWrap;
using CliWrap.Buffered;

namespace AES.Modules.DeviceBridge;

public sealed class AdbController
{
    private static string AdbExe =>
        Path.Combine(AppContext.BaseDirectory, "tools", "platform-tools", "adb.exe");

    private static string FastbootExe =>
        Path.Combine(AppContext.BaseDirectory, "tools", "platform-tools", "fastboot.exe");

    public bool IsAvailable => File.Exists(AdbExe);

    public async Task<List<Device>> GetDevicesAsync(CancellationToken ct = default)
    {
        if (!IsAvailable) return new();

        var result = await Cli.Wrap(AdbExe)
            .WithArguments("devices -l")
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(ct);

        return ParseDevices(result.StandardOutput);
    }

    public async Task<string> ShellAsync(string serial, string command,
                                          CancellationToken ct = default)
    {
        var result = await Cli.Wrap(AdbExe)
            .WithArguments(new[] { "-s", serial, "shell", command })
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(ct);

        return result.StandardOutput + result.StandardError;
    }

    public async Task<string> GetPropertiesAsync(string serial, CancellationToken ct = default)
    {
        var result = await Cli.Wrap(AdbExe)
            .WithArguments(new[] { "-s", serial, "shell", "getprop" })
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(ct);

        return result.StandardOutput;
    }

    public async Task<bool> IsRootedAsync(string serial, CancellationToken ct = default)
    {
        var result = await ShellAsync(serial, "id", ct);
        return result.Contains("uid=0");
    }

    public async IAsyncEnumerable<string> StreamLogcatAsync(
        string serial,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<string>(
            new UnboundedChannelOptions { SingleWriter = true });

        _ = Task.Run(async () =>
        {
            try
            {
                await Cli.Wrap(AdbExe)
                    .WithArguments(new[] { "-s", serial, "logcat", "-v", "threadtime" })
                    .WithStandardOutputPipe(PipeTarget.ToDelegate(
                        line => channel.Writer.TryWrite(line)))
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync(ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
            finally
            {
                channel.Writer.TryComplete();
            }
        }, CancellationToken.None);

        await foreach (var line in channel.Reader.ReadAllAsync(ct))
            yield return line;
    }

    public async Task<string> FastbootAsync(string serial, string command,
                                             CancellationToken ct = default)
    {
        if (!File.Exists(FastbootExe))
            return "fastboot.exe not found in tools/platform-tools/";

        var baseArgs = string.IsNullOrEmpty(serial)
            ? Array.Empty<string>()
            : new[] { "-s", serial };

        var args = baseArgs
            .Concat(command.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToArray();

        var result = await Cli.Wrap(FastbootExe)
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(ct);

        return result.StandardOutput + result.StandardError;
    }

    private static List<Device> ParseDevices(string output)
    {
        var devices = new List<Device>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            var serial   = parts[0].Trim();
            var stateStr = parts[1].Trim();

            var state = stateStr switch
            {
                "device"       => DeviceState.Online,
                "unauthorized" => DeviceState.Unauthorized,
                "fastboot"     => DeviceState.Fastboot,
                _              => DeviceState.Offline
            };

            string? model = line
                .Split(' ')
                .FirstOrDefault(p => p.StartsWith("model:"))?
                .Replace("model:", "")
                .Trim();

            devices.Add(new Device(serial, model, null, null, state, false, false));
        }

        return devices;
    }
}