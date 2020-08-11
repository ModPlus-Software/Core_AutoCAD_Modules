namespace ModPlus.MiniPlugins
{
    using Autodesk.AutoCAD.ApplicationServices.Core;
    using Autodesk.AutoCAD.Colors;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Runtime;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Сделать вложения "ПоБлоку"
    /// </summary>
    public class NestedEntitiesByBlock
    {
        private const string LangItem = "AutocadDlls";

        /// <summary>
        /// Задать вхождения ПоБлоку
        /// </summary>
        [CommandMethod("ModPlus", "mpEntByBlock", CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public void Start()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            try
            {
                var selectedObjects = ed.SelectImplied();
                if (selectedObjects.Value == null)
                {
                    var pso = new PromptSelectionOptions
                    {
                        MessageForAdding = $"\n{Language.GetItem(LangItem, "msg2")}",
                        MessageForRemoval = "\n",
                        AllowSubSelections = false,
                        AllowDuplicates = false
                    };
                    var psr = ed.GetSelection(pso);
                    if (psr.Status != PromptStatus.OK)
                        return;
                    selectedObjects = psr;
                }

                if (selectedObjects.Value.Count > 0)
                {
                    using (var tr = doc.TransactionManager.StartTransaction())
                    {
                        foreach (SelectedObject so in selectedObjects.Value)
                        {
                            var selEnt = tr.GetObject(so.ObjectId, OpenMode.ForRead);
                            if (selEnt is BlockReference blockReference)
                            {
                                ChangeProperties(blockReference.BlockTableRecord);
                            }
                        }

                        tr.Commit();
                    }

                    ed.Regen();
                }
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        public static void ChangeProperties(ObjectId objectId)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;

            using (var tr = doc.TransactionManager.StartTransaction())
            {
                var btr = (BlockTableRecord)tr.GetObject(objectId, OpenMode.ForRead);

                foreach (var entId in btr)
                {
                    var ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;

                    if (ent != null)
                    {
                        var br = ent as BlockReference;
                        if (br != null)
                        {
                            // recursive
                            ChangeProperties(br.BlockTableRecord);
                        }
                        else
                        {
                            ent.UpgradeOpen();
                            ent.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);
                            ent.LineWeight = LineWeight.ByBlock;
                            ent.Linetype = "ByBlock";
                            ent.DowngradeOpen();
                        }
                    }
                }

                tr.Commit();
            }
        }
    }
}
