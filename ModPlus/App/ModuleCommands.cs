namespace ModPlus.App
{
    using Autodesk.AutoCAD.Runtime;
    using ModPlusAPI.Windows;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
    using Exception = Autodesk.AutoCAD.Runtime.Exception;

    /// <summary>
    /// Команды модуля
    /// </summary>
    public class ModuleCommands
    {
        /// <summary>
        /// Запуск окна настроек
        /// </summary>
        [CommandMethod("ModPlus", "mpSettings", CommandFlags.Modal)]
        public static void OpenSettings()
        {
            try
            {
                var win = new SettingsWindow();
                var viewModel = new SettingsViewModel(win);
                win.DataContext = viewModel;
                win.Closed += (sender, args) => viewModel.ApplySettings();
                AcApp.ShowModalWindow(AcApp.MainWindow.Handle, win);
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        /// <summary>
        /// Запустить окно "Личный кабинет"
        /// </summary>
        [CommandMethod("ModPlus", "mpUserInfo", CommandFlags.Modal)]
        public void ShowUserInfo()
        {
            try
            {
                ModPlusAPI.UserInfo.UserInfoService.ShowUserInfo();
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        /// <summary>
        /// Запустить окно "Обратная связь"
        /// </summary>
        [CommandMethod("ModPlus", "mpFeedback", CommandFlags.Modal)]
        public void ShowFeedback()
        {
            try
            {
                ModPlusAPI.UserInfo.UserInfoService.ShowFeedback();
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
    }
}
