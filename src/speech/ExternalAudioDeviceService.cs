using NAudio.CoreAudioApi;

namespace LiveCaptionsTranslator.speech
{
    public sealed record AudioInputDevice(string DeviceId, string DisplayName);

    public interface IExternalAudioDeviceService
    {
        IReadOnlyList<AudioInputDevice> GetActiveCaptureDevices();
        MMDevice OpenActiveCaptureDevice(string deviceId);
    }

    /// <summary>Discovers active Windows recording endpoints and reopens them by stable endpoint ID.</summary>
    public sealed class ExternalAudioDeviceService : IExternalAudioDeviceService
    {
        private readonly Func<IEnumerable<AudioInputDevice>>? enumerationOverride;

        public ExternalAudioDeviceService()
        {
        }

        public ExternalAudioDeviceService(Func<IEnumerable<AudioInputDevice>> enumerationOverride)
        {
            this.enumerationOverride = enumerationOverride ?? throw new ArgumentNullException(nameof(enumerationOverride));
        }

        public IReadOnlyList<AudioInputDevice> GetActiveCaptureDevices()
        {
            if (enumerationOverride != null)
                return OrderDevices(enumerationOverride());

            using MMDeviceEnumerator enumerator = new();
            MMDeviceCollection endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            List<AudioInputDevice> devices = new(endpoints.Count);
            foreach (MMDevice endpoint in endpoints)
            {
                using (endpoint)
                    devices.Add(new AudioInputDevice(endpoint.ID, endpoint.FriendlyName));
            }

            return OrderDevices(devices);
        }

        private static IReadOnlyList<AudioInputDevice> OrderDevices(IEnumerable<AudioInputDevice> devices) =>
            devices
                .Where(device => !string.IsNullOrWhiteSpace(device.DeviceId))
                .OrderBy(device => device.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(device => device.DeviceId, StringComparer.Ordinal)
                .ToArray();

        public MMDevice OpenActiveCaptureDevice(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new InvalidOperationException("Select an external audio input device before starting recognition.");

            try
            {
                MMDeviceEnumerator enumerator = new();
                try
                {
                    MMDevice device = enumerator.GetDevice(deviceId);
                    if (device.State != DeviceState.Active)
                    {
                        device.Dispose();
                        throw new InvalidOperationException("Selected external audio device is unavailable.");
                    }
                    return device;
                }
                finally
                {
                    enumerator.Dispose();
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Selected external audio device is unavailable.", ex);
            }
        }
    }
}
