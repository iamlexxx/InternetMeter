using InternetMeter.Models;

namespace InternetMeter.Services;

public class RollingHistoryService : IRollingHistoryService
{
    public const int DefaultCapacity = 3600; // 60 minutes at 1 Hz
    private readonly int _capacity;
    private readonly BandwidthSample?[] _buffer;
    private int _head = 0;
    private int _count = 0;
    private readonly object _lock = new();

    public int Capacity => _capacity;
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _count;
            }
        }
    }

    public event Action<BandwidthSample>? SampleRecorded;

    public RollingHistoryService(int capacity = DefaultCapacity)
    {
        _capacity = capacity > 0 ? capacity : DefaultCapacity;
        _buffer = new BandwidthSample?[_capacity];
    }

    public void RecordSample(DateTime timestamp, double downloadSpeedBytes, double uploadSpeedBytes, string? topProcessName, double topProcessSpeedBytes)
    {
        var sample = new BandwidthSample(timestamp, downloadSpeedBytes, uploadSpeedBytes, topProcessName, topProcessSpeedBytes);
        lock (_lock)
        {
            _buffer[_head] = sample;
            _head = (_head + 1) % _capacity;
            if (_count < _capacity)
            {
                _count++;
            }
        }

        SampleRecorded?.Invoke(sample);
    }

    public IReadOnlyList<BandwidthSample> GetAllSamples()
    {
        lock (_lock)
        {
            if (_count == 0) return Array.Empty<BandwidthSample>();

            var result = new List<BandwidthSample>(_count);
            int start = (_head - _count + _capacity) % _capacity;
            for (int i = 0; i < _count; i++)
            {
                int index = (start + i) % _capacity;
                var s = _buffer[index];
                if (s != null)
                {
                    result.Add(s);
                }
            }
            return result;
        }
    }

    public IReadOnlyList<BandwidthSample> GetSamples(TimeSpan duration)
    {
        lock (_lock)
        {
            if (_count == 0) return Array.Empty<BandwidthSample>();

            int targetSeconds = (int)Math.Ceiling(duration.TotalSeconds);
            if (targetSeconds <= 0) return Array.Empty<BandwidthSample>();

            int takeCount = Math.Min(targetSeconds, _count);
            var result = new List<BandwidthSample>(takeCount);
            int start = (_head - takeCount + _capacity) % _capacity;
            for (int i = 0; i < takeCount; i++)
            {
                int index = (start + i) % _capacity;
                var s = _buffer[index];
                if (s != null)
                {
                    result.Add(s);
                }
            }
            return result;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_buffer, 0, _capacity);
            _head = 0;
            _count = 0;
        }
    }
}

