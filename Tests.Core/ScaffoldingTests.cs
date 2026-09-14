using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class ScaffoldingTests
    {
        [Test]
        public void CoreSourcesAreCompiledByTheSidecar()
        {
            Assert.That(Placeholder.AssemblyName, Is.EqualTo("CodexOfEchoes.Core"));
        }
    }
}
