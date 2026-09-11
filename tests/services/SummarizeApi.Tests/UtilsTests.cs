namespace SummarizeApi.Tests;

[TestClass]
public class UtilsTests
{
    [TestMethod]
    [DataRow("http://8.8.8.8", true)] // Google's public DNS
    [DataRow("not a url", false)]
    [DataRow("http://127.0.0.1", false)]
    [DataRow("http://10.0.0.1", false)]
    [DataRow("http://172.16.0.1", false)]
    [DataRow("http://192.168.1.1", false)]
    [DataRow("http://[::1]", false)]
    [DataRow("http://localhost:8080", false)]
    public async Task IsValidExternalUrl_Tests(string url, bool expected)
    {
        var result = await Utils.IsValidExternalUrl(url);
        Assert.AreEqual(expected, result);
    }
}
