using Zapstar.Api.Services;

namespace Zapstar.Api.Tests.Services;

public class LightningAddressParserTests
{
    [Fact]
    public void FindCandidate_ReturnsNull_ForEmptyText()
    {
        Assert.Null(LightningAddressParser.FindCandidate(""));
        Assert.Null(LightningAddressParser.FindCandidate(null));
    }

    [Fact]
    public void FindCandidate_ReturnsNull_WhenNoEmailShapedString()
    {
        Assert.Null(LightningAddressParser.FindCandidate("Just a regular bio, no address here."));
    }

    [Fact]
    public void FindCandidate_FindsBareEmailShapedString()
    {
        var result = LightningAddressParser.FindCandidate("Contact me at tobses@getalby.com for tips");
        Assert.Equal("tobses@getalby.com", result);
    }

    [Fact]
    public void FindCandidate_PrefersLightningBoltPrefixedAddress()
    {
        var text = "reach me at contact@company.com or ⚡ tobses@getalby.com for zaps";
        var result = LightningAddressParser.FindCandidate(text);
        Assert.Equal("tobses@getalby.com", result);
    }

    [Fact]
    public void FindCandidate_PrefersExplicitLightningPrefix()
    {
        var text = "email: hello@example.com, lightning: tobses@getalby.com";
        var result = LightningAddressParser.FindCandidate(text);
        Assert.Equal("tobses@getalby.com", result);
    }

    [Fact]
    public void FindCandidate_FallsBackToFirstMatch_WhenNoPrefixPresent()
    {
        var text = "some text with first@example.com and second@example.com";
        var result = LightningAddressParser.FindCandidate(text);
        Assert.Equal("first@example.com", result);
    }
}