using System.Text.Json;

using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.speech;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace LiveCaptionsTranslator.Tests;

public class AudioSourceTests
{
    [Fact]
    public void NewAndLegacySettings_DefaultToSystemAudio()
    {
        Assert.Equal(AudioSourceType.SystemAudio, new Setting().AudioSource);

        Setting? legacy = JsonSerializer.Deserialize<Setting>("{}");
        Assert.NotNull(legacy);
        Assert.Equal(AudioSourceType.SystemAudio, legacy.AudioSource);
    }

    [Theory]
    [InlineData(AudioSourceType.SystemAudio, "\"SystemAudio\"")]
    [InlineData(AudioSourceType.Microphone, "\"Microphone\"")]
    [InlineData(AudioSourceType.ExternalAudioInput, "\"ExternalAudioInput\"")]
    public void AudioSourceSetting_UsesStableStringValues(AudioSourceType source, string expectedJson)
    {
        string json = JsonSerializer.Serialize(source);
        Assert.Equal(expectedJson, json);
        Assert.Equal(source, JsonSerializer.Deserialize<AudioSourceType>(json));
    }

    [Fact]
    public void Factory_CreatesNormalizedIndependentCaptureImplementations()
    {
        using IAudioCaptureSource system = AudioCaptureSourceFactory.Create(AudioSourceType.SystemAudio);
        using IAudioCaptureSource microphone = AudioCaptureSourceFactory.Create(AudioSourceType.Microphone);
        using IAudioCaptureSource external = AudioCaptureSourceFactory.Create(
            AudioSourceType.ExternalAudioInput, "endpoint-id", "USB Audio CODEC");

        Assert.IsType<WasapiLoopbackAudioSource>(system);
        Assert.IsType<MicrophoneAudioSource>(microphone);
        Assert.IsType<ExternalAudioInputSource>(external);
        Assert.Equal("USB Audio CODEC", external.DisplayName);
        Assert.Equal((16000, (short)16, (short)1),
            (system.SampleRate, system.BitsPerSample, system.Channels));
        Assert.Equal((16000, (short)16, (short)1),
            (microphone.SampleRate, microphone.BitsPerSample, microphone.Channels));
        Assert.Equal((16000, (short)16, (short)1),
            (external.SampleRate, external.BitsPerSample, external.Channels));
    }

    [Fact]
    public void ExternalDeviceSelection_PersistsStableIdAndFriendlyName()
    {
        string path = Path.Combine(Path.GetTempPath(), $"oneboard-settings-{Guid.NewGuid():N}.json");
        try
        {
            Setting original = new()
            {
                AudioSource = AudioSourceType.ExternalAudioInput,
                ExternalAudioDeviceId = "{0.0.1.00000000}.stable-id",
                ExternalAudioDeviceDisplayName = "USB Audio CODEC"
            };
            original.Save(path);

            Setting loaded = Setting.Load(path);

            Assert.Equal(AudioSourceType.ExternalAudioInput, loaded.AudioSource);
            Assert.Equal("{0.0.1.00000000}.stable-id", loaded.ExternalAudioDeviceId);
            Assert.Equal("USB Audio CODEC", loaded.ExternalAudioDeviceDisplayName);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void LegacySettings_KeepExistingValuesAndInitializeExternalDeviceFields()
    {
        Setting? loaded = JsonSerializer.Deserialize<Setting>(
            """{"AudioSource":"Microphone","SpeechLanguage":"vi-VN","TargetLanguage":"en-US"}""");

        Assert.NotNull(loaded);
        Assert.Equal(AudioSourceType.Microphone, loaded.AudioSource);
        Assert.Equal("vi-VN", loaded.SpeechLanguage);
        Assert.Equal("en-US", loaded.TargetLanguage);
        Assert.Equal(string.Empty, loaded.ExternalAudioDeviceId);
        Assert.Equal(string.Empty, loaded.ExternalAudioDeviceDisplayName);
    }

    [Fact]
    public void DeviceService_UsesStableIdsWhenFriendlyNamesMatch()
    {
        ExternalAudioDeviceService service = new(() =>
        [
            new AudioInputDevice("device-b", "USB Audio Device"),
            new AudioInputDevice("device-a", "USB Audio Device"),
            new AudioInputDevice("device-c", "Focusrite USB")
        ]);

        IReadOnlyList<AudioInputDevice> devices = service.GetActiveCaptureDevices();

        Assert.Equal(["device-c", "device-a", "device-b"], devices.Select(device => device.DeviceId));
        Assert.Equal(2, devices.Count(device => device.DisplayName == "USB Audio Device"));
    }

    [Fact]
    public void ExternalSource_MissingSavedDeviceFailsWithoutFallback()
    {
        MissingDeviceService service = new();
        using ExternalAudioInputSource source = new("missing-device", "Mixer USB Audio", service);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => source.Start());

        Assert.Contains("unavailable", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, service.OpenAttempts);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Normalizer_DownmixesEitherStereoChannelTo16KhzPcm(bool signalOnLeft)
    {
        WaveFormat sourceFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        Pcm16MonoNormalizer normalizer = new(sourceFormat);
        float[] samples = new float[4800 * 2];
        for (int frame = 0; frame < 4800; frame++)
            samples[(frame * 2) + (signalOnLeft ? 0 : 1)] = 0.8f;
        byte[] source = new byte[samples.Length * sizeof(float)];
        Buffer.BlockCopy(samples, 0, source, 0, source.Length);

        byte[] normalized = normalizer.Convert(source, source.Length).SelectMany(chunk => chunk).ToArray();

        Assert.NotEmpty(normalized);
        Assert.Equal(0, normalized.Length % sizeof(short));
        short[] pcm = new short[normalized.Length / sizeof(short)];
        Buffer.BlockCopy(normalized, 0, pcm, 0, normalized.Length);
        Assert.InRange(pcm.Average(sample => Math.Abs((double)sample)), 9000, 16000);
    }

    private sealed class MissingDeviceService : IExternalAudioDeviceService
    {
        public int OpenAttempts { get; private set; }

        public IReadOnlyList<AudioInputDevice> GetActiveCaptureDevices() => Array.Empty<AudioInputDevice>();

        public MMDevice OpenActiveCaptureDevice(string deviceId)
        {
            OpenAttempts++;
            throw new InvalidOperationException("Selected external audio device is unavailable.");
        }
    }
}
