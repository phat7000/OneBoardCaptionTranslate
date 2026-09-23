using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace LiveCaptionsTranslator.models
{
    public sealed class TranscriptLine : INotifyPropertyChanged
    {
        private string text;
        private bool isFinal;

        public TranscriptLine(string text, bool isFinal, DateTimeOffset timestamp)
        {
            this.text = text;
            this.isFinal = isFinal;
            Timestamp = timestamp;
        }

        public string Text
        {
            get => text;
            set { text = value; OnPropertyChanged(); }
        }

        public bool IsFinal
        {
            get => isFinal;
            set { isFinal = value; OnPropertyChanged(); }
        }

        public DateTimeOffset Timestamp { get; }
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// A rolling transcript independent from viewport height. Final lines are stable and exactly one
    /// mutable partial line is coalesced to a 100 ms UI cadence.
    /// </summary>
    public sealed class TranscriptBuffer
    {
        private const int MaxLines = 200;
        private readonly object gate = new();
        private readonly TimeSpan partialCadence = TimeSpan.FromMilliseconds(100);
        private TranscriptLine? partialLine;
        private string? pendingPartial;
        private DateTimeOffset pendingTimestamp;
        private int partialGeneration;
        private bool partialUpdateScheduled;

        public ObservableCollection<TranscriptLine> Lines { get; } = new();
        public event EventHandler? Changed;

        public string CombinedText => string.Join(Environment.NewLine,
            Lines.Select(line => line.Text).Where(text => !string.IsNullOrWhiteSpace(text)));

        public string FinalText => string.Join(" ",
            Lines.Where(line => line.IsFinal).Select(line => line.Text).Where(text => !string.IsNullOrWhiteSpace(text)));

        public string CurrentPartialText => partialLine?.Text ?? string.Empty;

        public void UpdatePartial(string text, DateTimeOffset timestamp)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            int generation;
            lock (gate)
            {
                pendingPartial = text.Trim();
                pendingTimestamp = timestamp;
                generation = partialGeneration;
                if (partialUpdateScheduled)
                    return;
                partialUpdateScheduled = true;
            }

            _ = FlushPartialAfterDelayAsync(generation);
        }

        public void FinalizeLine(string text, DateTimeOffset timestamp)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            string finalText = text.Trim();
            lock (gate)
            {
                partialGeneration++;
                pendingPartial = null;
                partialUpdateScheduled = false;
            }

            Dispatch(() =>
            {
                if (partialLine != null)
                {
                    partialLine.Text = finalText;
                    partialLine.IsFinal = true;
                    partialLine = null;
                }
                else if (Lines.Count == 0 ||
                         !string.Equals(Lines[^1].Text, finalText, StringComparison.Ordinal))
                {
                    Lines.Add(new TranscriptLine(finalText, true, timestamp));
                }
                Trim();
                Changed?.Invoke(this, EventArgs.Empty);
            }, DispatcherPriority.DataBind);
        }

        public void ClearPartial()
        {
            lock (gate)
            {
                partialGeneration++;
                pendingPartial = null;
                partialUpdateScheduled = false;
            }
            Dispatch(() =>
            {
                if (partialLine != null)
                    Lines.Remove(partialLine);
                partialLine = null;
                Changed?.Invoke(this, EventArgs.Empty);
            }, DispatcherPriority.DataBind);
        }

        private async Task FlushPartialAfterDelayAsync(int generation)
        {
            await Task.Delay(partialCadence);
            string? text;
            DateTimeOffset timestamp;
            lock (gate)
            {
                if (generation != partialGeneration)
                    return;
                text = pendingPartial;
                timestamp = pendingTimestamp;
                pendingPartial = null;
                partialUpdateScheduled = false;
            }

            if (string.IsNullOrWhiteSpace(text))
                return;
            Dispatch(() =>
            {
                lock (gate)
                {
                    if (generation != partialGeneration)
                        return;
                }
                if (partialLine == null)
                {
                    partialLine = new TranscriptLine(text, false, timestamp);
                    Lines.Add(partialLine);
                    Trim();
                }
                else
                {
                    partialLine.Text = text;
                }
                Changed?.Invoke(this, EventArgs.Empty);
            }, DispatcherPriority.Background);
        }

        private void Trim()
        {
            while (Lines.Count > MaxLines)
                Lines.RemoveAt(0);
        }

        private static void Dispatch(Action action, DispatcherPriority priority)
        {
            Dispatcher? dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                _ = dispatcher.BeginInvoke(action, priority);
        }
    }
}
