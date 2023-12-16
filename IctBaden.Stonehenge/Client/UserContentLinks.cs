using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IctBaden.Stonehenge.Extensions;
using IctBaden.Stonehenge.Resources;

namespace IctBaden.Stonehenge.Client
{
    public static class UserContentLinks
    {
        private const string CssInsertPoint = "<!--stonehengeUserStylesheets-->";
        private const string CssLinkTemplate = "<link type='text/css' rel='stylesheet' href='{0}'>";

        private const string ExtensionsInsertPoint = "<!--stonehengeExtensions-->";
        private const string JsUserScriptsInsertPoint = "<!--stonehengeUserScripts-->";
        private const string JsLinkTemplate = "<script type='application/javascript' src='{0}'></script>";

        private static readonly Dictionary<string, string> StyleSheets = new Dictionary<string, string>();
        private static readonly string AppPath = Path.DirectorySeparatorChar + "app" + Path.DirectorySeparatorChar;

        public static string InsertUserCssLinks(Assembly appAssembly, string appFilesPath, string text, string theme)
        {
            if (!StyleSheets.ContainsKey(theme))
            {
                var styleSheets = string.Empty;

                var path = Path.Combine(appFilesPath, "styles");
                if (Directory.Exists(path))
                {
                    var links = Directory.GetFiles(path, "*.css", SearchOption.AllDirectories)
                      .Select(dir => string.Format(CssLinkTemplate, dir.Substring(dir.IndexOf(AppPath, StringComparison.InvariantCulture) + 1).Replace('\\', '/')));
                    styleSheets = string.Join(Environment.NewLine, links);
                }

                const string resourceBaseName = ".app.";
                const string baseNameStyles = resourceBaseName + "styles.";
                const string baseNameTheme = resourceBaseName + "themes.";
                var resourceNames = appAssembly.GetManifestResourceNames();
                var cssResources = resourceNames.Where(name => name.EndsWith(".css")).ToList();
                // styles first
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var resourceName in cssResources.Where(name => name.Contains(baseNameStyles)))
                {
                    var css = ResourceLoader.GetShortResourceName(appAssembly, resourceBaseName, resourceName).Replace(".", "/").Replace("/css", ".css");
                    styleSheets += Environment.NewLine + string.Format(CssLinkTemplate, css);
                }
                // then themes
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var resourceName in cssResources.Where(name => name.Contains(baseNameTheme + theme)))
                {
                    var css = ResourceLoader.GetShortResourceName(appAssembly, resourceBaseName, resourceName).Replace(".", "/").Replace("/css", ".css");
                    styleSheets += Environment.NewLine + string.Format(CssLinkTemplate, css);
                }

                path = Path.Combine(appFilesPath, "app", "themes", theme + ".css");
                if (File.Exists(path))
                {
                    var css = path.Substring(path.IndexOf(AppPath, StringComparison.InvariantCulture) + 1).Replace('\\', '/');
                    styleSheets += Environment.NewLine + string.Format(CssLinkTemplate, css);
                }

                StyleSheets.TryAdd(theme, styleSheets);
            }
            return text.Replace(CssInsertPoint, StyleSheets[theme]);
        }

        public static string InsertUserJsLinks(Assembly userAssembly, string appFilesPath, string text)
        {
            var scripts = string.Empty;

            var path = Path.Combine(appFilesPath, "scripts");
            if (Directory.Exists(path))
            {
                var links = Directory.GetFiles(path, "*.js", SearchOption.AllDirectories)
                  .Select(dir => string.Format(JsLinkTemplate, dir.Substring(dir.IndexOf(AppPath, StringComparison.InvariantCulture) + 1).Replace('\\', '/')));
                scripts = string.Join(Environment.NewLine, links);
            }

            const string resourceBaseName = ".app.";
            const string baseNameScripts = resourceBaseName + "scripts.";
            var resourceNames = userAssembly.GetManifestResourceNames();
            var jsResources = resourceNames.Where(name => name.EndsWith(".js")).ToList();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var resourceName in jsResources.Where(name => name.Contains(baseNameScripts)))
            {
                var js = ResourceLoader.GetShortResourceName(userAssembly, resourceBaseName, resourceName)
                    .Replace(".", "/")
                    .Replace("/js", ".js");
                scripts += Environment.NewLine + string.Format(JsLinkTemplate, js);
            }

            return text.Replace(JsUserScriptsInsertPoint, scripts);
        }
        
        public static string InsertExtensionLinks(List<Assembly> assemblies, string text)
        {
            var scripts = string.Empty;

            const string resourceBaseName = ".app.";
            const string baseNameSrc = resourceBaseName + "src.";
            foreach (var assembly in assemblies)
            {
                if(assembly.GetTypes().FirstOrDefault(t => t.GetInterfaces().Contains(typeof(IStonehengeExtension))) == null) continue;
                
                var resources = assembly.GetManifestResourceNames();

                var jsResources = resources
                    .Where(name => name.EndsWith(".js") && !name.Contains(".min."))
                    .ToArray();
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var resourceName in jsResources.Where(name => name.Contains(baseNameSrc)))
                {
                    var js = ResourceLoader.GetShortResourceName(assembly, resourceBaseName, resourceName)
                        .Replace(".", "/")
                        .Replace("/js", ".js");
                    if (resources.Contains(resourceName.Replace(".js", ".min.js")))
                    {
                        js = js.Replace(".js", "{.min}.js");
                    }
                    scripts += Environment.NewLine + string.Format(JsLinkTemplate, js);
                }
            
                var cssResources = resources
                    .Where(name => name.EndsWith(".css") && !name.Contains(".min."))
                    .ToArray();
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var resourceName in cssResources.Where(name => name.Contains(baseNameSrc)))
                {
                    var css = ResourceLoader.GetShortResourceName(assembly, resourceBaseName, resourceName)
                        .Replace(".", "/")
                        .Replace("/css", ".css");
                    if (resources.Contains(resourceName.Replace(".css", ".min.css")))
                    {
                        css = css.Replace(".css", "{.min}.css");
                    }
                    scripts += Environment.NewLine + string.Format(CssLinkTemplate, css);
                }
            }
            
            return text.Replace(ExtensionsInsertPoint, scripts);
        }

    }
}
