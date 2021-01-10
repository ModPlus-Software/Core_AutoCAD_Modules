namespace ModPlus.App
{
    using System;
    using System.Linq;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.Windows;
    using Helpers;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
    using Orientation = System.Windows.Controls.Orientation;
    using RibbonPanelSource = Autodesk.Windows.RibbonPanelSource;
    using RibbonRowPanel = Autodesk.Windows.RibbonRowPanel;
    using RibbonSplitButton = Autodesk.Windows.RibbonSplitButton;
    using RibbonSplitButtonListStyle = Autodesk.Windows.RibbonSplitButtonListStyle;

    internal static class RibbonBuilder
    {
        private const string LangItem = "AutocadDlls";
        private static bool _wasActive;
        private static int _colorTheme = 1;

        public static void BuildRibbon(bool setActive = false)
        {
            if (!IsLoaded())
            {
                GetColorTheme();
                CreateRibbon(setActive);
                AcApp.SystemVariableChanged -= AcadApp_SystemVariableChanged;
                AcApp.SystemVariableChanged += AcadApp_SystemVariableChanged;
            }
        }

        private static bool IsLoaded()
        {
            var loaded = false;
            var ribbonControl = ComponentManager.Ribbon;
            foreach (var tab in ribbonControl.Tabs)
            {
                if (tab.Id.Equals("ModPlus_ID") && tab.Title.Equals("ModPlus"))
                {
                    loaded = true;
                    break;
                }
            }

            return loaded;
        }

        private static bool IsActive()
        {
            var ribbonControl = ComponentManager.Ribbon;
            foreach (var tab in ribbonControl.Tabs)
            {
                if (tab.Id.Equals("ModPlus_ID") && tab.Title.Equals("ModPlus"))
                    return tab.IsActive;
            }

            return false;
        }

        public static void RemoveRibbon()
        {
            try
            {
                if (IsLoaded())
                {
                    var ribbonControl = ComponentManager.Ribbon;
                    foreach (var tab in ribbonControl.Tabs.Where(
                        tab => tab.Id.Equals("ModPlus_ID") && tab.Title.Equals("ModPlus")))
                    {
                        ribbonControl.Tabs.Remove(tab);
                        AcApp.SystemVariableChanged -= AcadApp_SystemVariableChanged;
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private static void AcadApp_SystemVariableChanged(object sender, SystemVariableChangedEventArgs e)
        {
            if (e.Name.Equals("WSCURRENT"))
                BuildRibbon();
            if (e.Name.Equals("COLORTHEME"))
            {
                _wasActive = IsActive();
                RemoveRibbon();
                BuildRibbon();
            }
        }

        private static void GetColorTheme()
        {
            try
            {
                var sv = AcApp.GetSystemVariable("COLORTHEME").ToString();
                if (int.TryParse(sv, out var i))
                    _colorTheme = i;
                else 
                    _colorTheme = 1; // light
            }
            catch
            {
                _colorTheme = 1;
            }
        }

        private static void CreateRibbon(bool setActive = false)
        {
            try
            {
                var ribCntrl = ComponentManager.Ribbon;

                // add the tab
                var ribTab = new RibbonTab { Title = "ModPlus", Id = "ModPlus_ID" };
                ribCntrl.Tabs.Add(ribTab);

                // add content
                AddPanels(ribTab);

                // add help panel
                AddHelpPanel(ribTab);

                ribCntrl.UpdateLayout();
                if (_wasActive || setActive)
                    ribTab.IsActive = true;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private static void AddPanels(RibbonTab ribTab)
        {
            try
            {
                var confCuiXel = ModPlusAPI.RegistryData.Adaptation.GetCuiAsXElement("AutoCAD");

                // Проходим по группам
                if (confCuiXel != null)
                {
                    foreach (var group in confCuiXel.Elements("Group"))
                    {
                        if (group.Attribute("GroupName") == null)
                            continue;

                        // create the panel source
                        var ribSourcePanel = new RibbonPanelSource
                        {
                            Title = Language.TryGetCuiLocalGroupName(group.Attribute("GroupName")?.Value)
                        };

                        // now the panel
                        var ribPanel = new RibbonPanel
                        {
                            Source = ribSourcePanel
                        };
                        ribTab.Panels.Add(ribPanel);
                        var ribRowPanel = new RibbonRowPanel();

                        // Вводим спец.счетчик, который потребуется для разбивки по строкам
                        var nr = 0;
                        var hasFunctions = false;

                        // Если последняя функция в группе была 32х32
                        var lastWasBig = false;

                        // Проходим по функциям группы
                        foreach (var func in group.Elements("Function"))
                        {
                            var fNameAttr = func.Attribute("Name")?.Value;
                            if (string.IsNullOrEmpty(fNameAttr))
                                continue;
                            if (LoadPluginsHelper.LoadedFunctions.Any(x => x.Name.Equals(fNameAttr)))
                            {
                                var loadedFunction = LoadPluginsHelper.LoadedFunctions.FirstOrDefault(x => x.Name.Equals(fNameAttr));
                                if (loadedFunction == null)
                                    continue;
                                hasFunctions = true;
                                if (nr == 0)
                                    ribRowPanel = new RibbonRowPanel();

                                // В зависимости от размера
                                var btnSizeAttr = func.Attribute("WH")?.Value;
                                if (string.IsNullOrEmpty(btnSizeAttr))
                                    continue;
                                #region 16
                                if (btnSizeAttr.Equals("16"))
                                {
                                    lastWasBig = false;

                                    // Если функция имеет "подфункции", то делаем SplitButton
                                    if (func.Elements("SubFunction").Any())
                                    {
                                        // Создаем SplitButton
                                        var risSplitBtn = new RibbonSplitButton
                                        {
                                            Text = "RibbonSplitButton",
                                            Orientation = Orientation.Horizontal,
                                            Size = RibbonItemSize.Standard,
                                            ShowImage = true,
                                            ShowText = false,
                                            ListButtonStyle = Autodesk.Private.Windows.RibbonListButtonStyle.SplitButton,
                                            ResizeStyle = RibbonItemResizeStyles.NoResize,
                                            ListStyle = RibbonSplitButtonListStyle.List
                                        };

                                        // Добавляем в него первую функцию, которую делаем основной
                                        var ribBtn = RibbonHelpers.AddButton(
                                            loadedFunction.Name,
                                            Language.GetFunctionLocalName(loadedFunction.Name, loadedFunction.LName),
                                            GetSmallIcon(loadedFunction),
                                            GetBigIcon(loadedFunction),
                                            Language.GetFunctionShortDescription(loadedFunction.Name, loadedFunction.Description),
                                            Orientation.Horizontal,
                                            Language.GetFunctionFullDescription(loadedFunction.Name, loadedFunction.FullDescription),
                                            loadedFunction.ToolTipHelpImage);

                                        risSplitBtn.Items.Add(ribBtn);
                                        risSplitBtn.Current = ribBtn;

                                        // Затем добавляем подфункции
                                        foreach (var subFunc in func.Elements("SubFunction"))
                                        {
                                            if (LoadPluginsHelper.LoadedFunctions.Any(x => x.Name.Equals(subFunc.Attribute("Name")?.Value)))
                                            {
                                                var loadedSubFunction = LoadPluginsHelper.LoadedFunctions
                                                    .FirstOrDefault(x => x.Name.Equals(subFunc.Attribute("Name")?.Value));
                                                if (loadedSubFunction == null)
                                                    continue;
                                                risSplitBtn.Items.Add(RibbonHelpers.AddButton(
                                                    loadedSubFunction.Name,
                                                    Language.GetFunctionLocalName(loadedSubFunction.Name, loadedSubFunction.LName),
                                                    GetSmallIcon(loadedSubFunction),
                                                    GetBigIcon(loadedSubFunction),
                                                    Language.GetFunctionShortDescription(loadedSubFunction.Name, loadedSubFunction.Description),
                                                    Orientation.Horizontal,
                                                    Language.GetFunctionFullDescription(loadedSubFunction.Name, loadedSubFunction.FullDescription),
                                                    loadedSubFunction.ToolTipHelpImage));
                                            }
                                        }

                                        ribRowPanel.Items.Add(risSplitBtn);
                                    }

                                    // Если в конфигурации меню не прописано наличие подфункций, то проверяем, что они могут быть в самой функции
                                    else if (loadedFunction.SubPluginsNames.Any())
                                    {
                                        // Создаем SplitButton
                                        var risSplitBtn = new RibbonSplitButton
                                        {
                                            Text = "RibbonSplitButton",
                                            Orientation = Orientation.Horizontal,
                                            Size = RibbonItemSize.Standard,
                                            ShowImage = true,
                                            ShowText = false,
                                            ListButtonStyle = Autodesk.Private.Windows.RibbonListButtonStyle.SplitButton,
                                            ResizeStyle = RibbonItemResizeStyles.NoResize,
                                            ListStyle = RibbonSplitButtonListStyle.List
                                        };

                                        // Добавляем в него первую функцию, которую делаем основной
                                        var ribBtn = RibbonHelpers.AddButton(
                                            loadedFunction.Name,
                                            Language.GetFunctionLocalName(loadedFunction.Name, loadedFunction.LName),
                                            GetSmallIcon(loadedFunction),
                                            GetBigIcon(loadedFunction),
                                            Language.GetFunctionShortDescription(loadedFunction.Name, loadedFunction.Description),
                                            Orientation.Horizontal,
                                            Language.GetFunctionFullDescription(loadedFunction.Name, loadedFunction.FullDescription),
                                            loadedFunction.ToolTipHelpImage);
                                        risSplitBtn.Items.Add(ribBtn);
                                        risSplitBtn.Current = ribBtn;

                                        // Затем добавляем подфункции
                                        for (int i = 0; i < loadedFunction.SubPluginsNames.Count; i++)
                                        {
                                            risSplitBtn.Items.Add(RibbonHelpers.AddButton(
                                                loadedFunction.SubPluginsNames[i],
                                                Language.GetFunctionLocalName(loadedFunction.Name, loadedFunction.SubPluginsLNames[i], i + 1),
                                                GetSmallIconForSubFunction(loadedFunction, i),
                                                GetBigIconForSubFunction(loadedFunction, i),
                                                Language.GetFunctionShortDescription(loadedFunction.Name, loadedFunction.SubDescriptions[i], i + 1),
                                                Orientation.Horizontal,
                                                Language.GetFunctionFullDescription(loadedFunction.Name, loadedFunction.SubFullDescriptions[i], i + 1),
                                                loadedFunction.SubHelpImages[i]));
                                        }

                                        ribRowPanel.Items.Add(risSplitBtn);
                                    }

                                    // Иначе просто добавляем маленькую кнопку
                                    else
                                    {
                                        ribRowPanel.Items.Add(RibbonHelpers.AddSmallButton(
                                            loadedFunction.Name,
                                            Language.GetFunctionLocalName(loadedFunction.Name, loadedFunction.LName),
                                            GetSmallIcon(loadedFunction),
                                            Language.GetFunctionShortDescription(loadedFunction.Name, loadedFunction.Description),
                                            Language.GetFunctionFullDescription(loadedFunction.Name, loadedFunction.FullDescription),
                                            loadedFunction.ToolTipHelpImage));
                                    }

                                    nr++;
                                    if (nr == 3 | nr == 6)
                                        ribRowPanel.Items.Add(new RibbonRowBreak());
                                    if (nr == 9)
                                    {
                                        ribSourcePanel.Items.Add(ribRowPanel);
                                        nr = 0;
                                    }
                                }
                                #endregion

                                // Если кнопка большая, то добавляем ее в отдельную Row Panel
                                #region 32
                                if (btnSizeAttr.Equals("32"))
                                {
                                    lastWasBig = true;
                                    if (ribRowPanel.Items.Count > 0)
                                    {
                                        ribSourcePanel.Items.Add(ribRowPanel);
                                        nr = 0;
                                    }

                                    ribRowPanel = new RibbonRowPanel();

                                    // Если функция имеет "подфункции", то делаем SplitButton
                                    if (func.Elements("SubFunction").Any())
                                    {
                                        // Создаем SplitButton
                                        var risSplitBtn = new RibbonSplitButton
                                        {
                                            Text = "RibbonSplitButton",
                                            Orientation = Orientation.Vertical,
                                            Size = RibbonItemSize.Large,
                                            ShowImage = true,
                                            ShowText = true,
                                            ListButtonStyle = Autodesk.Private.Windows.RibbonListButtonStyle.SplitButton,
                                            ResizeStyle = RibbonItemResizeStyles.NoResize,
                                            ListStyle = RibbonSplitButtonListStyle.List
                                        };

                                        // Добавляем в него первую функцию, которую делаем основной
                                        var ribBtn = RibbonHelpers.AddBigButton(
                                            loadedFunction.Name,
                                            Language.GetFunctionLocalName(loadedFunction.Name, loadedFunction.LName),
                                            GetBigIcon(loadedFunction),
                                            Language.GetFunctionShortDescription(loadedFunction.Name, loadedFunction.Description),
                                            Orientation.Horizontal,
                                            Language.GetFunctionFullDescription(loadedFunction.Name, loadedFunction.FullDescription),
                                            loadedFunction.ToolTipHelpImage);
                                        risSplitBtn.Items.Add(ribBtn);
                                        risSplitBtn.Current = ribBtn;

                                        // Затем добавляем подфункции
                                        foreach (var subFunc in func.Elements("SubFunction"))
                                        {
                                            if (LoadPluginsHelper.LoadedFunctions.Any(x => x.Name.Equals(subFunc.Attribute("Name")?.Value)))
                                            {
                                                var loadedSubFunction = LoadPluginsHelper.LoadedFunctions.FirstOrDefault(x => x.Name.Equals(subFunc.Attribute("Name")?.Value));
                                                if (loadedSubFunction == null)
                                                    continue;
                                                risSplitBtn.Items.Add(RibbonHelpers.AddBigButton(
                                                    loadedSubFunction.Name,
                                                    Language.GetFunctionLocalName(loadedSubFunction.Name, loadedSubFunction.LName),
                                                    GetBigIcon(loadedSubFunction),
                                                    Language.GetFunctionShortDescription(loadedSubFunction.Name, loadedSubFunction.Description),
                                                    Orientation.Horizontal,
                                                    Language.GetFunctionFullDescription(loadedSubFunction.Name, loadedSubFunction.FullDescription),
                                                    loadedSubFunction.ToolTipHelpImage));
                                            }
                                        }

                                        ribRowPanel.Items.Add(risSplitBtn);
                                    }

                                    // Если в конфигурации меню не прописано наличие подфункций, то проверяем, что они могут быть в самой функции
                                    else if (loadedFunction.SubPluginsNames.Any())
                                    {
                                        // Создаем SplitButton
                                        var risSplitBtn = new RibbonSplitButton
                                        {
                                            Text = "RibbonSplitButton",
                                            Orientation = Orientation.Vertical,
                                            Size = RibbonItemSize.Large,
                                            ShowImage = true,
                                            ShowText = true,
                                            ListButtonStyle = Autodesk.Private.Windows.RibbonListButtonStyle.SplitButton,
                                            ResizeStyle = RibbonItemResizeStyles.NoResize,
                                            ListStyle = RibbonSplitButtonListStyle.List
                                        };

                                        // Добавляем в него первую функцию, которую делаем основной
                                        var ribBtn = RibbonHelpers.AddBigButton(
                                            loadedFunction.Name,
                                            Language.GetFunctionLocalName(loadedFunction.Name, loadedFunction.LName),
                                            GetBigIcon(loadedFunction),
                                            Language.GetFunctionShortDescription(loadedFunction.Name, loadedFunction.Description),
                                            Orientation.Horizontal,
                                            Language.GetFunctionFullDescription(loadedFunction.Name, loadedFunction.FullDescription),
                                            loadedFunction.ToolTipHelpImage);
                                        risSplitBtn.Items.Add(ribBtn);
                                        risSplitBtn.Current = ribBtn;

                                        // Затем добавляем подфункции
                                        for (int i = 0; i < loadedFunction.SubPluginsNames.Count; i++)
                                        {
                                            risSplitBtn.Items.Add(RibbonHelpers.AddBigButton(
                                                loadedFunction.SubPluginsNames[i],
                                                Language.GetFunctionLocalName(loadedFunction.Name, loadedFunction.SubPluginsLNames[i], i + 1),
                                                GetBigIconForSubFunction(loadedFunction, i),
                                                Language.GetFunctionShortDescription(loadedFunction.Name, loadedFunction.SubDescriptions[i], i + 1),
                                                Orientation.Horizontal,
                                                Language.GetFunctionFullDescription(loadedFunction.Name, loadedFunction.SubFullDescriptions[i], i + 1),
                                                loadedFunction.SubHelpImages[i]));
                                        }

                                        ribRowPanel.Items.Add(risSplitBtn);
                                    }

                                    // Иначе просто добавляем большую кнопку
                                    else
                                    {
                                        RibbonButton ribbonButton = RibbonHelpers.AddBigButton(
                                            loadedFunction.Name,
                                            Language.GetFunctionLocalName(loadedFunction.Name, loadedFunction.LName),
                                            GetBigIcon(loadedFunction),
                                            Language.GetFunctionShortDescription(loadedFunction.Name, loadedFunction.Description),
                                            Orientation.Vertical,
                                            Language.GetFunctionFullDescription(loadedFunction.Name, loadedFunction.FullDescription),
                                            loadedFunction.ToolTipHelpImage);
                                        ribRowPanel.Items.Add(ribbonButton);
                                    }

                                    ribSourcePanel.Items.Add(ribRowPanel);
                                }
                                #endregion
                            }
                        }// foreach functions

                        if (ribRowPanel.Items.Any() & !lastWasBig)
                        {
                            ribSourcePanel.Items.Add(ribRowPanel);
                        }

                        // Если в группе нет функций(например отключены), то не добавляем эту группу
                        if (!hasFunctions)
                            ribTab.Panels.Remove(ribPanel);
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private static string GetSmallIcon(LoadedPlugin loadedPlugin)
        {
            if (_colorTheme == 0)
            {
                if (!string.IsNullOrEmpty(loadedPlugin.SmallDarkIconUrl))
                    return loadedPlugin.SmallDarkIconUrl;
            }

            return loadedPlugin.SmallIconUrl;
        }

        private static string GetBigIcon(LoadedPlugin loadedPlugin)
        {
            if (_colorTheme == 0)
            {
                if (!string.IsNullOrEmpty(loadedPlugin.BigDarkIconUrl))
                    return loadedPlugin.BigDarkIconUrl;
            }

            return loadedPlugin.BigIconUrl;
        }

        private static string GetSmallIconForSubFunction(LoadedPlugin loadedPlugin, int i)
        {
            if (_colorTheme == 0)
            {
                if (!string.IsNullOrEmpty(loadedPlugin.SubSmallDarkIconsUrl[i]))
                    return loadedPlugin.SubSmallDarkIconsUrl[i];
            }

            return loadedPlugin.SubSmallIconsUrl[i];
        }

        private static string GetBigIconForSubFunction(LoadedPlugin loadedPlugin, int i)
        {
            if (_colorTheme == 0)
            {
                if (!string.IsNullOrEmpty(loadedPlugin.SubBigDarkIconsUrl[i]))
                    return loadedPlugin.SubBigDarkIconsUrl[i];
            }

            return loadedPlugin.SubBigIconsUrl[i];
        }

        private static void AddHelpPanel(RibbonTab ribTab)
        {
            var ribSourcePanel = new RibbonPanelSource
            {
                Title = "ModPlus"
            };

            var ribPanel = new RibbonPanel
            {
                Source = ribSourcePanel
            };
            ribTab.Panels.Add(ribPanel);

            var ribRowPanel = new RibbonRowPanel();

            ribRowPanel.Items.Add(
                RibbonHelpers.AddBigButton(
                    "mpUserInfo",
                    Language.GetItem(LangItem, "h56"),
                    _colorTheme == 1
                        ? $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/UserInfo_32x32.png"
                        : $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/UserInfo_32x32_dark.png",
                    Language.GetItem(LangItem, "h56"),
                    Orientation.Vertical,
                    string.Empty,
                    string.Empty,
                    "help/userinfo"));

            ribRowPanel.Items.Add(
                RibbonHelpers.AddBigButton(
                "mpSettings",
                Language.GetItem(LangItem, "h12"),
                _colorTheme == 1
                    ? $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/HelpBt.png"
                    : $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/HelpBt_dark.png",
                Language.GetItem(LangItem, "h41"),
                Orientation.Vertical,
                Language.GetItem(LangItem, "h42"),
                string.Empty,
                "help/mpsettings"));
            ribSourcePanel.Items.Add(ribRowPanel);

            ribRowPanel = new RibbonRowPanel();
            if (LoadPluginsHelper.HasStampsPlugin(_colorTheme, out var fieldsIcon, out var signaturesIcon))
            {
                ribRowPanel.Items.Add(
                    RibbonHelpers.AddSmallButton(
                        "mpStampFields",
                        Language.GetItem(LangItem, "h43"),
                        fieldsIcon,
                        Language.GetItem(LangItem, "h44"),
                        Language.GetItem(LangItem, "h45"),
                        string.Empty,
                        "autocadplugins/mpstamps"));
                
                ribRowPanel.Items.Add(
                    RibbonHelpers.AddSmallButton(
                        "mpSignatures",
                        Language.GetItem(LangItem, "h60"),
                        signaturesIcon,
                        Language.GetItem(LangItem, "h61"),
                        Language.GetItem(LangItem, "h62"),
                        string.Empty,
                        "autocadplugins/mpstamps"));
                
                ribRowPanel.Items.Add(new RibbonRowBreak());
            }

            ribRowPanel.Items.Add(
                RibbonHelpers.AddSmallButton(
                    "mpShowProductIcons",
                    Language.GetItem(LangItem, "h46"),
                    _colorTheme == 1
                        ? $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/mpShowProductIcons_16x16.png"
                        : $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/mpShowProductIcons_16x16_dark.png",
                    Language.GetItem(LangItem, "h37"),
                    Language.GetItem(LangItem, "h38"),
                    $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/mpShowProductIcon.png", "help/mpsettings"));
            
            ribRowPanel.Items.Add(
                RibbonHelpers.AddSmallButton(
                    "mpHideProductIcons",
                    Language.GetItem(LangItem, "h47"),
                    _colorTheme == 1
                        ? $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/mpHideProductIcons_16x16.png"
                        : $"pack://application:,,,/Modplus_{VersionData.CurrentCadVersion};component/Resources/mpHideProductIcons_16x16_dark.png",
                    Language.GetItem(LangItem, "h39"),
                    string.Empty,
                    string.Empty, 
                    "help/mpsettings"));
            
            ribSourcePanel.Items.Add(ribRowPanel);
        }
    }
}
