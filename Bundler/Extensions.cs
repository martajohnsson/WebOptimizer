﻿using Bundler.Transformers;
using Microsoft.AspNetCore.Builder;
using NUglify.Css;
using NUglify.JavaScript;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using Bundler.Utilities;

namespace Bundler
{
    /// <summary>
    /// Extension methods to register bundles and minifiers.
    /// </summary>
    public static class Extensions
    {
        // TODO: Add this to DI
        /// <summary>
        /// Gets the bundle options.
        /// </summary>
        public static Options Options { get; } = new Options();

        /// <summary>
        /// Adds Bundler to the <see cref="IApplicationBuilder"/> request execution pipeline
        /// </summary>
        /// <param name="app">The application object.</param>
        /// <param name="transformOptions">The transform options.</param>
        public static void UseBundles(this IApplicationBuilder app, Action<Options> transformOptions)
        {
            transformOptions(Options);

            foreach (ITransform transform in Options.Transforms)
            {
                app.Map(transform.Path, builder =>
                {
                    builder.UseMiddleware<BundleMiddleware>(transform);
                });
            }
        }

        /// <summary>
        /// Extension method to localizes the files in a bundle
        /// </summary>
        public static ITransform Localize<T>(this ITransform transform, IApplicationBuilder app)
        {
            transform.PostProcessors.Add(config =>
            {
                CultureInfo culture = LocalizationUtilities.GetRequestUICulture(config);
                IStringLocalizer<T> stringProvider = LocalizationUtilities.GetStringLocalizer<T>(app);

                config.Transform.CacheKeys["culture"] = culture.Name;

                return ScriptLocalizer.Localize(config.Content, stringProvider);
            });
            return transform;
        }

        /// <summary>
        /// Minifies JavaScript files (.js).
        /// </summary>
        public static void MinifyJavaScript(this IApplicationBuilder app, CodeSettings settings = null)
        {
            app.UseMiddleware<JavaScriptMiddleware>(settings ?? new CodeSettings());
        }

        /// <summary>
        /// Minifies CSS files (.css).
        /// </summary>
        public static void MinifyCss(this IApplicationBuilder app, CssSettings settings = null)
        {
            app.UseMiddleware<CssMiddleware>(settings ?? new CssSettings());
        }

        /// <summary>
        /// Adds a processor to the transformation
        /// </summary>
        public static ITransform Run(this ITransform transform, Func<BundlerConfig, string> func)
        {
            transform.PostProcessors.Add(func);
            return transform;
        }

        /// <summary>
        /// Adds a JavaScript bundle.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="route">The route name from where the bundle is served. Example: /my/bundle.js.</param>
        /// <param name="sourceFiles">An array of webroot relative file paths.</param>
        public static ITransform AddJs(this Options options, string route, params string[] sourceFiles)
        {
            ITransform transform = new JavaScriptMinifier(route).Include(sourceFiles);
            options.Transforms.Add(transform);

            return transform;
        }

        /// <summary>
        /// Adds a JavaScript bundle.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="settings">The JavaScript minification settings.</param>
        /// <param name="route">The route name from where the bundle is served. Example: /my/bundle.js.</param>
        /// <param name="sourceFiles">An array of webroot relative file paths.</param>
        public static ITransform AddJs(this Options options, CodeSettings settings, string route, params string[] sourceFiles)
        {
            ITransform transform = new JavaScriptMinifier(route, settings).Include(sourceFiles);
            options.Transforms.Add(transform);

            return transform;
        }

        /// <summary>
        /// Adds a CSS bundle.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="route">The route name from where the bundle is served. Example: /my/bundle.css.</param>
        /// <param name="sourceFiles">An array of webroot relative file paths.</param>
        public static ITransform AddCss(this Options options, string route, params string[] sourceFiles)
        {
            ITransform transform = new CssMinifier(route).Include(sourceFiles);
            options.Transforms.Add(transform);

            return transform;
        }

        /// <summary>
        /// Adds a CSS bundle.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="settings">The CSS minification settings.</param>
        /// <param name="route">The route name from where the bundle is served. Example: /my/bundle.css.</param>
        /// <param name="sourceFiles">An array of webroot relative file paths.</param>
        public static ITransform AddCss(this Options options, CssSettings settings, string route, params string[] sourceFiles)
        {
            ITransform transform = new CssMinifier(route, settings).Include(sourceFiles);
            options.Transforms.Add(transform);

            return transform;
        }
    }
}