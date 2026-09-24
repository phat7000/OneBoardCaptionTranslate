using LiveCaptionsTranslator.speech;

namespace LiveCaptionsTranslator.Tests;

public class SpeechProviderSessionTests
{
    [Fact]
    public async Task ProviderSwitch_StopsAndDisposesOldProviderBeforeStartingNewOne()
    {
        SessionTracker tracker = new();
        SpeechProviderSession session = new(
            id => tracker.Create(id),
            id => id);

        await session.SwitchAsync("AzureSpeech", "en-US");
        await session.SwitchAsync("GoogleSpeech", "en-US");

        Assert.Equal("GoogleSpeech", session.CurrentProviderId);
        Assert.Equal(1, tracker.ActiveProviders);
        Assert.Equal(1, tracker.MaximumActiveProviders);
        Assert.Equal(
            ["start:AzureSpeech", "stop:AzureSpeech", "dispose:AzureSpeech", "start:GoogleSpeech"],
            tracker.Actions);

        await session.StopAsync();
        Assert.Equal(0, tracker.ActiveProviders);
    }

    [Fact]
    public async Task AudioSourceRestart_ForcesFreshProviderAndDisposesPreviousCaptureOwner()
    {
        SessionTracker tracker = new();
        SpeechProviderSession session = new(
            id => tracker.Create(id),
            id => id);

        await session.SwitchAsync("AzureSpeech", "en-US");
        await session.SwitchAsync("AzureSpeech", "en-US", forceRestart: true);

        Assert.Equal(2, tracker.CreatedProviders);
        Assert.Equal(1, tracker.ActiveProviders);
        Assert.Equal(1, tracker.MaximumActiveProviders);
        Assert.Equal(
            ["start:AzureSpeech", "stop:AzureSpeech", "dispose:AzureSpeech", "start:AzureSpeech"],
            tracker.Actions);

        await session.StopAsync();
    }

    private sealed class SessionTracker
    {
        public List<string> Actions { get; } = [];
        public int ActiveProviders { get; set; }
        public int MaximumActiveProviders { get; set; }
        public int CreatedProviders { get; private set; }

        public FakeProvider Create(string id)
        {
            CreatedProviders++;
            return new FakeProvider(id, this);
        }
    }

    private sealed class FakeProvider(string id, SessionTracker tracker) : ISpeechRecognitionProvider
    {
        public string Id { get; } = id;
        public string DisplayName => Id;
        public bool IsRunning { get; private set; }
        public event EventHandler<SpeechResult>? ResultReceived;
        public event EventHandler<SpeechProviderStatus>? StatusChanged;

        public Task<SpeechConfigurationValidation> ValidateConfigurationAsync(CancellationToken cancellationToken) =>
            Task.FromResult(SpeechConfigurationValidation.Valid());

        public Task StartAsync(string languageCode, CancellationToken cancellationToken)
        {
            IsRunning = true;
            tracker.ActiveProviders++;
            tracker.MaximumActiveProviders = Math.Max(tracker.MaximumActiveProviders, tracker.ActiveProviders);
            tracker.Actions.Add($"start:{Id}");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (IsRunning)
            {
                IsRunning = false;
                tracker.ActiveProviders--;
                tracker.Actions.Add($"stop:{Id}");
            }
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            tracker.Actions.Add($"dispose:{Id}");
            return ValueTask.CompletedTask;
        }
    }
}
