namespace InternetMeter.Models;

public record BandwidthSample(
    DateTime Timestamp,
    double DownloadSpeedBytes,
    double UploadSpeedBytes,
    string? TopProcessName,
    double TopProcessSpeedBytes);

