﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using mpMsg;
using mpSettings;
using MahApps.Metro;
using ModPlus.MinFuncWins;
using Exception = System.Exception;

namespace ModPlus.App
{
    public partial class MpMainSettings
    {
        private string _curUserEmail = string.Empty;
        private string _curTheme = string.Empty;
        private string _curColor = string.Empty;
        private bool _curFloatMenu = false;
        private bool _curPalette = false;
        private bool _curPaletteFunctions = false;
        private bool _curPaletteDrawings = false;
        private bool _curDrwsOnMnu = false;
        private bool _curRibbon = false;
        private bool _curDrawingsAlone = false;
        private int _curFloatMenuCollapseTo = 0;
        private int _curDrawingsCollapseTo = 1;
        private string _curBordersType = string.Empty;
        public List<AccentColorMenuData> AccentColors { get; set; }
        public List<AppThemeMenuData> AppThemes { get; set; }
        public MpMainSettings()
        {
            InitializeComponent();

            FillThemesAndColors();
            ChangeWindowTheme();
            SetAppRegistryKeyForCurrentUser();
            GetDataFromConfigFile();
            GetDataByVars();
            Closing += MpMainSettings_Closing;
            Closed += MpMainSettings_OnClosed;
        }
        
        private void FillThemesAndColors()
        {
            ThemeManager.AddAppTheme("DarkBlue", new Uri("pack://application:,,,/mpCustomThemes;component/DarkBlue.xaml"));
            // create accent color menu items for the demo
            AccentColors = ThemeManager.Accents
                                            .Select(a => new AccentColorMenuData() { Name = a.Name, ColorBrush = a.Resources["AccentColorBrush"] as Brush })
                                            .ToList();

            // create metro theme color menu items for the demo
            AppThemes = ThemeManager.AppThemes
                                           .Select(a => new AppThemeMenuData() { Name = a.Name, BorderColorBrush = a.Resources["BlackColorBrush"] as Brush, ColorBrush = a.Resources["WhiteColorBrush"] as Brush })
                                           .ToList();

            MiColor.ItemsSource = AccentColors;
            MiTheme.ItemsSource = AppThemes;

            // Устанавливаем текущие. На всякий случай "без ошибок"
            try
            {
                _curTheme = MpSettings.GetValue("Settings", "MainSet", "Theme");
                foreach (var item in MiTheme.Items.Cast<AppThemeMenuData>().Where(item => item.Name.Equals(_curTheme)))
                {
                    MiTheme.SelectedIndex = MiTheme.Items.IndexOf(item);
                }

                _curColor = MpSettings.GetValue("Settings", "MainSet", "AccentColor");
                foreach (
                    var item in MiColor.Items.Cast<AccentColorMenuData>().Where(item => item.Name.Equals(_curColor)))
                {
                    MiColor.SelectedIndex = MiColor.Items.IndexOf(item);
                }
            }
            catch
            {
                //ignored
            }
        }
        // Заполнение поля Ключ продукта
        private void SetAppRegistryKeyForCurrentUser()
        {
            // Ключ берем из глобальных настроек
            var key = MpVars.RegistryKey;
            if (string.IsNullOrEmpty(key))
            {
                TbAboutRegKey.Visibility = Visibility.Collapsed;
                TbRegistryKey.Text = string.Empty;
            }
            else
            {
                TbRegistryKey.Text = key;
                var regVariant = MpSettings.GetValue("User", "RegestryVariant");
                if (!string.IsNullOrEmpty(regVariant))
                {
                    TbAboutRegKey.Visibility = Visibility.Visible;
                    if (regVariant.Equals("0"))
                        TbAboutRegKey.Text = "Ключ привязан к физическому диску " + MpSettings.GetValue("User", "HDmodel");
                    else if (regVariant.Equals("1"))
                        TbAboutRegKey.Text = "Ключ привязан к аккаунту Google: " + MpSettings.GetValue("User", "gName");
                }
            }
        }

        private void ChangeWindowTheme()
        {
            //Theme
            try
            {
                ThemeManager.ChangeAppStyle(this,
                    ThemeManager.Accents.First(
                        x => x.Name.Equals(MpSettings.GetValue("Settings", "MainSet", "AccentColor"))
                        ),
                    ThemeManager.AppThemes.First(
                        x => x.Name.Equals(MpSettings.GetValue("Settings", "MainSet", "Theme")))
                    );

            }
            catch
            {
                //ignored
            }
        }
        // Загрузка данных из файла конфигурации
        // которые требуется отобразить в окне
        private void GetDataFromConfigFile()
        {
            // Separator
            var separator = MpSettings.GetValue("Settings", "MainSet", "Separator");
            CbSeparatorSettings.SelectedIndex = string.IsNullOrEmpty(separator) ? 0 : int.Parse(separator);
            // Check updates and new
            bool b;
            ChkEntByBlock.IsChecked = !bool.TryParse(MpSettings.GetValue("Settings", "EntByBlockOCM"), out b) || b; //true
            ChkFastBlocks.IsChecked = !bool.TryParse(MpSettings.GetValue("Settings", "FastBlocksCM"), out b) || b; //true
            ChkVPtoMS.IsChecked = !bool.TryParse(MpSettings.GetValue("Settings", "VPtoMS"), out b) || b; //true
            
            // Виды границ окна
            var border = MpSettings.GetValue("Settings", "MainSet", "BordersType");
            foreach (ComboBoxItem item in CbWindowsBorders.Items)
            {
                if (item.Tag.Equals(border))
                {
                    CbWindowsBorders.SelectedItem = item; break;
                }
            }
            if (CbWindowsBorders.SelectedIndex == -1) CbWindowsBorders.SelectedIndex = 3;
            _curBordersType = ((ComboBoxItem)CbWindowsBorders.SelectedItem).Tag.ToString();
        }
        /// <summary>
        /// Получение значений из глобальных переменных плагина
        /// </summary>
        private void GetDataByVars()
        {
            try
            {
                // Адаптация
                ChkMpFloatMenu.IsChecked = _curFloatMenu = MpVars.MpFloatMenu;
                ChkMpPalette.IsChecked = _curPalette = MpVars.MpPalette;
                ChkMpPaletteFunctions.IsChecked = _curPaletteFunctions = MpVars.MpPaletteFunctions;
                ChkMpPaletteDrawings.IsChecked = _curPaletteDrawings = MpVars.MpPaletteDrawings;
                ChkMpRibbon.IsChecked = _curRibbon = MpVars.MpRibbon;
                ChkMpChkDrwsOnMnu.IsChecked = _curDrwsOnMnu = MpVars.MpChkDrwsOnMnu;
                ChkMpDrawingsAlone.IsChecked = _curDrawingsAlone = MpVars.DrawingsAlone;
                // Выбор в выпадающих списках (сворачивать в)
                CbFloatMenuCollapseTo.SelectedIndex = _curFloatMenuCollapseTo = MpVars.FloatMenuCollapseTo;
                CbDrawingsCollapseTo.SelectedIndex = _curDrawingsCollapseTo = MpVars.DrawingsCollapseTo;
                // Видимость в зависимости от галочек
                ChkMpChkDrwsOnMnu.Visibility =
                    CbFloatMenuCollapseTo.Visibility =
                        TbFloatMenuCollapseTo.Visibility = _curFloatMenu ? Visibility.Visible : Visibility.Collapsed;
                CbDrawingsCollapseTo.Visibility =
                    TbDrawingsCollapseTo.Visibility = _curDrawingsAlone ? Visibility.Visible : Visibility.Collapsed;
                ChkMpPaletteDrawings.Visibility =
                    ChkMpPaletteFunctions.Visibility = _curPalette ? Visibility.Visible : Visibility.Collapsed;
                // Тихая загрузка
                ChkQuietLoading.IsChecked = MpVars.QuietLoading;
                // email
                TbEmailAdress.Text = _curUserEmail = MpVars.UserEmail;
            }
            catch (Exception exception)
            {
                MpExWin.Show(exception);
            }
        }
        // Выбор разделителя целой и дробной части для чисел
        private void CbSeparatorSettings_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MpSettings.SetValue("Settings", "MainSet", "Separator",
                ((ComboBox)sender).SelectedIndex.ToString(CultureInfo.InvariantCulture), true);
        }
        // Выбор темы
        private void MiTheme_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MpSettings.SetValue("Settings", "MainSet", "Theme", ((AppThemeMenuData)e.AddedItems[0]).Name, true);
            ChangeWindowTheme();
        }
        // Выбор цвета
        private void MiColor_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MpSettings.SetValue("Settings", "MainSet", "AccentColor", ((AccentColorMenuData)e.AddedItems[0]).Name, true);
            ChangeWindowTheme();
        }
        // windows borders select
        private void CbWindowsBorders_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            var cbi = cb?.SelectedItem as ComboBoxItem;
            if (cbi == null) return;
            MpWindowHelpers.ChangeWindowBordes(cbi.Tag.ToString(), this);
            MpSettings.SetValue("Settings", "MainSet", "BordersType", cbi.Tag.ToString(), true);
        }


        private void MpMainSettings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!string.IsNullOrEmpty(TbEmailAdress.Text))
            {
                if (IsValidEmail(TbEmailAdress.Text))
                    TbEmailAdress.BorderBrush = FindResource("TextBoxBorderBrush") as Brush;
                else
                {
                    TbEmailAdress.BorderBrush = Brushes.Red;
                    MpMsgWin.Show("Указанный адрес почты не прошел проверку!" + Environment.NewLine +
                                  "Или вы ошиблись в указании адреса почты или у вас оооочень уникальный хостер почты =)");
                    TbEmailAdress.Focus();
                    e.Cancel = true;
                }
            }
        }
        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        private void MpMainSettings_OnClosed(object sender, EventArgs e)
        {
            try
            {
                // Так как эти значения хранятся в переменных, то их нужно перезаписать
                MpVars.MpChkDrwsOnMnu = ChkMpChkDrwsOnMnu.IsChecked.Value;
                MpVars.MpFloatMenu = ChkMpFloatMenu.IsChecked.Value;
                MpVars.MpPalette = ChkMpPalette.IsChecked.Value;
                MpVars.MpPaletteDrawings = ChkMpPaletteDrawings.IsChecked.Value;
                MpVars.MpPaletteFunctions = ChkMpPaletteFunctions.IsChecked.Value;
                MpVars.MpRibbon = ChkMpRibbon.IsChecked.Value;
                MpVars.MpSeparator = CbSeparatorSettings.SelectedIndex.ToString(CultureInfo.InvariantCulture);
                MpVars.QuietLoading = ChkQuietLoading.IsChecked.Value;
                MpVars.FloatMenuCollapseTo = CbFloatMenuCollapseTo.SelectedIndex;
                MpVars.DrawingsCollapseTo = CbDrawingsCollapseTo.SelectedIndex;
                MpVars.DrawingsAlone = ChkMpDrawingsAlone.IsChecked.Value;
                // Сохраняем в реестр почту, если изменилась
                MpVars.UserEmail = TbEmailAdress.Text;
                if (!TbEmailAdress.Text.Equals(_curUserEmail))
                {
                    var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("ModPlus");
                    using (key)
                        key?.SetValue("email", TbEmailAdress.Text);
                }
                // Если отключили плавающее меню
                if (!ChkMpFloatMenu.IsChecked.Value)
                {
                    // Закрываем плавающее меню
                    if (MpMenuFunction.MpMainMenuWin != null)
                        MpMenuFunction.MpMainMenuWin.Close();
                }
                else // Если включили плавающее меню
                {
                    // Если плавающее меню было включено
                    if (MpMenuFunction.MpMainMenuWin != null)
                    {
                        // Перегружаем плавающее меню, если изменилась тема, вкл/выкл открытые чертежи, границы, сворачивать в
                        if (!string.IsNullOrEmpty(_curColor) &
                            !string.IsNullOrEmpty(_curTheme) &
                            !string.IsNullOrEmpty(_curBordersType) &
                            !string.IsNullOrEmpty(_curFloatMenuCollapseTo.ToString()))
                        {
                            if (!MpSettings.GetValue("Settings", "MainSet", "Theme").Equals(_curTheme) |
                                !MpSettings.GetValue("Settings", "MainSet", "AccentColor").Equals(_curColor) |
                                !MpSettings.GetValue("Settings", "MainSet", "BordersType").Equals(_curBordersType) |
                                !MpSettings.GetValue("Settings", "MainSet", "FloatMenuCollapseTo").Equals(_curFloatMenuCollapseTo.ToString()) |
                                !ChkMpChkDrwsOnMnu.IsChecked.Value.Equals(_curDrwsOnMnu))
                            {
                                MpMenuFunction.MpMainMenuWin.Close();
                                MpMenuFunction.LoadMainMenu();
                            }
                        }
                    }
                    else MpMenuFunction.LoadMainMenu();
                }
                // если отключили палитру
                if (!ChkMpPalette.IsChecked.Value)
                {
                    if (MpPalette.MpPaletteSet != null)
                        MpPalette.MpPaletteSet.Visible = false;
                }
                else // если включили палитру
                {
                    MpPalette.CreatePalette();
                }
                // Если отключили плавающее меню Чертежи
                if (!ChkMpDrawingsAlone.IsChecked.Value)
                {
                    if (MpDrawingsFunction.MpDrawingsWin != null)
                        MpDrawingsFunction.MpDrawingsWin.Close();
                }
                else
                {
                    if (MpDrawingsFunction.MpDrawingsWin != null)
                    {
                        // Перегружаем плавающее меню, если изменилась тема, вкл/выкл открытые чертежи, границы, сворачивать в
                        if (!string.IsNullOrEmpty(_curColor) &
                            !string.IsNullOrEmpty(_curTheme) &
                            !string.IsNullOrEmpty(_curBordersType) &
                            !string.IsNullOrEmpty(_curDrawingsCollapseTo.ToString()))
                        {
                            if (!MpSettings.GetValue("Settings", "MainSet", "Theme").Equals(_curTheme) |
                                !MpSettings.GetValue("Settings", "MainSet", "AccentColor").Equals(_curColor) |
                                !MpSettings.GetValue("Settings", "MainSet", "BordersType").Equals(_curBordersType) |
                                !MpSettings.GetValue("Settings", "MainSet", "DrawingsCollapseTo").Equals(_curDrawingsCollapseTo.ToString()) |
                                !ChkMpDrawingsAlone.IsChecked.Value.Equals(_curDrawingsAlone))
                            {
                                MpDrawingsFunction.MpDrawingsWin.Close();
                                MpDrawingsFunction.LoadMainMenu();
                            }
                        }
                    }
                    else MpDrawingsFunction.LoadMainMenu();
                }
                // Если выключили/включили ленту
                if (!ChkMpRibbon.IsChecked.Value.Equals(_curRibbon))
                {
                    if (ChkMpRibbon.IsChecked.Value) RibbonBuilder.BuildRibbon();
                    else RibbonBuilder.RemoveRibbon();
                }
                // context menues
                MiniFunctions.LoadUnloadContextMenues();
                // перевод фокуса на автокад
                Utils.SetFocusToDwgView();
            }
            catch (Exception ex)
            {
                MpExWin.Show(ex);
            }

        }
        // Сохранение в файл конфигурации значений вкл/выкл для меню
        // Имена должны начинаться с ChkMp!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        private void Menues_OnChecked_Unchecked(object sender, RoutedEventArgs e)
        {
            var chkBox = sender as CheckBox;
            if (chkBox == null) return;
            var name = chkBox.Name;
            MpSettings.SetValue("Settings", "MainSet",
                name.Substring(5),
                chkBox.IsChecked?.ToString(),
                true
                );
            if (name.Equals("ChkMpFloatMenu"))
            {
                ChkMpChkDrwsOnMnu.Visibility = TbFloatMenuCollapseTo.Visibility = CbFloatMenuCollapseTo.Visibility =
                    chkBox.IsChecked != null && chkBox.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
            }
            if (name.Equals("ChkMpDrawingsAlone"))
            {
                TbDrawingsCollapseTo.Visibility = CbDrawingsCollapseTo.Visibility =
                    chkBox.IsChecked != null && chkBox.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
            }
            if (name.Equals("ChkMpPalette"))
            {
                ChkMpPaletteDrawings.Visibility = ChkMpPaletteFunctions.Visibility =
                    chkBox.IsChecked != null && chkBox.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        // Тихая загрузка
        private void ChkQuietLoading_OnChecked_OnUnchecked(object sender, RoutedEventArgs e)
        {
            MpSettings.SetValue("Settings", "MainSet", "ChkQuietLoading", (ChkQuietLoading.IsChecked != null && ChkQuietLoading.IsChecked.Value).ToString(), true);
            MpVars.QuietLoading = (ChkQuietLoading.IsChecked != null && ChkQuietLoading.IsChecked.Value);
        }
        // Сворачивать в - для плавающего меню
        private void CbFloatMenuCollapseTo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb != null)
            {
                MpSettings.SetValue("Settings", "MainSet", "FloatMenuCollapseTo",
                    cb.SelectedIndex.ToString(CultureInfo.InvariantCulture), true);
                MpVars.FloatMenuCollapseTo = cb.SelectedIndex;
            }
        }

        private void CbDrawingsCollapseTo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb != null)
            {
                MpSettings.SetValue("Settings", "MainSet", "DrawingsCollapseTo",
                    cb.SelectedIndex.ToString(CultureInfo.InvariantCulture), true);
                MpVars.DrawingsCollapseTo = cb.SelectedIndex;
            }
        }
        #region Контекстные меню
        // Задать вхождения ПоБлоку
        private void ChkEntByBlock_OnChecked(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk != null)
            {
                MpSettings.SetValue("Settings", "EntByBlockOCM", (chk.IsChecked != null && chk.IsChecked.Value).ToString(), true);
                if (chk.IsChecked != null && chk.IsChecked.Value)
                    MiniFunctions.ContextMenues.EntByBlockObjectContextMenu.Attach();
                else MiniFunctions.ContextMenues.EntByBlockObjectContextMenu.Detach();
            }
        }
        // Частоиспользуемые блоки
        private void ChkFastBlocks_OnChecked(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk != null)
            {
                MpSettings.SetValue("Settings", "FastBlocksCM", (chk.IsChecked != null && chk.IsChecked.Value).ToString(), true);
                if (chk.IsChecked != null && chk.IsChecked.Value)
                    MiniFunctions.ContextMenues.FastBlockContextMenu.Attach();
                else MiniFunctions.ContextMenues.FastBlockContextMenu.Detach();
            }
        }
        private void BtFastBlocksSettings_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new FastBlocksSettings();
            win.ShowDialog();
        }
        // Границы ВЭ в модель
        private void ChkVPtoMS_OnChecked(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk != null)
            {
                MpSettings.SetValue("Settings", "VPtoMS", (chk.IsChecked != null && chk.IsChecked.Value).ToString(), true);
                if (chk.IsChecked != null && chk.IsChecked.Value)
                    MiniFunctions.ContextMenues.VPtoMSobjectContextMenu.Attach();
                else MiniFunctions.ContextMenues.VPtoMSobjectContextMenu.Detach();
            }
        }
        #endregion
        private void TbEmailAdress_OnLostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                if (IsValidEmail(tb.Text))
                    tb.BorderBrush = FindResource("TextBoxBorderBrush") as Brush;
                else tb.BorderBrush = Brushes.Red;
            }
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    public class AccentColorMenuData
    {
        public string Name { get; set; }
        public Brush BorderColorBrush { get; set; }
        public Brush ColorBrush { get; set; }

    }
    public class AppThemeMenuData : AccentColorMenuData
    {
    }

    public class MpMainSettingsFunction
    {
        [CommandMethod("ModPlus", "mpSettings", CommandFlags.Modal)]
        public void Main()
        {
            var win = new MpMainSettings();
            win.ShowDialog();
        }
    }
}
