namespace ModPlus.Helpers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using ModPlusAPI;
    using ModPlusAPI.Interfaces;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    /* Функция из файла конфигурации читаю в том виде, в каком они там сохранены
     * А вот получение локализованных значений (имя, описание, полное описание)
     * происходит при построении ленты */

    // Вспомогательные методы для загрузки функций
    internal static class LoadFunctionsHelper
    {
        /// <summary>
        /// Список загруженных файлов в виде специального класса для последующего использования при построения ленты и меню
        /// </summary>
        public static List<LoadedFunction> LoadedFunctions = new List<LoadedFunction>();

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
                var modplusInterface = type.GetInterface(nameof(IModPlusFunctionInterface));
                if (modplusInterface != null)
                {
                    if (Activator.CreateInstance(type) is IModPlusFunctionInterface function)
                    {
                        var lf = new LoadedFunction
                        {
                            Name = function.Name,
                            LName = function.LName,
                            Description = function.Description,
                            CanAddToRibbon = function.CanAddToRibbon,
                            SmallIconUrl =
                                $"pack://application:,,,/{loadedFuncAssembly.GetName().FullName};component/Resources/{function.Name}_16x16.png",
                            SmallDarkIconUrl = GetSmallDarkIcon(loadedFuncAssembly, function.Name),
                            BigIconUrl =
                                $"pack://application:,,,/{loadedFuncAssembly.GetName().FullName};component/Resources/{function.Name}_32x32.png",
                            BigDarkIconUrl = GetBigDarkIcon(loadedFuncAssembly, function.Name),
                            AvailProductExternalVersion = VersionData.CurrentCadVersion,
                            FullDescription = function.FullDescription,
                            ToolTipHelpImage = !string.IsNullOrEmpty(function.ToolTipHelpImage)
                            ? $"pack://application:,,,/{loadedFuncAssembly.GetName().FullName};component/Resources/Help/{function.ToolTipHelpImage}"
                            : string.Empty,
                            SubFunctionsNames = function.SubFunctionsNames,
                            SubFunctionsLNames = function.SubFunctionsLames,
                            SubDescriptions = function.SubDescriptions,
                            SubFullDescriptions = function.SubFullDescriptions,
                            SubBigIconsUrl = new List<string>(),
                            SubSmallIconsUrl = new List<string>(),
                            SubHelpImages = new List<string>()
                        };

                        if (function.SubFunctionsNames != null)
                        {
                            foreach (var subFunctionsName in function.SubFunctionsNames)
                            {
                                lf.SubSmallIconsUrl.Add(
                                    $"pack://application:,,,/{loadedFuncAssembly.GetName().FullName};component/Resources/{subFunctionsName}_16x16.png");
                                lf.SubSmallDarkIconsUrl.Add(GetSmallDarkIcon(loadedFuncAssembly, subFunctionsName));
                                lf.SubBigIconsUrl.Add(
                                    $"pack://application:,,,/{loadedFuncAssembly.GetName().FullName};component/Resources/{subFunctionsName}_32x32.png");
                                lf.SubBigDarkIconsUrl.Add(GetBigDarkIcon(loadedFuncAssembly, subFunctionsName));
                            }
                        }

                        if (function.SubHelpImages != null)
                        {
                            foreach (var helpImage in function.SubHelpImages)
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

        public static bool HasStampsPlugin(int colorTheme, out string icon)
        {
            icon = string.Empty;
            try
            {
                if (LoadedFunctions.Any(x => x.Name.Equals("mpStamps")))
                {
                    if (colorTheme == 1)
                    {
                        icon =
                            $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/mpStampFields_16x16.png";
                    }
                    else
                    {
                        icon =
                            $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/mpStampFields_16x16_dark.png";
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

    internal class LoadedFunction
    {
        public string Name { get; set; }

        public string LName { get; set; }

        public string AvailProductExternalVersion { get; set; }

        public string SmallIconUrl { get; set; }

        public string SmallDarkIconUrl { get; set; }

        public string BigIconUrl { get; set; }

        public string BigDarkIconUrl { get; set; }

        public string Description { get; set; }

        public bool CanAddToRibbon { get; set; }

        public string FullDescription { get; set; }

        public string ToolTipHelpImage { get; set; }

        public List<string> SubFunctionsNames { get; set; }

        public List<string> SubFunctionsLNames { get; set; }

        public List<string> SubDescriptions { get; set; }

        public List<string> SubFullDescriptions { get; set; }

        public List<string> SubHelpImages { get; set; }

        public List<string> SubSmallIconsUrl { get; set; }

        public List<string> SubSmallDarkIconsUrl { get; set; }

        public List<string> SubBigIconsUrl { get; set; }

        public List<string> SubBigDarkIconsUrl { get; set; }
    }

    internal static class WPFMenuesHelper
    {
        public static Button AddButton(
            FrameworkElement sourceWindow, string name,
            string lname, string img32, string description, string fullDescription, string helpImage,
            bool statTextWidth)
        {
            var brd = new Border
            {
                Padding = new Thickness(1),
                Margin = new Thickness(1),
                Background = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            try
            {
                var img = new Image
                {
                    Source = new BitmapImage(new Uri(img32, UriKind.RelativeOrAbsolute)),
                    Stretch = Stretch.Uniform,
                    Width = 32,
                    Height = 32
                };
                brd.Child = img;
            }
            catch
            {
                // ignored
            }

            var txt = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Text = lname,
                Margin = new Thickness(3, 0, 1, 0)
            };
            if (statTextWidth)
                txt.Width = 150;
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.Children.Add(brd);
            grid.Children.Add(txt);
            brd.SetValue(Grid.ColumnProperty, 0);
            txt.SetValue(Grid.ColumnProperty, 1);
            var btn = new Button
            {
                Name = name,
                Content = grid,
                ToolTip = AddTooltip(description, fullDescription, helpImage),
                Margin = new Thickness(1),
                Padding = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            btn.Click += CommandButtonClick;

            return btn;
        }

        private static ToolTip AddTooltip(string description, string fullDescription, string imgUri)
        {
            var tt = new ToolTip();
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            var txtDescription = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 500,
                Text = description,
                Margin = new Thickness(2)
            };
            stackPanel.Children.Add(txtDescription);
            var txtFullDescription = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 500,
                Text = fullDescription,
                Margin = new Thickness(2)
            };
            if (!string.IsNullOrEmpty(fullDescription))
                stackPanel.Children.Add(txtFullDescription);
            try
            {
                if (!string.IsNullOrEmpty(imgUri))
                {
                    var img = new Image
                    {
                        Source = new BitmapImage(new Uri(imgUri, UriKind.RelativeOrAbsolute)),
                        Stretch = Stretch.Uniform,
                        MaxWidth = 350
                    };
                    stackPanel.Children.Add(img);
                }
            }
            catch
            {
                // ignored
            }

            tt.Content = stackPanel;
            return tt;
        }

        // Обработка запуска функций        
        private static void CommandButtonClick(object sender, RoutedEventArgs e)
        {
            AcApp.DocumentManager.MdiActiveDocument.SendStringToExecute(
                $"_{((Button)sender).Name} ",
                false, false, false);
        }
    }
}
