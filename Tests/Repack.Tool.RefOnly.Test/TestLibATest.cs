using TestLibA;

namespace Repack.Tool.Test;

public class TestLibATest
{
    [Fact]
    public void SampleA()
    {
        SampleA sampleA = new();
        Assert.Equal(1, sampleA.PerformWork());
    }
}
