using Registry.PresentationKit.Auth;

namespace Registry.UnitTests.PresentationKit;

public class ReturnUrlTests
{
    [Theory]
    [InlineData("/setup", "/setup")]
    [InlineData("/groups?id=1", "/groups?id=1")]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("https://evil.example", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData("/\\evil.example", "/")]
    public void Only_paths_inside_the_app_are_kept(string? returnUrl, string expected) =>
        Assert.Equal(expected, ReturnUrl.Sanitize(returnUrl));
}
