namespace ModPlus.Helpers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using ModPlusAPI;
    using ModPlusAPI.Abstractions;

    /* Плагин из файла конфигурации читаю в том виде, в каком они там сохранены
     * А вот получение локализованных значений (имя, описание, полное описание)
     * происходит при построении ленты */

    /// <summary>
    /// Вспомогательные методы для загрузки плагинов
    /// </summary>
    internal static class LoadPluginsHelper
    {
        /// <summary>
        /// Список загруженных файлов в виде специального класса для последующего использования при построения ленты и меню
        /// </summary>
        public static List<LoadedPlugin> LoadedFunctions = new List<LoadedPlugin>();

        /// <summary>
        /// Чтение данных из интерфейса функции
        /// </summary>
        /// <param name="loadedFuncAssembly">Load assembly</param>
        public static void GetDataFromFunctionInterface(Assembly loadedFuncAssembly)
        {
            // Есть два интерфейса - старый и новый. Нужно учесть оба
            var types = GetLoadableTypes(loadedFuncAssembly);
            foreach (var type in types)
            {
                var i = type.GetInterface(nameof(IModPlusPlugin));
                if (i != null)
                {
                    if (Activator.CreateInstance(type) is IModPlusPlugin plugin)
                    {
                        var lf = new LoadedPlugin
                        {
                            Name = plugin.Name,
                            LName = plugin.LName,
                            Description = plugin.Description,
                            CanAddToRibbon = plugin.CanAddToRibbon,
                            SmallIconUrl =
                                $"pack://application:,,,/{loadedFuncAssembly.GetName().FullName};component/Resources/{plugin.Name}_16x16.png",
                            SmallDarkIconUrl = GetSmallDarkIcon(loadedFuncAssembly, plugin.Name),
                            BigIconUrl =
                                $"pack://application:,,,/{loadedFuncAssembly.GetName().FullName};component/Resources/{plugin.Name}_32x32.png",
                            BigDarkIconUrl = GetBigDarkIcon(loadedFuncAssembly, plugin.Name),
                            AvailProductExternalVersion = VersionData.CurrentCadVersion,
                            FullDescription = plugin.FullDescription,
                            ToolTipHelpImage = !string.IsNullOrEmpty(plugin.ToolTipHelpImage)
                            ? $"pack://application:,,,/{loadedFuncAssembly.GetName().FullName};component/Resources/Help/{plugin.ToolTipHelpImage}"
                            : string.Empty,
                            SubPluginsNames = plugin.SubPluginsNames,
                            SubPluginsLNames = plugin.SubPluginsLNames,
                            SubDescriptions = plugin.SubDescriptions,
                            SubFullDescriptions = plugin.SubFullDescriptions,
                            SubBigIconsUrl = new List<string>(),
                            SubSmallIconsUrl = new List<string>(),
                            SubHelpImages = new List<string>()
                        };

                        if (plugin.SubPluginsNames != null)
                        {
                            foreach (var subFunctionsName in plugin.SubPluginsNames)
                            {
                                lf.SubSmallIconsUrl.Add(
                                    $"pack://application:,,,/{loadedFuncAssembly.GetName().FullName};component/Resources/{subFunctionsName}_16x16.png");
                                lf.SubSmallDarkIconsUrl.Add(GetSmallDarkIcon(loadedFuncAssembly, subFunctionsName));
                                lf.SubBigIconsUrl.Add(
                                    $"pack://application:,,,/{loadedFuncAssembly.GetName().FullName};component/Resources/{subFunctionsName}_32x32.png");
                                lf.SubBigDarkIconsUrl.Add(GetBigDarkIcon(loadedFuncAssembly, subFunctionsName));
                            }
                        }

                        if (plugin.SubHelpImages != null)
                        {
                            foreach (var helpImage in plugin.SubHelpImages)
                            {
                                lf.SubHelpImages.Add(
                                    !string.IsNullOrEmpty(helpImage)
                                    ? $"pack://application:,,,/{loadedFuncAssembly.GetName().FullName};component/Resources/Help/{helpImage}"
                                    : string.Empty);
                            }
                        }

                        LoadedFunctions.Add(lf);
                    }

                    break;
                }
            }
        }

        private static string GetSmallDarkIcon(Assembly funcAssembly, string funcName)
        {
            var iconUri = string.Empty;
            var iconName = $"{funcName}_16x16_dark.png";
            if (ResourceExists(funcAssembly, iconName))
                iconUri = $"pack://application:,,,/{funcAssembly.GetName().FullName};component/Resources/{iconName}";
            return iconUri;
        }

        private static string GetBigDarkIcon(Assembly funcAssembly, string funcName)
        {
            var iconUri = string.Empty;
            var iconName = $"{funcName}_32x32_dark.png";
            if (ResourceExists(funcAssembly, iconName))
                iconUri = $"pack://application:,,,/{funcAssembly.GetName().FullName};component/Resources/{iconName}";
            return iconUri;
        }

        private static bool ResourceExists(Assembly assembly, string resourcePath)
        {
            return GetResourcePaths(assembly).Any(rk => rk.ToLower().Contains(resourcePath.ToLower()));
        }

        private static IEnumerable<string> GetResourcePaths(Assembly assembly)
        {
            var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
            var resourceName = $"{assembly.GetName().Name}.g";
            var resourceManager = new ResourceManager(resourceName, assembly);
            var resKeys = new List<string>();
            try
            {
                var resourceSet = resourceManager.GetResourceSet(culture, true, true);
                foreach (DictionaryEntry resource in resourceSet)
                    resKeys.Add(resource.Key.ToString());
            }
            finally
            {
                resourceManager.ReleaseAllResources();
            }

            return resKeys;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        /// <summary>
        /// Поиск файла функции, если в файле конфигурации вдруг нет атрибута
        /// </summary>
        /// <param name="pluginName">Plugin name</param>
        /// <returns></returns>
        public static string FindFile(string pluginName)
        {
            var fileName = string.Empty;

            var funcDir = Path.Combine(Constants.CurrentDirectory, "Functions", pluginName);
            if (Directory.Exists(funcDir))
            {
                foreach (var file in Directory.GetFiles(funcDir, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Name.Equals($"{pluginName}_{VersionData.CurrentCadVersion}.dll"))
                    {
                        fileName = file;
                        break;
                    }
                }
            }

            return fileName;
        }

        /// <summary>
        /// Имеется ли плагин "Штампы"
        /// </summary>
        /// <param name="colorTheme">Тема оформления</param>
        /// <param name="fieldsIcon">Иконка плагина "Поля"</param>
        /// <param name="signaturesIcon">Иконка плагина "Подписи"</param>
        public static bool HasStampsPlugin(int colorTheme, out string fieldsIcon, out string signaturesIcon)
        {
            fieldsIcon = string.Empty;
            signaturesIcon = string.Empty;
            try
            {
                if (LoadedFunctions.Any(x => x.Name.Equals("mpStamps")))
                {
                    if (colorTheme == 1)
                    {
                        fieldsIcon =
                            $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/mpStampFields_16x16.png";
                        signaturesIcon =
                            $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/mpSignatures_16x16.png";
                    }
                    else
                    {
                        fieldsIcon =
                            $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/mpStampFields_16x16_dark.png";
                        signaturesIcon =
                            $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/mpSignatures_16x16_dark.png";
                    }

                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Имеется ли плагин "Штампы"
        /// </summary>
        public static bool HasStampsPlugin()
        {
            try
            {
                return LoadedFunctions.Any(x => x.Name.Equals("mpStamps"));
            }
            catch
            {
                return false;
            }
        }
    }
}
