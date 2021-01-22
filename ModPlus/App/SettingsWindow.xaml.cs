namespace ModPlus.App
{
    /// <summary>
    /// Окно настроек ModPlus
    /// </summary>
    public partial class SettingsWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        internal SettingsWindow()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem("AutocadDlls", "h1");
            ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources, "LangApi");
        }
    }
}
