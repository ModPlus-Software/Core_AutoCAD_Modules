namespace ModPlus.App
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using MiniPlugins;
    using ModPlusAPI;
    using ModPlusAPI.Enums;
    using ModPlusAPI.LicenseServer;
    using ModPlusAPI.Mvvm;
    using ModPlusAPI.Windows;
    using ModPlusStyle;
    using ModPlusStyle.Controls.Dialogs;
    using Windows;
    using ModPlusAPI.UserInfo;

    /// <summary>
    /// Модель представления окна настроек
    /// </summary>
    public class SettingsViewModel : VmBase
    {
        private const string LangApi = "ModPlusAPI";
        private readonly SettingsWindow _parentWindow;
        private Language.LangItem _selectedLanguage;
        private BitmapImage _languageImage;

        private string _languageOnWindowOpen;
        private string _themeOnWindowOpen = string.Empty;
        private bool _drawingsInFloatMenuOnWindowOpen;
        private bool _ribbonOnWindowOpen;
        private bool _functionsInPaletteOnWindowOpen;
        private bool _drawingsInPaletteOnWindowOpen;
        private int _floatMenuCollapseToOnWindowOpen;
        private int _drawingsCollapseToOnWindowOpen;
        private bool _restartClientOnClose = true;

        private bool _canStopLocalLicenseServerConnection;
        private bool _canStopWebLicenseServerNotification;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        /// <param name="parentWindow">Parent window</param>
        public SettingsViewModel(SettingsWindow parentWindow)
        {
            _parentWindow = parentWindow;
            MiniPluginsViewModel = new MiniPluginsSettingsViewModel();
            FillData();
        }

        /// <summary>
        /// Модель представления для окна настроек, отвечающая за мини-плагины
        /// </summary>
        public MiniPluginsSettingsViewModel MiniPluginsViewModel { get; }

        /// <summary>
        /// Список доступных языков
        /// </summary>
        public List<Language.LangItem> Languages { get; private set; }

        /// <summary>
        /// Выбранный язык
        /// </summary>
        public Language.LangItem SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage == value)
                    return;
                _selectedLanguage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MessageAboutLanguageVisibility));
                Language.SetCurrentLanguage(value.Name);
                _parentWindow.SetLanguageProviderForModPlusWindow();
                _parentWindow.SetLanguageProviderForModPlusWindow("LangApi");
                SetLanguageImage();
            }
        }

        /// <summary>
        /// Изображение для языка (флаг)
        /// </summary>
        public BitmapImage LanguageImage
        {
            get => _languageImage;
            set
            {
                if (_languageImage == value)
                    return;
                _languageImage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Видимость сообщения о невозможности изменить какие-то настройки при переключении языка без перезагрузки
        /// </summary>
        public Visibility MessageAboutLanguageVisibility =>
            SelectedLanguage.Name != _languageOnWindowOpen ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Доступные темы
        /// </summary>
        public List<Theme> Themes { get; private set; }

        /// <summary>
        /// Выбранная тема оформления
        /// </summary>
        public Theme SelectedTheme
        {
            get => ThemeManager.GetTheme(Variables.PluginStyle);
            set
            {
                Variables.PluginStyle = value.Name;
                OnPropertyChanged();
                ThemeManager.ChangeTheme(_parentWindow, value);
            }
        }

        /// <summary>
        /// Разделители для чисел
        /// </summary>
        public string[] Separators => new[] { ".", "," };

        /// <summary>
        /// Выбранный разделитель целой и дробной части чисел
        /// </summary>
        public string SelectedSeparator
        {
            get => Variables.Separator;
            set => Variables.Separator = value;
        }

        /// <summary>
        /// Тихая загрузка
        /// </summary>
        public bool QuietLoading
        {
            get => Variables.QuietLoading;
            set => Variables.QuietLoading = value;
        }
        
        /// <summary>
        /// Запускает окно настроек уведомлений
        /// </summary>
        public ICommand NotificationSettingsCommand => new RelayCommandWithoutParameter(() =>
        {
            try
            {
                UserInfoService.ShowUserNotificationSettings();
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        });

        #region Adaptation

        /// <summary>
        /// Лента
        /// </summary>
        public bool Ribbon
        {
            get => Variables.Ribbon;
            set => Variables.Ribbon = value;
        }

        /// <summary>
        /// Палитра
        /// </summary>
        public bool Palette
        {
            get => Variables.Palette;
            set
            {
                Variables.Palette = value;
                OnPropertyChanged(nameof(PaletteDependsVisibility));
            }
        }

        /// <summary>
        /// Видимость опций, зависимых от опции <see cref="Palette"/>
        /// </summary>
        public Visibility PaletteDependsVisibility => Palette ? Visibility.Visible : Visibility.Hidden;

        /// <summary>
        /// Вкладка "Плагины" в палитре
        /// </summary>
        public bool FunctionsInPalette
        {
            get => Variables.FunctionsInPalette;
            set => Variables.FunctionsInPalette = value;
        }

        /// <summary>
        /// Вкладка "Чертежи" в палитре
        /// </summary>
        public bool DrawingsInPalette
        {
            get => Variables.DrawingsInPalette;
            set => Variables.DrawingsInPalette = value;
        }

        /// <summary>
        /// Плавающее меню
        /// </summary>
        public bool FloatMenu
        {
            get => Variables.FloatMenu;
            set
            {
                Variables.FloatMenu = value;
                OnPropertyChanged(nameof(FloatMenuDependsVisibility));
            }
        }

        /// <summary>
        /// Видимость опций, зависимых от опции <see cref="FloatMenu"/>
        /// </summary>
        public Visibility FloatMenuDependsVisibility => FloatMenu ? Visibility.Visible : Visibility.Hidden;

        /// <summary>
        /// В какое состояние сворачивать плавающее меню
        /// </summary>
        public int FloatMenuCollapseTo
        {
            get => Variables.FloatMenuCollapseTo;
            set => Variables.FloatMenuCollapseTo = value;
        }

        /// <summary>
        /// Вкладка "Чертежи" в плавающем меню
        /// </summary>
        public bool DrawingsInFloatMenu
        {
            get => Variables.DrawingsInFloatMenu;
            set => Variables.DrawingsInFloatMenu = value;
        }

        /// <summary>
        /// Плавающее меню чертежи
        /// </summary>
        public bool DrawingsFloatMenu
        {
            get => Variables.DrawingsFloatMenu;
            set
            {
                Variables.DrawingsFloatMenu = value;
                OnPropertyChanged(nameof(DrawingsFloatMenuDependsVisibility));
            }
        }

        /// <summary>
        /// Видимость опций, зависимых от опции <see cref="DrawingsFloatMenu"/>
        /// </summary>
        public Visibility DrawingsFloatMenuDependsVisibility =>
            DrawingsFloatMenu ? Visibility.Visible : Visibility.Hidden;

        /// <summary>
        /// В какое состояние сворачивать плавающее меню "Чертежи"
        /// </summary>
        public int DrawingsFloatMenuCollapseTo
        {
            get => Variables.DrawingsFloatMenuCollapseTo;
            set => Variables.DrawingsFloatMenuCollapseTo = value;
        }

        #endregion

        #region LAN License Server

        /// <summary>
        /// Включена работа с ЛВС Сервером Лицензий
        /// </summary>
        public bool IsLocalLicenseServerEnable
        {
            get => Variables.IsLocalLicenseServerEnable;
            set
            {
                Variables.IsLocalLicenseServerEnable = value;
                OnPropertyChanged();
                if (value)
                    IsWebLicenseServerEnable = false;
            }
        }

        /// <summary>
        /// Ip адрес локального сервера лицензий
        /// </summary>
        public string LocalLicenseServerIpAddress
        {
            get => Variables.LocalLicenseServerIpAddress;
            set => Variables.LocalLicenseServerIpAddress = value;
        }

        /// <summary>
        /// Порт локального сервера лицензий
        /// </summary>
        public int? LocalLicenseServerPort
        {
            get => Variables.LocalLicenseServerPort;
            set => Variables.LocalLicenseServerPort = value;
        }

        /// <summary>
        /// Можно ли выполнить остановку локального сервера лицензий. True - значит локальный сервер лицензий работает
        /// на момент открытия окна
        /// </summary>
        public bool CanStopLocalLicenseServerConnection
        {
            get => _canStopLocalLicenseServerConnection;
            set
            {
                if (_canStopLocalLicenseServerConnection == value)
                    return;
                _canStopLocalLicenseServerConnection = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Проверка соединения с локальным сервером лицензий
        /// </summary>
        public ICommand CheckLocalLicenseServerConnectionCommand => new RelayCommandWithoutParameter(async () =>
            {
                try
                {
                    await _parentWindow.ShowMessageAsync(
                        ClientStarter.IsLicenseServerAvailable()
                            ? Language.GetItem(LangApi, "h21")
                            : Language.GetItem(LangApi, "h20"),
                        $"{Language.GetItem(LangApi, "h22")} {LocalLicenseServerIpAddress}:{LocalLicenseServerPort}");
                }
                catch (Exception exception)
                {
                    ExceptionBox.Show(exception);
                }
            });

        /// <summary>
        /// Прервать работу локального сервера лицензий
        /// </summary>
        public ICommand StopLocalLicenseServerCommand => new RelayCommandWithoutParameter(
            () =>
        {
            try
            {
                ClientStarter.StopConnection();
                CanStopLocalLicenseServerConnection = false;
                _restartClientOnClose = false;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        },
            _ => CanStopLocalLicenseServerConnection);

        /// <summary>
        /// Восстановить работу локального сервера лицензий
        /// </summary>
        public ICommand RestoreLocalLicenseServerCommand => new RelayCommandWithoutParameter(
            () =>
        {
            try
            {
                ClientStarter.StartConnection(SupportedProduct.AutoCAD);
                CanStopLocalLicenseServerConnection = true;
                _restartClientOnClose = true;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        },
            _ => !CanStopLocalLicenseServerConnection);

        #endregion

        #region Web License Server

        /// <summary>
        /// Включена работа с веб сервером лицензий
        /// </summary>
        public bool IsWebLicenseServerEnable
        {
            get => Variables.IsWebLicenseServerEnable;
            set
            {
                Variables.IsWebLicenseServerEnable = value;
                OnPropertyChanged();
                if (value)
                    IsLocalLicenseServerEnable = false;
            }
        }

        /// <summary>
        /// Уникальный идентификатор сервера лицензий
        /// </summary>
        public string WebLicenseServerGuid
        {
            get => Variables.WebLicenseServerGuid.ToString();
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Variables.WebLicenseServerGuid = Guid.Empty;
                    WebLicenseServerClient.Instance.ReloadLicenseServerData();
                }
                else if (Guid.TryParse(value, out var guid))
                {
                    Variables.WebLicenseServerGuid = guid;
                    WebLicenseServerClient.Instance.ReloadLicenseServerData();
                }
                
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Адрес электронной почты пользователя, используемый для его идентификации в веб сервере лицензий
        /// </summary>
        public string WebLicenseServerUserEmail
        {
            get => Variables.WebLicenseServerUserEmail;
            set
            {
                Variables.WebLicenseServerUserEmail = value;
                WebLicenseServerClient.Instance.ReloadLicenseServerData();
            }
        }

        /// <summary>
        /// Можно ли остановить отправку уведомлений веб серверу лицензий
        /// </summary>
        public bool CanStopWebLicenseServerNotification
        {
            get => _canStopWebLicenseServerNotification;
            set
            {
                if (_canStopWebLicenseServerNotification == value)
                    return;
                _canStopWebLicenseServerNotification = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Проверка доступности веб сервера лицензий
        /// </summary>
        public ICommand CheckWebLicenseServerConnectionCommand => new RelayCommandWithoutParameter(async () =>
        {
            var result = false;
            await _parentWindow.InvokeWithIndeterminateProgress(Task.Run(async () =>
            {
                result = await WebLicenseServerClient.Instance.CheckConnection();
            }));
            if (result)
            {
                await _parentWindow.ShowMessageAsync(Language.GetItem(LangApi, "h21"), string.Empty);
            }
            else
            {
                ModPlusAPI.Windows.MessageBox.Show(
                    string.Format(Language.GetItem(LangApi, "h39"), WebLicenseServerGuid),
                    Language.GetItem(LangApi, "h20"),
                    MessageBoxIcon.Close);
            }
        });

        /// <summary>
        /// Проверка текущего пользователя на доступность работы с сервером лицензий
        /// </summary>
        public ICommand CheckIsUserAllowForWebLicenseServerCommand => new RelayCommandWithoutParameter(async () =>
        {
            var result = false;
            await _parentWindow.InvokeWithIndeterminateProgress(Task.Run(async () =>
            {
                result = await WebLicenseServerClient.Instance.CheckIsValidForUser();
            }));
            if (result)
            {
                // Для пользователя "{0}" имеется доступ к серверу лицензий "{1}"
                ModPlusAPI.Windows.MessageBox.Show(
                    string.Format(Language.GetItem(LangApi, "h41"), WebLicenseServerUserEmail, WebLicenseServerGuid),
                    MessageBoxIcon.Message);
            }
            else
            {
                // Для пользователя "{0}" отсутствует доступ к серверу лицензий "{1}"
                ModPlusAPI.Windows.MessageBox.Show(
                    string.Format(Language.GetItem(LangApi, "h42"), WebLicenseServerUserEmail, WebLicenseServerGuid) + 
                    Environment.NewLine + Language.GetItem(LangApi, "h43"),
                    MessageBoxIcon.Close);
            }
        });

        /// <summary>
        /// Остановить отправку уведомлений веб серверу лицензий
        /// </summary>
        public ICommand StopWebLicenseServerNotificationsCommand => new RelayCommandWithoutParameter(
            () =>
        {
            WebLicenseServerClient.Instance.Stop();
            CanStopWebLicenseServerNotification = false;
        },
            _ => CanStopWebLicenseServerNotification);

        /// <summary>
        /// Восстановить отправку уведомлений веб серверу лицензий
        /// </summary>
        public ICommand RestoreWebLicenseServerNotificationsCommand => new RelayCommandWithoutParameter(
            () =>
        {
            WebLicenseServerClient.Instance.Start(SupportedProduct.AutoCAD);
            CanStopWebLicenseServerNotification = true;
        },
            _ => !CanStopWebLicenseServerNotification);

        #endregion

        /// <summary>
        /// Отключить работу сервера лицензий в AutoCAD
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public bool DisableConnectionWithLicenseServerInAutoCAD
        {
            get => Variables.DisableConnectionWithLicenseServerInAutoCAD;
            set => Variables.DisableConnectionWithLicenseServerInAutoCAD = value;
        }

        /// <summary>
        /// Заполнение данных
        /// </summary>
        public void FillData()
        {
            try
            {
                FillAndSetLanguages();
                FillThemesAndColors();

                _ribbonOnWindowOpen = Ribbon;
                _functionsInPaletteOnWindowOpen = FunctionsInPalette;
                _drawingsInPaletteOnWindowOpen = DrawingsInPalette;
                _floatMenuCollapseToOnWindowOpen = FloatMenuCollapseTo;
                _drawingsInFloatMenuOnWindowOpen = DrawingsInFloatMenu;
                _drawingsCollapseToOnWindowOpen = DrawingsFloatMenuCollapseTo;

                CanStopLocalLicenseServerConnection = ClientStarter.IsClientWorking();
                CanStopWebLicenseServerNotification =
                    Variables.IsWebLicenseServerEnable && 
                    !Variables.DisableConnectionWithLicenseServerInAutoCAD &&
                    WebLicenseServerClient.Instance.IsWorked;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        /// <summary>
        /// Применить настройки
        /// </summary>
        public void ApplySettings()
        {
            try
            {
                var isDifferentLanguage = SelectedLanguage.Name != _languageOnWindowOpen;

                // Если отключили плавающее меню
                if (!FloatMenu)
                {
                    // Закрываем плавающее меню
                    if (FloatMenuCommand.MainMenuWin != null)
                        FloatMenuCommand.MainMenuWin.Close();
                }
                else //// Если включили плавающее меню
                {
                    // Если плавающее меню было включено
                    if (FloatMenuCommand.MainMenuWin != null)
                    {
                        if (SelectedTheme.Name != _themeOnWindowOpen ||
                            FloatMenuCollapseTo != _floatMenuCollapseToOnWindowOpen ||
                            DrawingsInFloatMenu != _drawingsInFloatMenuOnWindowOpen ||
                            isDifferentLanguage)
                        {
                            FloatMenuCommand.MainMenuWin.Close();
                            FloatMenuCommand.LoadMainMenu();
                        }
                    }
                    else
                    {
                        FloatMenuCommand.LoadMainMenu();
                    }
                }

                // если отключили палитру
                if (!Palette)
                {
                    if (MpPalette.MpPaletteSet != null)
                        MpPalette.MpPaletteSet.Visible = false;
                }
                else if (FunctionsInPalette != _functionsInPaletteOnWindowOpen ||
                         DrawingsInPalette != _drawingsInPaletteOnWindowOpen)
                {
                    MpPalette.CreatePalette(true);
                }
                else
                {
                    MpPalette.CreatePalette(false);
                }

                // Если отключили плавающее меню Чертежи
                if (!DrawingsFloatMenu)
                {
                    if (MpDrawingsFunction.MpDrawingsWin != null)
                        MpDrawingsFunction.MpDrawingsWin.Close();
                }
                else
                {
                    if (MpDrawingsFunction.MpDrawingsWin != null)
                    {
                        if (SelectedTheme.Name != _themeOnWindowOpen ||
                            DrawingsFloatMenuCollapseTo != _drawingsCollapseToOnWindowOpen ||
                            DrawingsInFloatMenu != _drawingsInFloatMenuOnWindowOpen ||
                            isDifferentLanguage)
                        {
                            MpDrawingsFunction.MpDrawingsWin.Close();
                            MpDrawingsFunction.LoadMainMenu();
                        }
                    }
                    else
                    {
                        MpDrawingsFunction.LoadMainMenu();
                    }
                }

                // Ribbon
                // Если включили и была выключена
                if (Ribbon && !_ribbonOnWindowOpen)
                    RibbonBuilder.BuildRibbon();

                // Если включили и была включена, но сменился язык
                if (Ribbon && _ribbonOnWindowOpen && isDifferentLanguage)
                {
                    RibbonBuilder.RemoveRibbon();
                    RibbonBuilder.BuildRibbon(true);
                }

                // Если выключили и была включена
                if (!Ribbon && _ribbonOnWindowOpen)
                    RibbonBuilder.RemoveRibbon();

                // context menu
                // если сменился язык, то все выгружаю
                if (isDifferentLanguage)
                    MiniFunctions.UnloadAll();

                MiniFunctions.LoadUnloadContextMenu();

                if (_restartClientOnClose)
                {
                    // reload server
                    ClientStarter.StopConnection();
                    ClientStarter.StartConnection(SupportedProduct.AutoCAD);
                }

                if (!IsWebLicenseServerEnable && WebLicenseServerClient.Instance.IsWorked)
                    WebLicenseServerClient.Instance.Stop();
                if (IsWebLicenseServerEnable && !WebLicenseServerClient.Instance.IsWorked)
                    WebLicenseServerClient.Instance.Start(SupportedProduct.AutoCAD);

                // перевод фокуса на AutoCAD
                Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
            }
            catch (Exception ex)
            {
                ExceptionBox.Show(ex);
            }
        }

        private void FillAndSetLanguages()
        {
            Languages = Language.GetLanguagesByFiles();
            OnPropertyChanged(nameof(Languages));
            var selectedLanguage = Languages.First(li => li.Name == Language.CurrentLanguageName);
            _languageOnWindowOpen = selectedLanguage.Name;
            SelectedLanguage = selectedLanguage;
        }

        private void FillThemesAndColors()
        {
            Themes = new List<Theme>(ThemeManager.Themes);
            OnPropertyChanged(nameof(Themes));
            var pluginStyle = ThemeManager.Themes.First();
            var savedPluginStyleName = Variables.PluginStyle;
            if (!string.IsNullOrEmpty(savedPluginStyleName))
            {
                var theme = ThemeManager.Themes.Single(t => t.Name == savedPluginStyleName);
                if (theme != null)
                    pluginStyle = theme;
            }

            _themeOnWindowOpen = pluginStyle.Name;
            SelectedTheme = pluginStyle;
        }

        private void SetLanguageImage()
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(
                    $"pack://application:,,,/ModPlus_{VersionData.CurrentCadVersion};component/Resources/Flags/{SelectedLanguage.Name}.png");
                bi.EndInit();
                LanguageImage = bi;
            }
            catch
            {
                LanguageImage = null;
            }
        }
    }
}
