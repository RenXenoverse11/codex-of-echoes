using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class ScaffoldingTests
    {
        [Test]
        public void SidecarCompilesCoreSources()
        {
            Assert.That(typeof(Rng.SeededRandom).Namespace,
                Is.EqualTo("CodexOfEchoes.Core.Rng"));
        }
    }
}
