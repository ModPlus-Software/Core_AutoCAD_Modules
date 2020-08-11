namespace ModPlus.MiniPlugins
{
    using Autodesk.AutoCAD.ApplicationServices.Core;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Runtime;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Windows.MiniPlugins;

    public class NestedEntitiesLayer
    {
        private const string LangItem = "AutocadDlls";

        [CommandMethod("ModPlus", "mpNestedEntLayer", CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public void EntriesLayer()
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
                    var selectLayerWin = new SelectLayer();
                    if (selectLayerWin.ShowDialog() == true && selectLayerWin.LbLayers.SelectedIndex != -1)
                    {
                        var selectedLayer = (SelectLayer.SelLayer)selectLayerWin.LbLayers.SelectedItem;
                        using (var tr = doc.TransactionManager.StartTransaction())
                        {
                            foreach (SelectedObject so in selectedObjects.Value)
                            {
                                var selEnt = tr.GetObject(so.ObjectId, OpenMode.ForRead);
                                if (selEnt is BlockReference blockReference)
                                {
                                    ChangeLayer(blockReference.BlockTableRecord, selectedLayer.LayerId);
                                }
                            }

                            tr.Commit();
                        }

                        ed.Regen();
                    }
                }
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private static void ChangeLayer(ObjectId objectId, ObjectId layerId)
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
                           NestedEntitiesByBlock.ChangeProperties(br.BlockTableRecord);
                        }
                        else
                        {
                            ent.UpgradeOpen();
                            ent.SetLayerId(layerId, true);
                            ent.DowngradeOpen();
                        }
                    }
                }

                tr.Commit();
            }
        }
    }
}
