using System;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Localization.JsonLocalizer.StringLocalizer
{
    public class JsonStringLocalizerFactory : IStringLocalizerFactory
    {
        private static readonly string[] KnownViewExtensions = new[] { ".cshtml" };
        
        private readonly ConcurrentDictionary<string, JsonStringLocalizer> _localizerCache =
            new ConcurrentDictionary<string, JsonStringLocalizer>();
        
        private readonly ILogger<JsonStringLocalizerFactory> _logger;
        private string _resourcesRelativePath;
        private string _applicationName;

        public JsonStringLocalizerFactory(IOptions<JsonLocalizationOptions> localizationOptions,
                                          ILogger<JsonStringLocalizerFactory> logger)
        {
            if (localizationOptions == null)
            {
                throw new ArgumentNullException(nameof(localizationOptions));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this._logger = logger;
            
            _resourcesRelativePath = localizationOptions.Value.ResourcesPath ?? string.Empty;
            if (!string.IsNullOrEmpty(_resourcesRelativePath))
            {
                _resourcesRelativePath = _resourcesRelativePath
                    .Replace(Path.AltDirectorySeparatorChar, '.')
                    .Replace(Path.DirectorySeparatorChar, '.') + ".";
            }
            
            _applicationName = Assembly.GetEntryAssembly().GetName().Name;
            logger.LogDebug($"Created {nameof(JsonStringLocalizerFactory)} with:{Environment.NewLine}" +
                $"    (application name: {_applicationName}{Environment.NewLine}" +
                $"    (resources relative path: {_resourcesRelativePath})");
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            if (resourceSource == null)
            {
                throw new ArgumentNullException(nameof(resourceSource));
            }
            
            _logger.LogDebug($"Getting localizer for type {resourceSource}");
            
            var typeInfo = resourceSource.GetTypeInfo();
            var assembly = typeInfo.Assembly;

            // Re-root the base name if a resources path is set.
            var resourceBaseName = string.IsNullOrEmpty(_resourcesRelativePath)
                ? typeInfo.FullName
                : _applicationName + "." + _resourcesRelativePath +
                    LocalizerUtil.TrimPrefix(typeInfo.FullName, _applicationName + ".");
            _logger.LogDebug($"Localizer basename: {resourceBaseName}");

            return _localizerCache.GetOrAdd(
                resourceBaseName, new JsonStringLocalizer(resourceBaseName, _applicationName, _logger));
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }
            
            _logger.LogDebug($"Getting localizer for baseName {baseName} and location {location}");
            
            location = location ?? _applicationName;
            
            // Re-root base name if a resources path is set and strip the cshtml part.
            var resourceBaseName = location + "." + _resourcesRelativePath + LocalizerUtil.TrimPrefix(baseName, location + ".");
            
            var viewExtension = KnownViewExtensions.FirstOrDefault(extension => resourceBaseName.EndsWith(extension));
            if (viewExtension != null)
            {
                resourceBaseName = resourceBaseName.Substring(0, resourceBaseName.Length - viewExtension.Length);
            }
            
            _logger.LogDebug($"Localizer basename: {resourceBaseName}");
            
            return _localizerCache.GetOrAdd(
                resourceBaseName, new JsonStringLocalizer(resourceBaseName, _applicationName, _logger));
        }
    }
}
