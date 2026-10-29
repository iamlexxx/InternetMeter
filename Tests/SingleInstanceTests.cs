using Xunit;

namespace InternetMeter.Tests;

public class SingleInstanceTests
{
    [Fact]
    public void NamedMutex_DetectsSecondInstance()
    {
        var testMutexName = "Local\\InternetMeter_TestMutex_" + Guid.NewGuid();

        using var firstInstance = new Mutex(true, testMutexName, out bool firstCreated);
        Assert.True(firstCreated, "First instance must acquire ownership.");

        using var secondInstance = new Mutex(true, testMutexName, out bool secondCreated);
        Assert.False(secondCreated, "Second instance must be rejected.");
    }

    [Fact]
    public void NamedMutex_AllowsNewInstanceAfterFirstExits()
    {
        var testMutexName = "Local\\InternetMeter_TestMutex_" + Guid.NewGuid();

        var firstInstance = new Mutex(true, testMutexName, out bool firstCreated);
        Assert.True(firstCreated);

        firstInstance.ReleaseMutex();
        firstInstance.Dispose();

        using var secondInstance = new Mutex(true, testMutexName, out bool secondCreated);
        Assert.True(secondCreated, "Subsequent instance must succeed after previous instance disposes.");
    }
}

