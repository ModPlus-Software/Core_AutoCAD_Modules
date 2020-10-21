namespace ModPlus
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Xml.Linq;
    using App;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Runtime;
    using Helpers;
    using MiniPlugins;
    using ModPlusAPI;
    using ModPlusAPI.Interfaces;
    using ModPlusAPI.LicenseServer;
    using ModPlusAPI.UserInfo;
    using ModPlusAPI.Windows;
    using Windows;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    // ReSharper disable once UnusedMember.Global

    /// <inheritdoc />
    public class ModPlus : IExtensionApplication
    {
        private const string LangItem = "AutocadDlls";
        private static bool _dropNextHelpCall; // Flag to tell if the next message from AutoCAD to display it's own help should be ignored
        private static string _currentTooltip; // If not null, this is the HelpTopic of the currently open tooltip. If null, no tooltip is displaying.
        private static bool _quiteLoad;
        private static readonly string[] BaseFiles = {
            "mpBaseInt.dll", "mpMetall.dll", "mpConcrete.dll", "mpMaterial.dll", "mpOther.dll", "mpWood.dll", "mpProductInt.dll"
        };
        
        /// <inheritdoc />
        public void Initialize()
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                // init lang
                if (!Language.Initialize())
                    return;

                // Получим значение переменной "Тихая загрузка" в первую очередь
                _quiteLoad = ModPlusAPI.Variables.QuietLoading;

                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                // Файла конфигурации может не существовать при загрузке плагина!
                // Поэтому все, что связанно с работой с файлом конфигурации должно это учитывать!
                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                var ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
                if (!CheckCadVersion())
                {
                    ed.WriteMessage("\n***************************");
                    ed.WriteMessage($"\n{Language.GetItem(LangItem, "p1")}");
                    ed.WriteMessage($"\n{Language.GetItem(LangItem, "p2")}");
                    ed.WriteMessage($"\n{Language.GetItem(LangItem, "p3")}");
                    ed.WriteMessage("\n***************************");
                    return;
                }

                Statistic.SendPluginStarting("AutoCAD", VersionData.CurrentCadVersion);
                ed.WriteMessage("\n***************************");
                ed.WriteMessage($"\n{Language.GetItem(LangItem, "p4")}");
                if (!_quiteLoad)
                    ed.WriteMessage($"\n{Language.GetItem(LangItem, "p5")}");

                // Принудительная загрузка сборок
                LoadAssemblies(ed);
                if (!_quiteLoad)
                    ed.WriteMessage($"\n{Language.GetItem(LangItem, "p6")}");
                LoadBaseAssemblies(ed);
                if (!_quiteLoad)
                    ed.WriteMessage($"\n{Language.GetItem(LangItem, "p7")}");
                UserConfigFile.InitConfigFile();
                if (!_quiteLoad)
                    ed.WriteMessage($"\n{Language.GetItem(LangItem, "p8")}");
                LoadFunctions(ed);

                // check adaptation
                CheckAdaptation();

                // Строим: ленту, меню, плавающее меню
                // Загрузка ленты
                Autodesk.Windows.ComponentManager.ItemInitialized += ComponentManager_ItemInitialized;
                
                if (ModPlusAPI.Variables.Palette)
                    MpPalette.CreatePalette(false);

                AcApp.SystemVariableChanged += AcAppOnSystemVariableChanged;

                // Загрузка основного меню (с проверкой значения из файла настроек)
                FloatMenuCommand.LoadMainMenu();

                // Загрузка окна Чертежи
                MpDrawingsFunction.LoadMainMenu();

                // Загрузка контекстных меню для мини-функций
                MiniFunctions.LoadUnloadContextMenu();

                // проверка загруженности модуля автообновления
                CheckAutoUpdaterLoaded();

                // Включение иконок для продуктов
                var showProductsIcon = 
                    bool.TryParse(UserConfigFile.GetValue("mpProductInsert", "ShowIcon"), out var b) && b; // false
                if (showProductsIcon)
                    ProductIconFunctions.ShowIcon();

                // license server
                var disableConnectionWithLicenseServerInAutoCad = 
                    ModPlusAPI.Variables.DisableConnectionWithLicenseServerInAutoCAD;

                if (ModPlusAPI.Variables.IsLocalLicenseServerEnable &&
                    !disableConnectionWithLicenseServerInAutoCad)
                    ClientStarter.StartConnection(SupportedProduct.AutoCAD);
                
                if (ModPlusAPI.Variables.IsWebLicenseServerEnable &&
                    !disableConnectionWithLicenseServerInAutoCad)
                    WebLicenseServerClient.Instance.Start(SupportedProduct.AutoCAD);

                // user info
                AuthorizationOnStartup();

                // tooltip hook
                AcApp.PreTranslateMessage += AutoCadMessageHandler;
                Autodesk.Windows.ComponentManager.ToolTipOpened += ComponentManager_ToolTipOpened;
                Autodesk.Windows.ComponentManager.ToolTipClosed += ComponentManager_ToolTipClosed;

                sw.Stop();
                ed.WriteMessage($"\n{Language.GetItem(LangItem, "p9")} {sw.ElapsedMilliseconds}");
                ed.WriteMessage($"\n{Language.GetItem(LangItem, "p10")}");
                ed.WriteMessage("\n***************************");
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
        
        /// <inheritdoc/>
        public void Terminate()
        {
            ClientStarter.StopConnection();
            AcApp.PreTranslateMessage -= AutoCadMessageHandler;
            Autodesk.Windows.ComponentManager.ToolTipOpened -= ComponentManager_ToolTipOpened;
            Autodesk.Windows.ComponentManager.ToolTipClosed -= ComponentManager_ToolTipClosed;
        }

        // проверка соответствия версии AutoCAD
        private static bool CheckCadVersion()
        {
            var cadVer = AcApp.Version;
            return (cadVer.Major + "." + cadVer.Minor).Equals(VersionData.CurrentCadInternalVersion);
        }

        private static void LoadAssemblies(Editor ed)
        {
            try
            {
                foreach (var fileName in Constants.ExtensionsLibraries)
                {
                    var extDll = Path.Combine(Constants.ExtensionsDirectory, fileName);
                    if (!File.Exists(extDll))
                        continue;

                    if (!_quiteLoad)
                        ed.WriteMessage($"\n* {Language.GetItem(LangItem, "p11")} {fileName}");
                    Assembly.LoadFrom(extDll);
                }
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private static void LoadBaseAssemblies(Editor ed)
        {
            try
            {
                var directory = Path.Combine(Constants.CurrentDirectory, "Data");
                if (Directory.Exists(directory))
                {
                    foreach (var baseFile in BaseFiles)
                    {
                        var file = Path.Combine(directory, baseFile);
                        if (File.Exists(file))
                        {
                            if (!_quiteLoad)
                                ed.WriteMessage($"\n* {Language.GetItem(LangItem, "p12")} {baseFile}");
                            Assembly.LoadFrom(file);
                        }
                        else
                            if (!_quiteLoad)
                        {
                            ed.WriteMessage($"\n* {Language.GetItem(LangItem, "p13")} {baseFile}");
                        }
                    }
                }
                else
                {
                    if (!_quiteLoad)
                        ed.WriteMessage($"\n{Language.GetItem(LangItem, "p14")}");
                }
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private static void LoadFunctions(Editor ed)
        {
            try
            {
                var functionsKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("ModPlus\\Functions");
                if (functionsKey == null)
                    return;
                using (functionsKey)
                {
                    foreach (var functionKeyName in functionsKey.GetSubKeyNames())
                    {
                        var functionKey = functionsKey.OpenSubKey(functionKeyName);
                        if (functionKey == null)
                            continue;
                        foreach (var availPrVersKeyName in functionKey.GetSubKeyNames())
                        {
                            // Если версия продукта не совпадает, то пропускаю
                            if (!availPrVersKeyName.Equals(VersionData.CurrentCadVersion))
                                continue;
                            var availProductVersionKey = functionKey.OpenSubKey(availPrVersKeyName);
                            if (availProductVersionKey == null)
                                continue;

                            // беру свойства функции из реестра
                            var file = availProductVersionKey.GetValue("File") as string;
                            var onOff = availProductVersionKey.GetValue("OnOff") as string;
                            var productFor = availProductVersionKey.GetValue("ProductFor") as string;
                            if (string.IsNullOrEmpty(onOff) || string.IsNullOrEmpty(productFor))
                                continue;
                            if (!productFor.Equals("AutoCAD"))
                                continue;
                            var isOn = !bool.TryParse(onOff, out var b) || b; // default - true

                            // Если "Продукт для" подходит, файл существует и функция включена - гружу
                            if (isOn)
                            {
                                if (!string.IsNullOrEmpty(file) && File.Exists(file))
                                {
                                    // load
                                    if (!_quiteLoad)
                                        ed.WriteMessage($"\n* {Language.GetItem(LangItem, "p15")} {functionKeyName}");
                                    var localFuncAssembly = Assembly.LoadFrom(file);
                                    LoadFunctionsHelper.GetDataFromFunctionInterface(localFuncAssembly);
                                }
                                else
                                {
                                    var foundedFile = LoadFunctionsHelper.FindFile(functionKeyName);
                                    if (!string.IsNullOrEmpty(foundedFile) && File.Exists(foundedFile))
                                    {
                                        if (!_quiteLoad)
                                            ed.WriteMessage($"\n* {Language.GetItem(LangItem, "p15")} {functionKeyName}");
                                        var localFuncAssembly = Assembly.LoadFrom(foundedFile);
                                        LoadFunctionsHelper.GetDataFromFunctionInterface(localFuncAssembly);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        /// <summary>
        /// Обработчик события, который проверяет, что построилась лента
        /// И когда она построилась - уже грузим свою вкладку, если надо
        /// </summary>
        private static void ComponentManager_ItemInitialized(object sender, Autodesk.Windows.RibbonItemEventArgs e)
        {
            if (Autodesk.Windows.ComponentManager.Ribbon == null)
                return;
            
            Autodesk.Windows.ComponentManager.Ribbon.BackgroundRenderFinished += RibbonOnBackgroundRenderFinished;
            
            if (ModPlusAPI.Variables.Ribbon)
                RibbonBuilder.BuildRibbon();
            else
                RibbonBuilder.RemoveRibbon();

            Autodesk.Windows.ComponentManager.ItemInitialized -= ComponentManager_ItemInitialized;
        }

        private static void RibbonOnBackgroundRenderFinished(object sender, EventArgs e)
        {
            if (ModPlusAPI.Variables.Ribbon)
                RibbonBuilder.BuildRibbon();
            else
                RibbonBuilder.RemoveRibbon();
        }

        private void AcAppOnSystemVariableChanged(object sender, SystemVariableChangedEventArgs e)
        {
            if (e.Name.Equals("WSCURRENT"))
            {
                if (ModPlusAPI.Variables.Palette)
                    MpPalette.CreatePalette(false);
            }
        }

        /// <summary>
        /// Проверка загруженности модуля автообновления
        /// </summary>
        private static void CheckAutoUpdaterLoaded()
        {
            try
            {
                var loadWithWindows = !bool.TryParse(RegistryUtils.GetValue("AutoUpdater", "LoadWithWindows"), out bool b) || b;
                if (loadWithWindows)
                {
                    // Если "грузить с ОС", то проверяем, что модуль запущен
                    // если не запущен - запускаем
                    var isOpen = Process.GetProcesses().Any(t => t.ProcessName == "mpAutoUpdater");
                    if (!isOpen)
                    {
                        var fileToStart = Path.Combine(Constants.CurrentDirectory, "mpAutoUpdater.exe");
                        if (File.Exists(fileToStart))
                        {
                            Process.Start(fileToStart);
                        }
                    }
                }
            }
            catch (System.Exception exception)
            {
                Statistic.SendException(exception);
            }
        }

        private static async void CheckAdaptation()
        {
            var confCuiXel = ModPlusAPI.RegistryData.Adaptation.GetCuiAsXElement("AutoCAD");

            // Проходим по группам
            if (confCuiXel == null || confCuiXel.IsEmpty)
            {
                if (await ModPlusAPI.Web.Connection.HasAllConnectionAsync(3))
                {
                    // Грузим файл
                    try
                    {
                        var url = "http://www.modplus.org/Downloads/StandardCUI.xml";
                        if (string.IsNullOrEmpty(url))
                            return;
                        string xmlStr;
                        using (var wc = new WebClientWithTimeout { Proxy = ModPlusAPI.Web.Proxy.GetWebProxy() })
                            xmlStr = wc.DownloadString(url);
                        var xmlDocument = XElement.Parse(xmlStr);

                        ModPlusAPI.RegistryData.Adaptation.SaveCuiFromXElement("AutoCAD", xmlDocument);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        private static async void AuthorizationOnStartup()
        {
            try
            {
                await UserInfoService.GetUserInfoAsync();
                var userInfo = UserInfoService.GetUserInfoResponseFromHash();
                if (userInfo == null) 
                    return;

                if (!userInfo.IsLocalData && !await ModPlusAPI.Web.Connection.HasAllConnectionAsync(3))
                {
                    ModPlusAPI.Variables.UserInfoHash = string.Empty;
                }
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        #region ToolTip Hook

        public enum WndMsg
        {
            WM_ACAD_HELP = 0x4D,
            WM_KEYDOWN = 0x100,
        }

        public enum WndKey
        {
            VK_F1 = 0x70,
        }

        private void AutoCadMessageHandler(object sender, PreTranslateMessageEventArgs e)
        {
            if (e.Message.message == (int)WndMsg.WM_KEYDOWN)
            {
                if ((int)e.Message.wParam == (int)WndKey.VK_F1)
                {
                    // F1 pressed
                    if (_currentTooltip != null && _currentTooltip.Length > 8 && _currentTooltip.StartsWith("https://modplus.org/"))
                    {
                        // Another implementation could be to look up the help topic in an index file matching it to URLs.
                        _dropNextHelpCall = true; // Even though we don't forward this F1 keypress, AutoCAD sends a message to itself to open the AutoCAD help file
                        object nomutt = AcApp.GetSystemVariable("NOMUTT");
                        string cmd = $"._BROWSER {_currentTooltip} _NOMUTT {nomutt} ";
                        AcApp.SetSystemVariable("NOMUTT", 1);
                        AcApp.DocumentManager.MdiActiveDocument.SendStringToExecute(cmd, true, false, false);
                        e.Handled = true;
                    }
                }
            }
            else if (e.Message.message == (int)WndMsg.WM_ACAD_HELP && _dropNextHelpCall)
            {
                // Seems this is the message AutoCAD generates itself to open the help file. Drop this if help was called from a ribbon tooltip.
                _dropNextHelpCall = false; // Reset state of help calls
                e.Handled = true; // Stop this message from being passed on to AutoCAD
            }
        }

        // AutoCAD event handlers to detect if a tooltip is open or not
        private static void ComponentManager_ToolTipOpened(object sender, EventArgs e)
        {
            Autodesk.Internal.Windows.ToolTip tt = sender as Autodesk.Internal.Windows.ToolTip;
            
            if (tt == null)
                return;

            Autodesk.Windows.RibbonToolTip rtt = tt.Content as Autodesk.Windows.RibbonToolTip;
            
            if (rtt == null)
                _currentTooltip = tt.HelpTopic;
            else
                _currentTooltip = rtt.HelpTopic;
        }

        private static void ComponentManager_ToolTipClosed(object sender, EventArgs e)
        {
            _currentTooltip = null;
        }

        #endregion

        /// <inheritdoc />
        internal class WebClientWithTimeout : WebClient
        {
            /// <inheritdoc/>
            protected override WebRequest GetWebRequest(Uri uri)
            {
                var w = base.GetWebRequest(uri);
                if (w != null) 
                    w.Timeout = 3000;
                return w;
            }
        }
    }
}