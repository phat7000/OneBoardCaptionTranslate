using LiveCaptionsTranslator.speech;
using Xunit.Abstractions;

namespace LiveCaptionsTranslator.Tests;

public sealed class ExternalAudioHardwareSmokeTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", "Hardware")]
    public void EnumeratesAndInitializesAnAvailableRecordingEndpoint()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("ONEBOARD_AUDIO_HARDWARE_SMOKE"),
                "1",
                StringComparison.Ordinal))
        {
            output.WriteLine("Set ONEBOARD_AUDIO_HARDWARE_SMOKE=1 to run the local recording-endpoint smoke test.");
            return;
        }

        ExternalAudioDeviceService service = new();
        IReadOnlyList<AudioInputDevice> devices = service.GetActiveCaptureDevices();
        output.WriteLine($"Active Windows recording endpoints: {devices.Count}");
        foreach (AudioInputDevice device in devices)
            output.WriteLine(device.DisplayName);

        if (devices.Count == 0)
            return;

        AudioInputDevice selected = devices[0];
        using ExternalAudioInputSource source = new(
            selected.DeviceId,
            selected.DisplayName,
            service);
        source.Start();
        Thread.Sleep(250);
        source.Stop();
        Assert.Equal(selected.DisplayName, source.DisplayName);
    }
}
