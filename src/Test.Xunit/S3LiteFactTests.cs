namespace Test.Xunit
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Core;
    using Touchstone.XunitAdapter;
    using Xunit;

    /// <summary>
    /// xUnit host for all shared S3Lite Touchstone suites.
    /// </summary>
    public sealed class S3LiteFactTests : TouchstoneFactBase
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
        /// Execute every shared S3Lite descriptor through the Touchstone xUnit adapter.
        /// </summary>
        /// <returns>Task.</returns>
        [Fact]
        public async Task RunAll()
        {
            await RunAllAsync();
        }
    }
}
