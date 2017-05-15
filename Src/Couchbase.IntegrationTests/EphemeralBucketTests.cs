﻿using Couchbase.Authentication;
using Couchbase.Core;
using Couchbase.IntegrationTests.Utils;
using Couchbase.Utils;
using NUnit.Framework;

namespace Couchbase.IntegrationTests
{
    [TestFixture]
    public class EphemeralBucketTests
    {
        private ICluster _cluster;

        [OneTimeSetUp]
        public void Setup()
        {
            _cluster = new Cluster(TestConfiguration.GetDefaultConfiguration());
            _cluster.Authenticate(new PasswordAuthenticator("Administrator", "password"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _cluster.Dispose();
        }

        [Test, Ignore("Ephemeral buckets are only supported for Couchbase server 5.0")]
        public void Submitting_View_Query_To_Ephemeral_Bucket_Fails()
        {
            var bucket = _cluster.OpenBucket("ephemeral");

            var viewQuery = bucket.CreateQuery("designDoc", "viewName");
            var result = bucket.Query<dynamic>(viewQuery);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(result.Error, ExceptionUtil.EphemeralBucketViewQueries);
        }
    }
}
