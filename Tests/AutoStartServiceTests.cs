using InternetMeter.Services;
using Microsoft.Win32;
using Xunit;

namespace InternetMeter.Tests;

public class AutoStartServiceTests
{
    [Theory]
    [InlineData(@"C:\Program Files\InternetMeter\InternetMeter.exe", true, "\"C:\\Program Files\\InternetMeter\\InternetMeter.exe\" --autostart")]
    [InlineData(@"C:\Tools\InternetMeter.exe", false, "\"C:\\Tools\\InternetMeter.exe\"")]
    public void BuildCommandLine_WrapsQuotesAndAppendsAutostartFlag(string exePath, bool startMinimized, string expected)
    {
        var result = AutoStartService.BuildCommandLine(startMinimized, exePath);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void AutoStartService_RegistryOperations_WorkAgainstIsolatedSubKey()
    {
        var testKeyName = @"Software\InternetMeter_UnitTest_" + Guid.NewGuid().ToString("N");

        try
        {
            // Ensure clean test key
            using (var testKey = Registry.CurrentUser.CreateSubKey(testKeyName))
            {
                Assert.NotNull(testKey);
            }

            var service = new AutoStartService(testKeyName);

            // Initially not enabled
            Assert.False(service.IsAutoStartEnabled());

            // Enable auto-start
            service.SetAutoStart(true, startMinimized: true);
            Assert.True(service.IsAutoStartEnabled());

            // Verify stored value
            using (var key = Registry.CurrentUser.OpenSubKey(testKeyName, false))
            {
                var val = key?.GetValue("InternetMeter") as string;
                Assert.NotNull(val);
                Assert.Contains("--autostart", val);
            }

            // Disable auto-start
            service.SetAutoStart(false);
            Assert.False(service.IsAutoStartEnabled());
        }
        finally
        {
            // Clean up test registry key
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(testKeyName, false);
            }
            catch
            {
                // Silently ignore cleanup error
            }
        }
    }
}

