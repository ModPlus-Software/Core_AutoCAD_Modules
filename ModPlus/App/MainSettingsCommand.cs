namespace ModPlus.App
{
    using Autodesk.AutoCAD.Runtime;
    using ModPlusAPI.Windows;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    /// <summary>
    /// Команда запуска окна настроек
    /// </summary>
    public class MainSettingsCommand
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
    }
}