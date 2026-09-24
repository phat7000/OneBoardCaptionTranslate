using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.Tests;

public class TranscriptBufferTests
{
    [Fact]
    public async Task PartialUpdates_MutateOneLine_ThenPromoteIt()
    {
        TranscriptBuffer buffer = new();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        buffer.UpdatePartial("hel", now);
        buffer.UpdatePartial("hello", now);
        await WaitForAsync(() => buffer.Lines.Count == 1);

        Assert.Single(buffer.Lines);
        Assert.Equal("hello", buffer.Lines[0].Text);
        Assert.False(buffer.Lines[0].IsFinal);

        buffer.FinalizeLine("hello world", now);

        Assert.Single(buffer.Lines);
        Assert.Equal("hello world", buffer.Lines[0].Text);
        Assert.True(buffer.Lines[0].IsFinal);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(2);
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(20);
    }
}
