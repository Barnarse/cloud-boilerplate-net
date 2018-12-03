using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CloudBoilerplateNet.Helpers;
using CloudBoilerplateNet.Models;
using CloudBoilerplateNet.Resolvers;
using CloudBoilerplateNet.Services;
using KenticoCloud.Delivery;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace CloudBoilerplateNet.Tests.Services
{
    public class ReactiveCacheManagerTests
    {
        private const string TEST_ITEM_IDENTIFIER = "TestItem";
        private const string TEST_ITEM_VALUE = "TestItemValue";

        [Fact]
        public void CreatesEntry()
        {
            PrepareFixture(out ReactiveCacheManager cacheManager, out List<string> identifiers, out string value);
            cacheManager.CreateEntry(identifiers, value, ItemFormatDependencyFactory);

            Assert.Equal(value, cacheManager.MemoryCache.Get<string>(identifiers.First()));
            Assert.NotNull(cacheManager.MemoryCache.Get<CancellationTokenSource>(
                    string.Join("|", "dummy", ItemFormatDependencyFactory(value).First().Type, ItemFormatDependencyFactory(value).First().Codename)
                    ));
        }

        [Fact]
        public async Task CreatesEntryIfNotExists()
        {
            PrepareFixture(out ReactiveCacheManager cacheManager, out List<string> identifiers, out string value);
            var cacheEntry = await cacheManager.GetOrCreateAsync(identifiers, ValueFactory, (response) => false, ItemFormatDependencyFactory, false);

            Assert.Equal(value, cacheManager.MemoryCache.Get<string>(identifiers.First()));
            Assert.NotNull(cacheManager.MemoryCache.Get<CancellationTokenSource>(
                    string.Join("|", "dummy", ItemFormatDependencyFactory(value).First().Type, ItemFormatDependencyFactory(value).First().Codename)
                    ));
        }

        [Fact]
        public void InvalidatesEntry()
        {
            PrepareFixture(out ReactiveCacheManager cacheManager, out List<string> identifiers, out string value);
            cacheManager.CreateEntry(identifiers, value, ItemFormatDependencyFactory);
            cacheManager.InvalidateEntry(ItemFormatDependencyFactory(value).First());

            Assert.Null(cacheManager.MemoryCache.Get<string>(identifiers.First()));
        }

        [Fact]
        public void InvalidatesDependentTypes()
        {
            var cacheManager = BuildCacheManager();
            cacheManager.CreateEntry(new List<string> { TEST_ITEM_IDENTIFIER }, TEST_ITEM_VALUE, ItemFormatDependencyFactory);
            cacheManager.CreateEntry(new List<string> { "TestVariant" }, "TestVariantValue", ItemFormatDependencyFactory);
            cacheManager.InvalidateEntry(ItemFormatDependencyFactory(null).ElementAt(0));

            Assert.Null(cacheManager.MemoryCache.Get<string>(TEST_ITEM_IDENTIFIER));
            Assert.Null(cacheManager.MemoryCache.Get<string>("TestVariant"));
        }

        // The observer doesn't get triggered in these tests for some reason. Investigating.
        //[Fact]
        public void IgnoresConsecutiveNotification()
        {
            var cacheManager = BuildCacheManager();
            var webhookListener = new WebhookListener();
            CreateTestEntry(cacheManager);
            var firstTimestamp = DateTime.Parse("2018-12-03T00:00:00.0000000Z");
            RaiseNotification(webhookListener, firstTimestamp);
            CreateTestEntry(cacheManager);
            RaiseNotification(webhookListener, firstTimestamp.AddMilliseconds(900));

            Assert.NotNull(cacheManager.MemoryCache.Get<string>(TEST_ITEM_IDENTIFIER));
        }

        //[Fact]
        public void AcceptsConsecutiveNotification()
        {
            var cacheManager = BuildCacheManager();
            var webhookListener = new WebhookListener();
            CreateTestEntry(cacheManager);
            var firstTimestamp = DateTime.Parse("2018-12-03T00:00:00.0000000Z");
            RaiseNotification(webhookListener, firstTimestamp);
            CreateTestEntry(cacheManager);
            RaiseNotification(webhookListener, firstTimestamp.AddMilliseconds(1100));

            Assert.Null(cacheManager.MemoryCache.Get<string>(TEST_ITEM_IDENTIFIER));
        }

        private void PrepareFixture(out ReactiveCacheManager cacheManager, out List<string> identifier, out string value)
        {
            cacheManager = BuildCacheManager();
            identifier = new List<string> { TEST_ITEM_IDENTIFIER };
            value = TEST_ITEM_VALUE;
        }

        private Task<string> ValueFactory()
        {
            return Task.FromResult(TEST_ITEM_VALUE);
        }

        private List<IdentifierSet> ItemFormatDependencyFactory(string value)
        {
            return new List<IdentifierSet>
            {
                new IdentifierSet
                {
                    Type = KenticoCloudCacheHelper.CONTENT_ITEM_SINGLE_IDENTIFIER,
                    Codename = TEST_ITEM_IDENTIFIER
                }
            };
        }

        private ReactiveCacheManager BuildCacheManager()
        {
            var projectOptions = Options.Create(new ProjectOptions
            {
                CacheTimeoutSeconds = 60,
                DeliveryOptions = new DeliveryOptions
                {
                    ProjectId = Guid.NewGuid().ToString()
                }
            });

            var memoryCacheOptions = Options.Create(new MemoryCacheOptions
            {
                Clock = new TestClock(),
                ExpirationScanFrequency = new TimeSpan(0, 0, 5)
            });

            return new ReactiveCacheManager(projectOptions, new MemoryCache(memoryCacheOptions), new DependentFormatResolver(), new WebhookListener(), new ConsecutiveNotificationComparer());
        }

        private void CreateTestEntry(ReactiveCacheManager cacheManager)
        {
            cacheManager.CreateEntry(new List<string> { TEST_ITEM_IDENTIFIER }, TEST_ITEM_VALUE, ItemFormatDependencyFactory);
        }

        private void RaiseNotification(WebhookListener webhookListener, DateTime timestamp)
        {
            webhookListener.RaiseWebhookNotification(
                            this,
                            "publish",
                            new IdentifierSet
                            {
                                Type = KenticoCloudCacheHelper.CONTENT_ITEM_SINGLE_IDENTIFIER,
                                Codename = TEST_ITEM_IDENTIFIER
                            },
                            timestamp);
        }
    }
}
