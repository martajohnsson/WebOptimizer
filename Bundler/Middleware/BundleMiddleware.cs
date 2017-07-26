﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bundler.Transformers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Bundler
{
    /// <summary>
    /// Middleware for setting up bundles
    /// </summary>
    public class BundleMiddleware : BaseMiddleware
    {
        private readonly ITransform _transform;

        /// <summary>
        /// Initializes a new instance of the <see cref="BundleMiddleware"/> class.
        /// </summary>
        public BundleMiddleware(RequestDelegate next, IHostingEnvironment env, ITransform transform, IMemoryCache cache)
            : base(next, cache, env)
        {
            _transform = transform;
        }

        /// <summary>
        /// Gets the content type of the response.
        /// </summary>
        protected override string ContentType => _transform.ContentType;

        /// <summary>
        /// Invokes the middleware
        /// </summary>
        public override async Task<string> ExecuteAsync(HttpContext context)
        {
            string source = await GetContentAsync(_transform);
            return _transform.Transform(context, source);
        }

        /// <summary>
        /// A list of files used for cache invalidation.
        /// </summary>
        protected override IEnumerable<string> GetFiles(HttpContext context)
        {
            return _transform.SourceFiles;
        }

        private async Task<string> GetContentAsync(ITransform transform)
        {
            IEnumerable<string> absolutes = transform.SourceFiles.Select(f => FileProvider.GetFileInfo(f).PhysicalPath);
            var sb = new StringBuilder();

            foreach (string absolute in absolutes)
            {
                sb.AppendLine(await File.ReadAllTextAsync(absolute));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the cache key.
        /// </summary>
        protected override string GetCacheKey(HttpContext context)
        {
            string baseCacheKey = base.GetCacheKey(context);

            if (!string.IsNullOrEmpty(baseCacheKey))
            {
                string transformKey = string.Join("", _transform.CacheKeys.Select(p => p.Key + p.Value));
                return (base.GetCacheKey(context) + transformKey).GetHashCode().ToString();
            }

            return null;
        }
    }
}