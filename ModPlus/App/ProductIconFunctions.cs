namespace ModPlus.App
{
    using Autodesk.AutoCAD.ApplicationServices.Core;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using ModPlusAPI;

    public class ProductIconFunctions
    {
        /// <summary>Включить идентификационные иконки для примитивов, имеющих расширенные данные продуктов ModPlus</summary>
        [CommandMethod("mpShowProductIcons")]
        public static void ShowIcon()
        {
            if (ProductsDrawableOverrule.ProductsDrawableOverruleInstance == null)
            {
                UserConfigFile.SetValue("mpProductInsert", "ShowIcon", true.ToString(), true);
                Overrule.AddOverrule(RXObject.GetClass(typeof(Entity)), ProductsDrawableOverrule.Instance(), true);
                Overrule.Overruling = true;
                Application.DocumentManager.MdiActiveDocument.Editor.Regen();
            }
        }

        /// <summary>Отключить идентификационные иконки для примитивов, имеющих расширенные данные продуктов ModPlus</summary>
        [CommandMethod("mpHideProductIcons")]
        public void HideIcon()
        {
            if (ProductsDrawableOverrule.ProductsDrawableOverruleInstance != null)
            {
                UserConfigFile.SetValue("mpProductInsert", "ShowIcon", false.ToString(), true);
                Overrule.RemoveOverrule(RXObject.GetClass(typeof(Entity)), ProductsDrawableOverrule.Instance());
                ProductsDrawableOverrule.ProductsDrawableOverruleInstance = null;
                Application.DocumentManager.MdiActiveDocument.Editor.Regen();
            }
        }
    }
}