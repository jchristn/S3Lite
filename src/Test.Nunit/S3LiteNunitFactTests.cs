namespace Test.Nunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.NunitAdapter;

    /// <summary>
    /// NUnit host for all shared S3Lite Touchstone suites.
    /// </summary>
    [TestFixture]
    public sealed class S3LiteNunitFactTests : TouchstoneNunitBase
    {
        /// <summary>
        /// Shared S3Lite test suites.
        /// </summary>
        protected override IReadOnlyList<TestSuiteDescriptor> Suites
        {
            get
            {
                return S3LiteTestSuites.All;
            }
        }

        /// <summary>
        /// Execute every shared S3Lite descriptor through the Touchstone NUnit adapter.
        /// </summary>
        /// <returns>Task.</returns>
        [Test]
        public async Task RunAll()
        {
            await RunAllAsync().ConfigureAwait(false);
        }
    }
}
