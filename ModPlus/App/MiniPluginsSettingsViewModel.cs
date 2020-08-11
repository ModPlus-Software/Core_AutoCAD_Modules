namespace ModPlus.App
{
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Input;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;
    using Windows.MiniPlugins;
    using MiniPlugins;

    /// <summary>
    /// Модель представления для окна настроек, отвечающая за мини-плагины
    /// </summary>
    public class MiniPluginsSettingsViewModel : VmBase
    {
        /// <summary>
        /// Задать вхождения по блоку
        /// </summary>
        public bool EntByBlock
        {
            get => !bool.TryParse(UserConfigFile.GetValue("EntByBlockOCM"), out var b) || b; // true
            set
            {
                UserConfigFile.SetValue("EntByBlockOCM", value.ToString(), true);
                if (value)
                    MiniFunctionsContextMenuExtensions.EntByBlockObjectContextMenu.Attach();
                else
                    MiniFunctionsContextMenuExtensions.EntByBlockObjectContextMenu.Detach();
            }
        }

        /// <summary>
        /// Задать слой вхождений
        /// </summary>
        public bool NestedEntLayer
        {
            get => !bool.TryParse(UserConfigFile.GetValue("NestedEntLayerOCM"), out var b) || b; // true
            set
            {
                UserConfigFile.SetValue("NestedEntLayerOCM", value.ToString(), true);
                if (value)
                    MiniFunctionsContextMenuExtensions.NestedEntLayerObjectContextMenu.Attach();
                else
                    MiniFunctionsContextMenuExtensions.NestedEntLayerObjectContextMenu.Detach();
            }
        }

        /// <summary>
        /// Часто используемые блоки
        /// </summary>
        public bool FastBlocks
        {
            get => !bool.TryParse(UserConfigFile.GetValue("FastBlocksCM"), out var b) || b; // true
            set
            {
                UserConfigFile.SetValue("FastBlocksCM", value.ToString(), true);
                if (value)
                    MiniFunctionsContextMenuExtensions.FastBlockContextMenu.Attach();
                else
                    MiniFunctionsContextMenuExtensions.FastBlockContextMenu.Detach();
            }
        }

        /// <summary>
        /// Границы видового экрана в пространство модели
        /// </summary>
        //// ReSharper disable once InconsistentNaming
        public bool VPtoMS
        {
            get => !bool.TryParse(UserConfigFile.GetValue("VPtoMS"), out var b) || b; // true
            set
            {
                UserConfigFile.SetValue("VPtoMS", value.ToString(), true);
                if (value)
                    MiniFunctionsContextMenuExtensions.VPtoMSObjectContextMenu.Attach();
                else
                    MiniFunctionsContextMenuExtensions.VPtoMSObjectContextMenu.Detach();
            }
        }

        /// <summary>
        /// Редактировать вершины полилиний
        /// </summary>
        public bool WipeoutEdit
        {
            get => !bool.TryParse(UserConfigFile.GetValue("WipeoutEditOCM"), out var b) || b; // true
            set
            {
                UserConfigFile.SetValue("WipeoutEditOCM", value.ToString(), true);
                if (value)
                    MiniFunctionsContextMenuExtensions.WipeoutEditObjectContextMenu.Attach();
                else 
                    MiniFunctionsContextMenuExtensions.WipeoutEditObjectContextMenu.Detach();
            }
        }

        /// <summary>
        /// Команда запуска окна настроек мини-плагина "Часто используемые блоки"
        /// </summary>
        public ICommand FastBlocksSettingsCommand => 
            new RelayCommandWithoutParameter(() => new FastBlocksSettings().ShowDialog());

        /// <summary>
        /// Открыть справку
        /// </summary>
        public ICommand OpenHelpCommand => new RelayCommand<string>(alias =>
        {
            var s = Language.RusWebLanguages.Contains(Language.CurrentLanguageName) ? "ru" : "en";
            Process.Start($"https://modplus.org/{s}/help/mini-plugins/{alias}");
        });
    }
}
