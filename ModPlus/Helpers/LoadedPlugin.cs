namespace ModPlus.Helpers
{
    using System;
    using System.Collections.Generic;
    using ModPlusAPI.Abstractions;
    using ModPlusAPI.Enums;

    /// <summary>
    /// Загруженный плагин
    /// </summary>
    internal class LoadedPlugin : IModPlusPlugin
    {
        /// <inheritdoc/>
        public SupportedProduct SupportedProduct => SupportedProduct.AutoCAD;

        /// <inheritdoc/>
        public string Name { get; set; }

        /// <inheritdoc/>
        public string LName { get; set; }

        /// <inheritdoc/>
        public string AvailProductExternalVersion { get; set; }

        /// <inheritdoc/>
        public string FullClassName { get; set; }

        /// <inheritdoc/>
        public string AppFullClassName { get; set; }

        /// <inheritdoc/>
        public Guid AddInId { get; set; }

        /// <summary>
        /// Маленькая иконка
        /// </summary>
        public string SmallIconUrl { get; set; }

        /// <summary>
        /// Маленькая иконка для темной темы
        /// </summary>
        public string SmallDarkIconUrl { get; set; }

        /// <summary>
        /// Большая иконка
        /// </summary>
        public string BigIconUrl { get; set; }

        /// <summary>
        /// Большая иконка для темной темы
        /// </summary>
        public string BigDarkIconUrl { get; set; }

        /// <inheritdoc/>
        public string Description { get; set; }

        /// <inheritdoc/>
        public string Author { get; set; }

        /// <inheritdoc/>
        public string Price { get; set; }

        /// <inheritdoc/>
        public bool CanAddToRibbon { get; set; }

        /// <inheritdoc/>
        public string FullDescription { get; set; }

        /// <inheritdoc/>
        public string ToolTipHelpImage { get; set; }

        /// <inheritdoc/>
        public List<string> SubPluginsNames { get; set; }

        /// <inheritdoc/>
        public List<string> SubPluginsLNames { get; set; }

        /// <inheritdoc/>
        public List<string> SubDescriptions { get; set; }

        /// <inheritdoc/>
        public List<string> SubFullDescriptions { get; set; }

        /// <inheritdoc/>
        public List<string> SubHelpImages { get; set; }

        /// <inheritdoc/>
        public List<string> SubClassNames { get; set; }

        /// <summary>
        /// Маленькие иконки под-команд
        /// </summary>
        public List<string> SubSmallIconsUrl { get; set; }

        /// <summary>
        /// Маленькие иконки под-команд для темной темы
        /// </summary>
        public List<string> SubSmallDarkIconsUrl { get; set; }

        /// <summary>
        /// Большие иконки под-команд
        /// </summary>
        public List<string> SubBigIconsUrl { get; set; }

        /// <summary>
        /// Большие иконки под-команд для темной темы
        /// </summary>
        public List<string> SubBigDarkIconsUrl { get; set; }
    }
}