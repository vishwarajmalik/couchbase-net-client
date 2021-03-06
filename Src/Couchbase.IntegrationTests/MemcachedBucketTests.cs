﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Core;
using Couchbase.IO;
using Couchbase.Utils;
using Couchbase.Views;
using NUnit.Framework;

namespace Couchbase.IntegrationTests
{
    [TestFixture]
    public class MemcachedBucketTests
    {
        private ICluster _cluster;

        [OneTimeSetUp]
        public void SetUp()
        {
            var config = Utils.TestConfiguration.GetCurrentConfiguration();
            _cluster = new Cluster(config);
            _cluster.OpenBucket("memcached"); // load memcached bucket before tests run
        }

        [Test]
        public void Replace_DocumentDoesNotExistException()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                //setup
                var key = "Replace_DocumentDoesNotExistException";
                bucket.Remove(new Document<dynamic> {Id = key});

                //act
                var result = bucket.Replace(new Document<dynamic> {Id = key, Content = new {name = "foo"}});

                //assert
                Assert.AreEqual(result.Exception.GetType(), typeof(DocumentDoesNotExistException));
            }
        }

        [Test]
        public async Task ReplaceAsync_DocumentDoesNotExistException()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                //setup
                var key = "ReplaceAsync_DocumentDoesNotExistException";
                bucket.Remove(new Document<dynamic> {Id = key});

                //act
                var result = await bucket.ReplaceAsync(new Document<dynamic> {Id = key, Content = new {name = "foo"}})
                        .ContinueOnAnyContext();

                //assert
                Assert.AreEqual(result.Exception.GetType(), typeof(DocumentDoesNotExistException));
            }
        }

        [Test]
        public void Insert_DocumentAlreadyExistsException()
        {

            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                //setup
                var key = "Insert_DocumentAlreadyExistsException";
                bucket.Remove(new Document<dynamic> {Id = key});
                bucket.Insert(new Document<dynamic> {Id = key, Content = new {name = "foo"}});

                //act
                var result = bucket.Insert(new Document<dynamic> {Id = key, Content = new {name = "foo"}});

                //assert
                Assert.AreEqual(result.Exception.GetType(), typeof(DocumentAlreadyExistsException));
            }
        }

        [Test]
        public async Task InsertAsync_DocumentAlreadyExistsException()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                //setup
                var key = "Insert_DocumentAlreadyExistsException";
                bucket.Remove(new Document<dynamic> {Id = key});
                bucket.Insert(new Document<dynamic> {Id = key, Content = new {name = "foo"}});

                //act
                var result = await bucket.InsertAsync(new Document<dynamic> {Id = key, Content = new {name = "foo"}})
                        .ContinueOnAnyContext();

                //assert
                Assert.AreEqual(result.Exception.GetType(), typeof(DocumentAlreadyExistsException));
            }
        }

        [Test]
        public void Replace_WithCasAndMutated_CasMismatchException()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                //setup
                var key = "ReplaceWithCas_CasMismatchException";
                bucket.Remove(new Document<dynamic> {Id = key});

                var docWithCas = bucket.Insert(new Document<dynamic> {Id = key, Content = new {name = "foo"}});
                bucket.Upsert(new Document<dynamic> {Id = key, Content = new {name = "foochanged!"}});

                //act
                var result = bucket.Replace(new Document<dynamic>
                {
                    Id = key,
                    Content = new {name = "foobarr"},
                    Cas = docWithCas.Document.Cas
                });

                //assert
                Assert.AreEqual(result.Exception.GetType(), typeof(CasMismatchException));
            }
        }

        [Test]
        public async Task ReplaceAsync_WithCasAndMutated_CasMismatchException()
        {
            //setup
            var key = "ReplaceWithCas_CasMismatchException";
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                bucket.Remove(new Document<dynamic> {Id = key});

                var docWithCas = bucket.Insert(new Document<dynamic> {Id = key, Content = new {name = "foo"}});
                bucket.Upsert(new Document<dynamic> {Id = key, Content = new {name = "foochanged!"}});

                //act
                var result = await bucket.ReplaceAsync(new Document<dynamic>
                {
                    Id = key,
                    Content = new {name = "foobarr"},
                    Cas = docWithCas.Document.Cas
                }).ContinueOnAnyContext();

                //assert
                Assert.AreEqual(result.Exception.GetType(), typeof(CasMismatchException));
            }
        }

        [Test]
        public void Test_OpenBucket()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                Assert.IsNotNull(bucket);
            }
        }

        [Test]
        public void When_Key_Does_Not_Exist_Exists_Returns_False()
        {
            var key = "thekeythatdoesnotexists_perhaps";
            using (var bucket = _cluster.OpenBucket())
            {
                var result = bucket.Exists(key);
                Assert.IsFalse(result);
            }
        }

        [Test]
        public void When_Key_Exists_Exists_Returns_True()
        {
            var key = "thekeythatexists";
            using (var bucket = _cluster.OpenBucket())
            {
                bucket.Remove(key);
                bucket.Upsert(key, "somevalue");
                var result = bucket.Exists(key);
                Assert.IsTrue(result);
            }
        }


        [Test]
        public void Test_That_OpenBucket_Throws_AuthenticationException_If_Bucket_Does_Not_Exist()
        {
            var ex = Assert.Throws<AggregateException>(() => _cluster.OpenBucket("doesnotexist"));

            Assert.True(ex.InnerExceptions.OfType<AuthenticationException>().Any());
        }

        [Test]
        public void Test_Insert_With_String()
        {
            const int zero = 0;
            const string key = "memkey1";
            const string value = "somedata";

            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var result = bucket.Upsert(key, value);
                Assert.IsTrue(result.Success);
                Assert.AreEqual(ResponseStatus.Success, result.Status);
                Assert.AreEqual(string.Empty, result.Message);
                Assert.AreEqual(string.Empty, result.Value);
                Assert.Greater(result.Cas, zero);
            }
        }

        [Test]
        public void Test_Get_With_String()
        {
            const int zero = 0;
            const string key = "memkey1";
            const string value = "somedata";

            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                bucket.Upsert(key, value);
                var result = bucket.Get<string>(key);

                Assert.IsTrue(result.Success);
                Assert.AreEqual(ResponseStatus.Success, result.Status);
                Assert.AreEqual(string.Empty, result.Message);
                Assert.AreEqual(value, result.Value);
                Assert.Greater(result.Cas, zero);
            }
        }

        [Test]
        public void When_Key_Does_Not_Exist_Replace_Fails()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                const string key = "When_Key_Does_Not_Exist_Replace_Fails";
                var value = new { P1 = "p1" };
                var result = bucket.Replace(key, value);
                Assert.IsFalse(result.Success);
                Assert.AreEqual(ResponseStatus.KeyNotFound, result.Status);
            }
        }

        [Test]
        public void When_Key_Exists_Replace_Succeeds()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                const string key = "When_Key_Exists_Replace_Succeeds";
                var value = new { P1 = "p1" };
                bucket.Upsert(key, value);

                var result = bucket.Replace(key, value);
                Assert.IsTrue(result.Success);
                Assert.AreEqual(ResponseStatus.Success, result.Status);
            }
        }

        [Test]
        public void When_Cas_Has_Changed_Replace_Fails()
        {
            const string key = "CouchbaseBucket.When_Cas_Has_Changed_Replace_Fails";
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                bucket.Remove(key);
                var set = bucket.Insert(key, "value");
                Assert.IsTrue(set.Success);

                var upsert = bucket.Upsert(key, "newvalue");
                Assert.IsTrue(upsert.Success);

                var replace = bucket.Replace(key, "should fail", set.Cas);
                Assert.IsFalse(replace.Success);
            }
        }

        [Test]
        public void When_Cas_Has_Not_Changed_Replace_Succeeds()
        {
            const string key = "CouchbaseBucket.When_Cas_Has_Not_Changed_Replace_Succeeds";
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                bucket.Remove(key);
                var set = bucket.Insert(key, "value");
                Assert.IsTrue(set.Success);

                var get = bucket.Get<string>(key);
                Assert.AreEqual(get.Cas, set.Cas);

                var replace = bucket.Replace(key, "should succeed", get.Cas);
                Assert.True(replace.Success);

                get = bucket.Get<string>(key);
                Assert.AreEqual("should succeed", get.Value);
            }
        }

        [Test]
        public void When_Key_Exists_Delete_Returns_Success()
        {
            const string key = "When_Key_Exists_Delete_Returns_Success";
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                bucket.Upsert(key, new { Foo = "foo" });
                var result = bucket.Remove(key);
                Assert.IsTrue(result.Success);
                Assert.AreEqual(result.Status, ResponseStatus.Success);
            }
        }

        [Test]
        public void When_Key_Does_Not_Exist_Delete_Returns_Success()
        {
            const string key = "When_Key_Does_Not_Exist_Delete_Returns_Success";
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var result = bucket.Remove(key);
                Assert.IsFalse(result.Success);
                Assert.AreEqual(result.Status, ResponseStatus.KeyNotFound);
            }
        }

        [Test]
        public void Test_Upsert()
        {
            const string key = "Test_Upsert";
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var expDoc1 = new { Bar = "Bar1" };
                var expDoc2 = new { Bar = "Bar2" };

                bucket.Remove(key);
                var result = bucket.Upsert(key, expDoc1);
                Assert.IsTrue(result.Success);

                var result1 = bucket.Get<dynamic>(key);
                Assert.IsTrue(result1.Success);

                var actDoc1 = result1.Value;
                Assert.AreEqual(expDoc1.Bar, actDoc1.bar.Value);

                var result2 = bucket.Upsert(key, expDoc2);
                Assert.IsTrue(result2.Success);

                var result3 = bucket.Get<dynamic>(key);
                Assert.IsTrue(result3.Success);

                var actDoc2 = result3.Value;
                Assert.AreEqual(expDoc2.Bar, actDoc2.bar.Value);
            }
        }

        [Test]
        public void When_KeyExists_Insert_Fails()
        {
            const string key = "When_KeyExists_Insert_Fails";
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                dynamic doc = new { Bar = "Bar1" };
                var result = bucket.Upsert(key, doc);
                Assert.IsTrue(result.Success);

                //Act
                var result1 = bucket.Insert(key, doc);

                //Assert
                Assert.IsFalse(result1.Success);
                Assert.AreEqual(result1.Status, ResponseStatus.KeyExists);
            }
        }

        [Test]
        public void When_Key_Does_Not_Exist_Insert_Succeeds()
        {
            const string key = "When_Key_Does_Not_Exist_Insert_Fails";
            using (var bucket = _cluster.OpenBucket())
            {
                //Arrange - delete key if it exists
                bucket.Remove(key);

                //Act
                var result1 = bucket.Insert(key, new { Bar = "somebar" });

                //Assert
                Assert.IsTrue(result1.Success);
                Assert.AreEqual(result1.Status, ResponseStatus.Success);
            }
        }

        [Test]
        public void When_Query_Called_On_Memcached_Bucket_With_N1QL_NotSupportedException_Is_Thrown()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                const string query = "SELECT * FROM tutorial WHERE fname = 'Ian'";

                Assert.Throws<NotSupportedException>(() => bucket.Query<dynamic>(query));
            }
        }

        [Test]
        public void When_Query_Called_On_Memcached_Bucket_With_ViewQuery_NotSupportedException_Is_Thrown()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var query = new ViewQuery();

                Assert.Throws<NotSupportedException>(() => bucket.Query<dynamic>(query));
            }
        }

        [Test]
        public void When_CreateQuery_Called_On_Memcached_Bucket_NotSupportedException_Is_Thrown()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                Assert.Throws<NotSupportedException>(() => bucket.CreateQuery("designdoc", "view", true));
            }
        }

        [Test]
        public void When_CreateQuery2_Called_On_Memcached_Bucket_NotSupportedException_Is_Thrown()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var ex = Assert.Throws<NotSupportedException>(() => bucket.CreateQuery("designdoc", "view"));
            }
        }

        [Test]
        public void When_CreateQuery3_Called_On_Memcached_Bucket_NotSupportedException_Is_Thrown()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var ex = Assert.Throws<NotSupportedException>(() => bucket.CreateQuery("designdoc", "view", true));
            }
        }

        [Test]
        public void When_Integer_Is_Incremented_By_Default_Value_Increases_By_One()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                const string key = "When_Integer_Is_Incremented_Value_Increases_By_One";
                bucket.Remove(key);

                var result = bucket.Increment(key);
                Assert.IsTrue(result.Success);
                Assert.AreEqual(1, result.Value);

                result = bucket.Increment(key);
                Assert.IsTrue(result.Success);
                Assert.AreEqual(2, result.Value);
            }
        }

        [Test]
        public void When_Delta_Is_10_And_Initial_Is_2_The_Result_Is_12()
        {
            const string key = "When_Delta_Is_10_And_Initial_Is_2_The_Result_Is_12";
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                bucket.Remove(key);
                var result = bucket.Increment(key, 10, 2);
                Assert.IsTrue(result.Success);
                Assert.AreEqual(2, result.Value);

                result = bucket.Increment(key, 10, 2);
                Assert.IsTrue(result.Success);
                Assert.AreEqual(12, result.Value);
            }
        }

        [Test]
        public void When_Expiration_Is_2_Key_Expires_After_2_Seconds()
        {
            const string key = "When_Expiration_Is_10_Key_Expires_After_10_Seconds";
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                bucket.Remove(key);
                var result = bucket.Increment(key, 1, 1, 1);
                Assert.IsTrue(result.Success);
                Assert.AreEqual(1, result.Value);
                Thread.Sleep(2000);
                result = bucket.Get<ulong>(key);
                Assert.AreEqual(ResponseStatus.KeyNotFound, result.Status);
            }
        }

        [Test]
        public void When_Key_Is_Decremented_Past_Zero_It_Remains_At_Zero()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                const string key = "When_Key_Is_Decremented_Past_Zero_It_Remains_At_Zero";

                //remove key if it exists
                bucket.Remove(key);

                //will add the initial value
                var result = bucket.Decrement(key);
                Assert.IsTrue(result.Success);
                Assert.AreEqual(1, result.Value);

                //decrement the key
                result = bucket.Decrement(key);
                Assert.IsTrue(result.Success);
                Assert.AreEqual(0, result.Value);

                //Should still be zero
                result = bucket.Decrement(key);
                Assert.IsTrue(result.Success);
                Assert.AreEqual(0, result.Value);
            }
        }

        [Test]
        public void When_Delta_Is_2_And_Initial_Is_4_The_Result_When_Decremented_Is_2()
        {
            const string key = "When_Delta_Is_2_And_Initial_Is_4_The_Result_When_Decremented_Is_2";
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                bucket.Remove(key);
                var result = bucket.Decrement(key, 2, 4);
                Assert.IsTrue(result.Success);
                Assert.AreEqual(4, result.Value);

                result = bucket.Decrement(key, 2, 4);
                Assert.IsTrue(result.Success);
                Assert.AreEqual(2, result.Value);
            }
        }

        [Test]
        public void When_Expiration_Is_2_Decremented_Key_Expires_After_2_Seconds()
        {
            const string key = "When_Expiration_Is_2_Decremented_Key_Expires_After_2_Seconds";
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                bucket.Remove(key);
                var result = bucket.Decrement(key, 1, 1, 1);
                Assert.IsTrue(result.Success);
                Assert.AreEqual(1, result.Value);
                Thread.Sleep(2000);
                result = bucket.Get<ulong>(key);
                Assert.AreEqual(ResponseStatus.KeyNotFound, result.Status);
            }
        }

        [Test]
        public void Test_MultiGet()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var keys = new List<string>();
                for (int i = 0; i < 1000; i++)
                {
                    var key = "key" + i;
                    bucket.Upsert(key, key);
                    keys.Add(key);
                }
                var multiget = bucket.Get<string>(keys);
                Assert.AreEqual(1000, multiget.Count);
            }
        }

        [Test]
        public void Test_Multi_Upsert()
        {
            using (var bucket = _cluster.OpenBucket("beer-sample")) //TODO fix this to "memcached"
            {
                var items = new Dictionary<string, dynamic>
                {
                    {"MemcachedBucketTests.Test_Multi_Upsert.String", "string"},
                    {"MemcachedBucketTests.Test_Multi_Upsert.Json", new {Foo = "Bar", Baz = 2}},
                    {"MemcachedBucketTests.Test_Multi_Upsert.Int", 2},
                    {"MemcachedBucketTests.Test_Multi_Upsert.Number", 5.8},
                    {"MemcachedBucketTests.Test_Multi_Upsert.Binary", new[] {0x00, 0x00}}
                };
                var multiUpsert = bucket.Upsert(items);
                Assert.AreEqual(multiUpsert.Count, items.Count);
                foreach (var item in multiUpsert)
                {
                    Assert.IsTrue(item.Value.Success);
                }
            }
        }

        [Test]
        public void When_Increment_Overflows_Value_Wraps_To_Zero()
        {
            using (var bucket = _cluster.OpenBucket())
            {
                var key = "When_Increment_Overflows_Value_Wraps_To_Zero";
                bucket.Remove(key);
                Assert.IsTrue(bucket.Insert(key, ulong.MaxValue.ToString()).Success);
                var result = bucket.Increment(key);
                Assert.AreEqual(0, result.Value);
                result = bucket.Increment(key);
                Assert.AreEqual(1, result.Value);
            }
        }

        [Test]
        public void Test_Memcached_BucketType()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                Assert.AreEqual(Couchbase.Core.Buckets.BucketTypeEnum.Memcached, bucket.BucketType);
            }
        }

        [Test]
        [Category("Integration")]
        [Category("Memcached")]
        public void Test_Multi_Remove()
        {
            var items = new Dictionary<string, string>();
            for (int i = 0; i < 1000; i++)
            {
                items.Add("key" + i, "Value" + i);
            }
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var multiUpsert = bucket.Upsert(items);
                Assert.AreEqual(items.Count, multiUpsert.Count);
                foreach (var pair in multiUpsert)
                {
                    Assert.IsTrue(pair.Value.Success);
                }

                var multiRemove = bucket.Remove(multiUpsert.Keys.ToList());
                foreach (var pair in multiRemove)
                {
                    Assert.IsTrue(pair.Value.Success);
                }

                var multiGet = bucket.Get<string>(multiUpsert.Keys.ToList());
                foreach (var pair in multiGet)
                {
                    Assert.IsFalse(pair.Value.Success);
                }
            }
        }

        [Test]
        [Category("Integration")]
        [Category("Memcached")]
        public void Test_Multi_Remove_With_MaxDegreeOfParallelism_2()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var items = new Dictionary<string, dynamic>
                {
                    {"CouchbaseBucketTests.Test_Multi_Upsert.String", "string"},
                    {"CouchbaseBucketTests.Test_Multi_Upsert.Json", new {Foo = "Bar", Baz = 2}},
                    {"CouchbaseBucketTests.Test_Multi_Upsert.Int", 2},
                    {"CouchbaseBucketTests.Test_Multi_Upsert.Number", 5.8},
                    {"CouchbaseBucketTests.Test_Multi_Upsert.Binary", new[] {0x00, 0x00}}
                };
                bucket.Upsert(items);

                var multiRemove = bucket.Remove(items.Keys.ToList(), new ParallelOptions { MaxDegreeOfParallelism = 2 });
                Assert.AreEqual(multiRemove.Count, items.Count);
                foreach (var item in multiRemove)
                {
                    Assert.IsTrue(item.Value.Success);
                }
            }
        }

        [Test]
        [Category("Integration")]
        [Category("Memcached")]
        public void Test_Multi_Remove_With_MaxDegreeOfParallelism_2_RangeSize_2()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var items = new Dictionary<string, dynamic>
                {
                    {"CouchbaseBucketTests.Test_Multi_Upsert.String", "string"},
                    {"CouchbaseBucketTests.Test_Multi_Upsert.Json", new {Foo = "Bar", Baz = 2}},
                    {"CouchbaseBucketTests.Test_Multi_Upsert.Int", 2},
                    {"CouchbaseBucketTests.Test_Multi_Upsert.Number", 5.8},
                    {"CouchbaseBucketTests.Test_Multi_Upsert.Binary", new[] {0x00, 0x00}}
                };
                bucket.Upsert(items);

                var multiRemove = bucket.Remove(items.Keys.ToList(), new ParallelOptions
                {
                    MaxDegreeOfParallelism = 2
                }, 2);
                Assert.AreEqual(multiRemove.Count, items.Count);
                foreach (var item in multiRemove)
                {
                    Assert.IsTrue(item.Value.Success);
                }
            }
        }

        [Test]
        [Category("Integration")]
        [Category("Memcached")]
        public void When_Keys_For_MultiGet_Are_Empty_Exception_Is_Not_Thrown()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var keys = new List<string>();
                var results = bucket.Get<dynamic>(keys);
                Assert.AreEqual(0, results.Count);
            }
        }

        [Test]
        [Category("Integration")]
        [Category("Memcached")]
        public void When_Keys_For_MultiRemove_Are_Empty_Exception_Is_Not_Thrown()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var keys = new List<string>();
                var results = bucket.Remove(keys);
                Assert.AreEqual(0, results.Count);
            }
        }

        [Test]
        [Category("Integration")]
        [Category("Memcached")]
        public void When_Keys_For_MultiUpsert_Are_Empty_Exception_Is_Not_Thrown()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var keys = new Dictionary<string, object>();
                var results = bucket.Upsert(keys);
                Assert.AreEqual(0, results.Count);
            }
        }

        [Test]
        [Category("Integration")]
        [Category("Memcached")]
        public void When_GetAndTouch_Is_Called_Expiration_Is_Extended()
        {
            var key = "When_GetAndTouch_Is_Called_Expiration_Is_Extended";
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                bucket.Remove(key);
                bucket.Insert(key, "{value}", new TimeSpan(0, 0, 0, 2));
                Thread.Sleep(3000);
                var result = bucket.Get<string>(key);
                Assert.AreEqual(result.Status, ResponseStatus.KeyNotFound);
                bucket.Remove(key);
                bucket.Insert(key, "{value}", new TimeSpan(0, 0, 0, 2));
                result = bucket.GetAndTouch<string>(key, new TimeSpan(0, 0, 0, 5));
                Assert.IsTrue(result.Success);
                Assert.AreEqual(result.Value, "{value}");
                Thread.Sleep(3000);
                result = bucket.Get<string>(key);
                Assert.AreEqual(result.Status, ResponseStatus.Success);
            }
        }

        [Test]
        [Category("Integration")]
        [Category("Memcached")]
        public void When_Key_Is_Touched_Expiration_Is_Extended()
        {
            var key = "When_Key_Is_Touched_Expiration_Is_Extended";
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                bucket.Remove(key);
                bucket.Insert(key, "{value}", new TimeSpan(0, 0, 0, 2));
                Thread.Sleep(3000);
                var result = bucket.Get<string>(key);
                Assert.AreEqual(result.Status, ResponseStatus.KeyNotFound);
                bucket.Remove(key);
                bucket.Insert(key, "{value}", new TimeSpan(0, 0, 0, 2));
                bucket.Touch(key, new TimeSpan(0, 0, 0, 5));
                Thread.Sleep(3000);
                result = bucket.Get<string>(key);
                Assert.AreEqual(result.Status, ResponseStatus.Success);
            }
        }

        [Test]
        public void When_Document_Has_Expiry_It_Is_Evicted_After_It_Expires_Upsert()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var document = new Document<dynamic>
                {
                    Id = "When_Document_Has_Expiry_It_Is_Evicted_After_It_Expires_Upsert",
                    Expiry = 2000,
                    Content = new { name = "I expire in 2000 milliseconds." }

                };

                var upsert = bucket.Upsert(document);
                Assert.IsTrue(upsert.Success);

                var get = bucket.GetDocument<dynamic>(document.Id);
                Assert.AreEqual(ResponseStatus.Success, get.Status);

                Thread.Sleep(3000);
                get = bucket.GetDocument<dynamic>(document.Id);
                Assert.AreEqual(ResponseStatus.KeyNotFound, get.Status);
            }
        }

        [Test]
        public void When_Document_Has_Expiry_It_Is_Evicted_After_It_Expires_Insert()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var document = new Document<dynamic>
                {
                    Id = "When_Document_Has_Expiry_It_Is_Evicted_After_It_Expires_Insert",
                    Expiry = 2000,
                    Content = new { name = "I expire in 2000 milliseconds." }

                };

                bucket.Remove(document);
                var upsert = bucket.Insert(document);
                Assert.IsTrue(upsert.Success);

                var get = bucket.GetDocument<dynamic>(document.Id);
                Assert.AreEqual(ResponseStatus.Success, get.Status);

                Thread.Sleep(3000);
                get = bucket.GetDocument<dynamic>(document.Id);
                Assert.AreEqual(ResponseStatus.KeyNotFound, get.Status);
            }
        }

        [Test]
        public void When_Document_Has_Expiry_It_Is_Evicted_After_It_Expires_Replace()
        {
            using (var bucket = _cluster.OpenBucket("memcached"))
            {
                var document = new Document<dynamic>
                {
                    Id = "When_Document_Has_Expiry_It_Is_Evicted_After_It_Expires_Replace",
                    Expiry = 2000,
                    Content = new { name = "I expire in 2000 milliseconds." }

                };

                bucket.Remove(document);
                var upsert = bucket.Insert(document);
                Assert.IsTrue(upsert.Success);

                var replace = bucket.Replace(document);
                Assert.IsTrue(replace.Success);

                var get = bucket.GetDocument<dynamic>(document.Id);
                Assert.AreEqual(ResponseStatus.Success, get.Status);

                Thread.Sleep(3000);
                get = bucket.GetDocument<dynamic>(document.Id);
                Assert.AreEqual(ResponseStatus.KeyNotFound, get.Status);
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _cluster.Dispose();
        }
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2014 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/

#endregion
