using System.Text.Json;

using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.speech;

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

        Assert.IsType<WasapiLoopbackAudioSource>(system);
        Assert.IsType<MicrophoneAudioSource>(microphone);
        Assert.Equal((16000, (short)16, (short)1),
            (system.SampleRate, system.BitsPerSample, system.Channels));
        Assert.Equal((16000, (short)16, (short)1),
            (microphone.SampleRate, microphone.BitsPerSample, microphone.Channels));
    }

}
