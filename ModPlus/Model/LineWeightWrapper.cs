namespace ModPlus.Model
{
    using Autodesk.AutoCAD.DatabaseServices;

    /// <summary>
    /// Обертка для <see cref="Autodesk.AutoCAD.DatabaseServices.LineWeight"/>
    /// </summary>
    public class LineWeightWrapper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LineWeightWrapper"/> class.
        /// </summary>
        /// <param name="displayName">Отображаемое имя</param>
        /// <param name="lineWeight">Соответствующий <see cref="Autodesk.AutoCAD.DatabaseServices.LineWeight"/></param>
        internal LineWeightWrapper(string displayName, LineWeight lineWeight)
        {
            DisplayName = displayName;
            LineWeight = lineWeight;
        }

        /// <summary>
        /// Отображаемое имя
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Соответствующий <see cref="Autodesk.AutoCAD.DatabaseServices.LineWeight"/>
        /// </summary>
        public LineWeight LineWeight { get; }
    }
}
