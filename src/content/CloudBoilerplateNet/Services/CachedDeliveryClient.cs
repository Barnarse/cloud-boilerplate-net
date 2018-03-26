﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using KenticoCloud.Delivery;
using KenticoCloud.Delivery.InlineContentItems;
using CloudBoilerplateNet.Helpers;
using CloudBoilerplateNet.Models;

namespace CloudBoilerplateNet.Services
{
    public class CachedDeliveryClient : IDeliveryClient
    {
        #region "Constants"

        protected const string CODENAME_IDENTIFIER = "codename";
        protected const string SYSTEM_IDENTIFIER = "system";
        protected const string TYPE_IDENTIFIER = "type";
        protected const string MODULAR_CONTENT_IDENTIFIER = "modular_content";

        #endregion

        #region "Fields"

        #endregion

        #region "Properties"

        protected ICacheManager CacheManager { get; }
        protected DeliveryClient DeliveryClient { get; }
        public int CacheExpirySeconds { get; set; }
        public IContentLinkUrlResolver ContentLinkUrlResolver { get => DeliveryClient.ContentLinkUrlResolver; set => DeliveryClient.ContentLinkUrlResolver = value; }
        public ICodeFirstModelProvider CodeFirstModelProvider { get => DeliveryClient.CodeFirstModelProvider; set => DeliveryClient.CodeFirstModelProvider = value; }
        public IInlineContentItemsProcessor InlineContentItemsProcessor => DeliveryClient.InlineContentItemsProcessor;

        #endregion

        #region "Constructors"

        public CachedDeliveryClient(IOptions<ProjectOptions> projectOptions, ICacheManager cacheManager, IMemoryCache memoryCache)
        {
            DeliveryClient = new DeliveryClient(projectOptions.Value.DeliveryOptions);
            CacheExpirySeconds = projectOptions.Value.CacheTimeoutSeconds;
            CacheManager = cacheManager;
        }

        #endregion

        #region "Public methods"

        /// <summary>
        /// Returns a content item as JSON data.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content item with the specified codename.</returns>
        public async Task<JObject> GetItemJsonAsync(string codename, params string[] parameters)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.CONTENT_ITEM_SINGLE_JSON_IDENTIFIER, codename };
            identifierTokens.AddRange(parameters);

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetItemJsonAsync(codename, parameters), GetContentItemSingleJsonDependencies);
        }

        /// <summary>
        /// Returns content items as JSON data.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for filtering, ordering or depth of modular content.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<JObject> GetItemsJsonAsync(params string[] parameters)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.CONTENT_ITEM_LISTING_JSON_IDENTIFIER };
            identifierTokens.AddRange(parameters);

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetItemsJsonAsync(parameters), GetContentItemListingJsonDependencies);
        }

        /// <summary>
        /// Returns a content item.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse> GetItemAsync(string codename, params IQueryParameter[] parameters)
        {
            return await GetItemAsync(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Gets one strongly typed content item by its codename.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, params IQueryParameter[] parameters)
        {
            return await GetItemAsync<T>(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns a content item.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse> GetItemAsync(string codename, IEnumerable<IQueryParameter> parameters)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.CONTENT_ITEM_SINGLE_IDENTIFIER, codename };
            identifierTokens.AddRange(GetIdentifiersFromParameters(parameters));

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetItemAsync(codename, parameters), GetContentItemSingleDependencies);
        }

        /// <summary>
        /// Gets one strongly typed content item by its codename.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, IEnumerable<IQueryParameter> parameters)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.CONTENT_ITEM_SINGLE_TYPED_IDENTIFIER, codename };
            identifierTokens.AddRange(GetIdentifiersFromParameters(parameters));

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetItemAsync<T>(codename, parameters), GetContentItemSingleDependencies);
        }

        /// <summary>
        /// Searches the content repository for items that match the filter criteria.
        /// Returns content items.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for filtering, ordering or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse> GetItemsAsync(params IQueryParameter[] parameters)
        {
            return await GetItemsAsync((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns content items.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example for filtering, ordering or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse> GetItemsAsync(IEnumerable<IQueryParameter> parameters)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.CONTENT_ITEM_LISTING_IDENTIFIER };
            identifierTokens.AddRange(GetIdentifiersFromParameters(parameters));

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetItemsAsync(parameters), GetContentItemListingDependencies);
        }

        /// <summary>
        /// Searches the content repository for items that match the filter criteria.
        /// Returns strongly typed content items.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for filtering, ordering or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(params IQueryParameter[] parameters)
        {
            return await GetItemsAsync<T>((IEnumerable<IQueryParameter>)parameters);
        }

        public async Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(IEnumerable<IQueryParameter> parameters)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.CONTENT_ITEM_LISTING_TYPED_IDENTIFIER };
            identifierTokens.AddRange(GetIdentifiersFromParameters(parameters));

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetItemsAsync<T>(parameters), GetContentItemListingDependencies);
        }

        /// <summary>
        /// Returns a content type as JSON data.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content type with the specified codename.</returns>
        public async Task<JObject> GetTypeJsonAsync(string codename)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.CONTENT_TYPE_JSON_IDENTIFIER, codename };

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetTypeJsonAsync(codename), GetTypeSingleJsonDependencies);
        }

        /// <summary>
        /// Returns content types as JSON data.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for paging.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<JObject> GetTypesJsonAsync(params string[] parameters)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.CONTENT_TYPE_LISTING_JSON_IDENTIFIER };
            identifierTokens.AddRange(parameters);

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetTypesJsonAsync(parameters), GetTypeListingJsonDependencies);
        }

        /// <summary>
        /// Returns a content type.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The content type with the specified codename.</returns>
        public async Task<ContentType> GetTypeAsync(string codename)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.CONTENT_TYPE_SINGLE_IDENTIFIER, codename };

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetTypeAsync(codename), GetTypeSingleDependencies);
        }

        /// <summary>
        /// Returns content types.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for paging.</param>
        /// <returns>The <see cref="DeliveryTypeListingResponse"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<DeliveryTypeListingResponse> GetTypesAsync(params IQueryParameter[] parameters)
        {
            return await GetTypesAsync((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns content types.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example for paging.</param>
        /// <returns>The <see cref="DeliveryTypeListingResponse"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<DeliveryTypeListingResponse> GetTypesAsync(IEnumerable<IQueryParameter> parameters)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.CONTENT_TYPE_LISTING_IDENTIFIER };
            identifierTokens.AddRange(GetIdentifiersFromParameters(parameters));

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetTypesAsync(parameters), GetTypeListingDependencies);
        }

        /// <summary>
        /// Returns a content element.
        /// </summary>
        /// <param name="contentTypeCodename">The codename of the content type.</param>
        /// <param name="contentElementCodename">The codename of the content element.</param>
        /// <returns>A content element with the specified codename that is a part of a content type with the specified codename.</returns>
        public async Task<ContentElement> GetContentElementAsync(string contentTypeCodename, string contentElementCodename)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.CONTENT_ELEMENT_IDENTIFIER, contentTypeCodename, contentElementCodename };

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetContentElementAsync(contentTypeCodename, contentElementCodename), GetContentElementDependency);
        }

        public async Task<JObject> GetTaxonomyJsonAsync(string codename)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.TAXONOMY_GROUP_SINGLE_JSON_IDENTIFIER, codename };

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetTaxonomyJsonAsync(codename), GetTaxonomySingleJsonDependency);
        }

        public async Task<JObject> GetTaxonomiesJsonAsync(params string[] parameters)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.TAXONOMY_GROUP_LISTING_JSON_IDENTIFIER };
            identifierTokens.AddRange(parameters);

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetTaxonomiesJsonAsync(parameters), GetTaxonomyListingJsonDependencies);
        }

        public async Task<TaxonomyGroup> GetTaxonomyAsync(string codename)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.TAXONOMY_GROUP_SINGLE_IDENTIFIER, codename };

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetTaxonomyAsync(codename), GetTaxonomySingleDependency);
        }

        public async Task<DeliveryTaxonomyListingResponse> GetTaxonomiesAsync(params IQueryParameter[] parameters)
        {
            return await GetTaxonomiesAsync((IEnumerable<IQueryParameter>)parameters);
        }

        public async Task<DeliveryTaxonomyListingResponse> GetTaxonomiesAsync(IEnumerable<IQueryParameter> parameters)
        {
            var identifierTokens = new List<string> { KenticoCloudCacheHelper.TAXONOMY_GROUP_LISTING_IDENTIFIER };
            identifierTokens.AddRange(GetIdentifiersFromParameters(parameters));

            return await CacheManager.GetOrCreateAsync(identifierTokens, () => DeliveryClient.GetTaxonomiesAsync(parameters), GetTaxonomyListingDependencies);
        }

        #region "Dependency resolvers"

        public static IEnumerable<IdentifierSet> GetContentItemSingleDependencies(dynamic response)
        {
            var dependencies = new List<IdentifierSet>();
            dependencies.AddRange(GetModularContentDependencies(response));

            if (IsDeliverySingleResponse(response) && !string.IsNullOrEmpty(response?.Item?.System?.Codename))
            {
                // Create dummy item for the content item itself.
                var ownDependency = new IdentifierSet
                {
                    Type = KenticoCloudCacheHelper.CONTENT_ITEM_SINGLE_IDENTIFIER,
                    Codename = response.Item.System.Codename
                };

                if (!dependencies.Contains(ownDependency))
                {
                    dependencies.Add(ownDependency);
                }
            }

            return dependencies;
        }

        public static IEnumerable<IdentifierSet> GetContentItemSingleJsonDependencies(JObject response)
        {
            var dependencies = new List<IdentifierSet>();

            if (response?["item"] != null)
            {
                dependencies.AddRange(GetContentItemJsonTaxonomyDependencies(response["item"].ToObject<JObject>()));
            }

            dependencies.AddRange(GetJsonModularContentDependencies(response));

            var ownDependency = new IdentifierSet
            {
                Type = KenticoCloudCacheHelper.CONTENT_ITEM_SINGLE_JSON_IDENTIFIER,
                Codename = GetContentItemSingleCodenameFromJson(response)
            };

            if (!dependencies.Contains(ownDependency))
            {
                dependencies.Add(ownDependency);
            }

            return dependencies;
        }

        public static IEnumerable<IdentifierSet> GetContentItemListingDependencies(dynamic response)
        {
            var dependencies = new List<IdentifierSet>();
            dependencies.AddRange(GetModularContentDependencies(response));

            // Create dummy item for each content item in the listing.
            foreach (var codename in GetContentItemCodenamesFromListingResponse(response))
            {
                var dependency = new IdentifierSet
                {
                    Type = KenticoCloudCacheHelper.CONTENT_ITEM_SINGLE_IDENTIFIER,
                    Codename = codename
                };

                if (!dependencies.Contains(dependency))
                {
                    dependencies.Add(dependency);
                }
            }

            return dependencies;
        }





        public static IEnumerable<IdentifierSet> GetContentItemListingJsonDependencies(JObject response)
        {
            foreach (var mc in GetJsonModularContentDependencies(response))
            {
                yield return mc;
            }

            foreach (var item in response["items"].Children())
            {
                yield return new IdentifierSet
                {
                    Type = KenticoCloudCacheHelper.CONTENT_ITEM_LISTING_JSON_IDENTIFIER,
                    Codename = item[SYSTEM_IDENTIFIER][CODENAME_IDENTIFIER].ToString()
                };
            }
        }

        public static IEnumerable<IdentifierSet> GetContentElementDependency(ContentElement response)
        {
            return response != null ? new List<IdentifierSet>
            {
                new IdentifierSet
                {
                    Type = KenticoCloudCacheHelper.CONTENT_ELEMENT_IDENTIFIER,
                    Codename = string.Join("|", response.Type, response.Codename)
                }
            } : null;
        }

        public static IEnumerable<IdentifierSet> GetTaxonomySingleDependency(TaxonomyGroup response)
        {
            return response != null ? new List<IdentifierSet>
            {
                new IdentifierSet
                {
                    Type = KenticoCloudCacheHelper.TAXONOMY_GROUP_SINGLE_IDENTIFIER,
                    Codename = response.System?.Codename
                }
            } : null;
        }

        public static IEnumerable<IdentifierSet> GetTaxonomySingleJsonDependency(JObject response)
        {
            return response != null ? new List<IdentifierSet>
            {
                new IdentifierSet
                {
                    Type = KenticoCloudCacheHelper.TAXONOMY_GROUP_SINGLE_JSON_IDENTIFIER,
                    Codename = response[SYSTEM_IDENTIFIER][CODENAME_IDENTIFIER]?.ToString()
                }
            } : null;
        }

        public static IEnumerable<IdentifierSet> GetTaxonomyListingDependencies(DeliveryTaxonomyListingResponse response)
        {
            return response?.Taxonomies?.SelectMany(t => GetTaxonomySingleDependency(t));
        }

        public static IEnumerable<IdentifierSet> GetTaxonomyListingJsonDependencies(JObject response)
        {
            return response?["taxonomies"]?.SelectMany(t => GetTaxonomySingleJsonDependency(t.ToObject<JObject>()));
        }

        public static IEnumerable<IdentifierSet> GetTypeSingleDependencies(ContentType response)
        {
            foreach (var dependency in response?.Elements.SelectMany(e => GetContentElementDependency(e.Value)))
            {
                yield return dependency;
            }

            yield return new IdentifierSet
            {
                Type = KenticoCloudCacheHelper.CONTENT_TYPE_SINGLE_IDENTIFIER,
                Codename = response.System?.Codename
            };
        }

        public static IEnumerable<IdentifierSet> GetTypeSingleJsonDependencies(JObject response)
        {
            foreach (var element in response?["elements"])
            {
                if (!string.IsNullOrEmpty((element as JProperty)?.Name))
                {
                    yield return new IdentifierSet
                    {
                        Type = KenticoCloudCacheHelper.CONTENT_ELEMENT_JSON_IDENTIFIER,
                        Codename = (element as JProperty)?.Name
                    };
                }
            }

            yield return new IdentifierSet
            {
                Type = KenticoCloudCacheHelper.CONTENT_TYPE_JSON_IDENTIFIER,
                Codename = response[SYSTEM_IDENTIFIER][CODENAME_IDENTIFIER]?.ToString()
            };
        }

        public static IEnumerable<IdentifierSet> GetTypeListingDependencies(DeliveryTypeListingResponse response)
        {
            return response?.Types?.SelectMany(t => GetTypeSingleDependencies(t));
        }

        public static IEnumerable<IdentifierSet> GetTypeListingJsonDependencies(JObject response)
        {
            return response?["types"]?.SelectMany(t => GetTypeSingleJsonDependencies(t.ToObject<JObject>()));
        }

        #endregion

        #endregion

        #region "Helper methods"

        protected static IEnumerable<string> GetContentItemCodenamesFromListingResponse(dynamic response)
        {
            if (IsDeliveryListingResponse(response))
            {
                foreach (dynamic item in response.Items)
                {
                    if (!string.IsNullOrEmpty(item.System?.Codename))
                    {
                        yield return item.System?.Codename;
                    }
                }
            }
        }

        protected static IEnumerable<IdentifierSet> GetModularContentDependencies(dynamic response)
        {
            if (IsDeliveryResponse(response))
            {
                var dependencies = new List<IdentifierSet>();

                foreach (var item in response.ModularContent)
                {
                    foreach (var codename in item.Children()[SYSTEM_IDENTIFIER][CODENAME_IDENTIFIER])
                    {
                        dependencies.Add(new IdentifierSet
                        {
                            Type = KenticoCloudCacheHelper.CONTENT_ITEM_SINGLE_IDENTIFIER,
                            Codename = codename.ToString()
                        });

                    }

                    IEnumerable<IdentifierSet> taxonomyDependencies = GetContentItemTaxonomyDependencies(item);
                    var filtered = taxonomyDependencies.Where(i => !dependencies.Contains(i));
                    dependencies.AddRange(filtered);
                }

                return dependencies;
            }

            return null;
        }

        // TODO: Unit tests
        protected static IEnumerable<IdentifierSet> GetJsonModularContentDependencies(JObject response)
        {
            var dependencies = new List<IdentifierSet>();

            foreach (var item in response?[MODULAR_CONTENT_IDENTIFIER])
            {
                dependencies.AddRange(item.Children()[SYSTEM_IDENTIFIER][CODENAME_IDENTIFIER]?.Select(cn =>
                {
                    return new IdentifierSet
                    {
                        Type = KenticoCloudCacheHelper.CONTENT_ITEM_SINGLE_JSON_IDENTIFIER,
                        Codename = cn.ToString()
                    };
                }));

                dependencies.AddRange(item.Children().SelectMany(ch => GetContentItemJsonTaxonomyDependencies(ch.ToObject<JObject>())).Where(i => !dependencies.Contains(i)));
            };

            return dependencies;
        }

        protected static IEnumerable<IdentifierSet> GetContentItemTaxonomyDependencies(dynamic responseFragment)
        {
            foreach (var kunda in responseFragment)
            {
                foreach (var zmrd in kunda)
                {
                    if (zmrd.Name == "elements")
                    {
                        foreach (var picus in zmrd)
                        {
                            foreach (var elementContainer in picus)
                            {
                                foreach (var mrdka in elementContainer)
                                {
                                    foreach (var cecky in mrdka)
                                    {
                                        if (cecky.Name == "taxonomy_group")
                                        {
                                            foreach (var doPiceSdynamicemZasranym in cecky)
                                            {
                                                if (!string.IsNullOrEmpty(doPiceSdynamicemZasranym?.ToString()))
                                                {
                                                    yield return new IdentifierSet
                                                    {
                                                        Type = KenticoCloudCacheHelper.TAXONOMY_GROUP_SINGLE_IDENTIFIER,
                                                        Codename = doPiceSdynamicemZasranym.ToString()
                                                    }; 
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected static IEnumerable<IdentifierSet> GetContentItemJsonTaxonomyDependencies(JObject responseFragment)
        {
            var taxonomyElements = responseFragment?["elements"]?.SelectMany(t => t.Children())?
                                .Where(e => e[TYPE_IDENTIFIER] != null && e[TYPE_IDENTIFIER].ToString().Equals("taxonomy", StringComparison.Ordinal) && e["taxonomy_group"] != null && !string.IsNullOrEmpty(e["taxonomy_group"].ToString()));

            return taxonomyElements.Select(e => new IdentifierSet
            {
                Type = KenticoCloudCacheHelper.TAXONOMY_GROUP_SINGLE_JSON_IDENTIFIER,
                Codename = e["taxonomy_group"].ToString()
            });
        }

        // TODO: Unit tests
        protected static IEnumerable<string> GetModularContentCodenames(dynamic response)
        {
            // if (response.ModularContent != null && response.ModularContent is System.Collections.IEnumerable) is not completely safe
            if (IsDeliveryResponse(response))
            {
                foreach (var mc in response.ModularContent)
                {
                    if (!string.IsNullOrEmpty(mc.Path))
                    {
                        yield return mc.Path;
                    }
                }
            }
        }

        protected static bool IsDeliveryResponse(dynamic response)
        {
            if (IsDeliverySingleResponse(response) || IsDeliveryListingResponse(response))
            {
                return true;
            }

            return false;
        }

        protected static bool IsDeliverySingleResponse(dynamic response)
        {
            if (response is DeliveryItemResponse || (response.GetType().IsGenericType && response.GetType().GetGenericTypeDefinition() == typeof(DeliveryItemResponse<>)))
            {
                return true;
            }

            return false;
        }

        protected static bool IsDeliveryListingResponse(dynamic response)
        {
            if (response is DeliveryItemListingResponse || (response.GetType().IsGenericType && response.GetType().GetGenericTypeDefinition() == typeof(DeliveryItemListingResponse<>)))
            {
                return true;
            }

            return false;
        }

        protected static IEnumerable<string> GetIdentifiersFromParameters(IEnumerable<IQueryParameter> parameters)
        {
            return parameters?.Select(p => p.GetQueryStringParameter());
        }

        protected static string GetContentItemSingleCodenameFromJson(JObject response)
        {
            return response?["item"][SYSTEM_IDENTIFIER][CODENAME_IDENTIFIER]?.ToString();
        }

        #endregion
    }
}
