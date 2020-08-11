// ReSharper disable InconsistentNaming
namespace ModPlus.MiniPlugins
{
    using System;
    using System.Runtime.InteropServices;
    using Windows.MiniPlugins;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.Colors;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.GraphicsInterface;
    using ModPlusAPI;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
    using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

    /// <summary>
    /// Мини-функции
    /// </summary>
    public class MiniFunctions
    {
        /// <summary>
        /// Включение/отключение всех контекстных меню мини-функций в зависимости от настроек
        /// </summary>
        public static void LoadUnloadContextMenu()
        {
            // ent by block
            var entByBlockObjContMen = !bool.TryParse(UserConfigFile.GetValue("EntByBlockOCM"), out bool b) || b;
            if (entByBlockObjContMen)
                MiniFunctionsContextMenuExtensions.EntByBlockObjectContextMenu.Attach();
            else
                MiniFunctionsContextMenuExtensions.EntByBlockObjectContextMenu.Detach();

            // nested ent layer
            var nestedEntLayerObjContMen = !bool.TryParse(UserConfigFile.GetValue("NestedEntLayerOCM"), out b) || b;
            if (nestedEntLayerObjContMen)
                MiniFunctionsContextMenuExtensions.NestedEntLayerObjectContextMenu.Attach();
            else
                MiniFunctionsContextMenuExtensions.NestedEntLayerObjectContextMenu.Detach();

            // Fast block
            var fastBlocksContextMenu = !bool.TryParse(UserConfigFile.GetValue("FastBlocksCM"), out b) || b;
            if (fastBlocksContextMenu)
                MiniFunctionsContextMenuExtensions.FastBlockContextMenu.Attach();
            else
                MiniFunctionsContextMenuExtensions.FastBlockContextMenu.Detach();

            // VP to MS
            var VPtoMSObjConMen = !bool.TryParse(UserConfigFile.GetValue("VPtoMS"), out b) || b;
            if (VPtoMSObjConMen)
                MiniFunctionsContextMenuExtensions.VPtoMSObjectContextMenu.Attach();
            else
                MiniFunctionsContextMenuExtensions.VPtoMSObjectContextMenu.Detach();

            // wipeout vertex edit
            /*
             * Так как не получается создать контекстное меню конкретно на класс Wipeout (возможно в поздних версиях устранили),
             * то приходится делать через подписку на событие и создание меню у Entity
             */
            var wipeoutEditOCM = !bool.TryParse(UserConfigFile.GetValue("WipeoutEditOCM"), out b) || b; // true
            if (wipeoutEditOCM)
            {
                AcApp.DocumentManager.DocumentCreated += WipeoutEditOCM_Documents_DocumentCreated;
                AcApp.DocumentManager.DocumentActivated += WipeoutEditOCM_Documents_DocumentActivated;

                foreach (Document document in AcApp.DocumentManager)
                {
                    document.ImpliedSelectionChanged += WipeoutEditOCM_Document_ImpliedSelectionChanged;
                }
            }
            else
            {
                AcApp.DocumentManager.DocumentCreated -= WipeoutEditOCM_Documents_DocumentCreated;
                AcApp.DocumentManager.DocumentActivated -= WipeoutEditOCM_Documents_DocumentActivated;

                foreach (Document document in AcApp.DocumentManager)
                {
                    document.ImpliedSelectionChanged -= WipeoutEditOCM_Document_ImpliedSelectionChanged;
                }
            }
        }

        /// <summary>
        /// Отключить все контекстные меню
        /// </summary>
        public static void UnloadAll()
        {
            MiniFunctionsContextMenuExtensions.EntByBlockObjectContextMenu.Detach();
            MiniFunctionsContextMenuExtensions.NestedEntLayerObjectContextMenu.Detach();
            MiniFunctionsContextMenuExtensions.FastBlockContextMenu.Detach();
            MiniFunctionsContextMenuExtensions.VPtoMSObjectContextMenu.Detach();

            // wipeout vertex edit
            /*
             * Так как не получается создать контекстное меню конкретно на класс Wipeout (возможно в поздних версиях устранили),
             * то приходится делать через подписку на событие и создание меню у Entity
             */
            AcApp.DocumentManager.DocumentCreated -= WipeoutEditOCM_Documents_DocumentCreated;
            AcApp.DocumentManager.DocumentActivated -= WipeoutEditOCM_Documents_DocumentActivated;

            foreach (Document document in AcApp.DocumentManager)
            {
                document.ImpliedSelectionChanged -= WipeoutEditOCM_Document_ImpliedSelectionChanged;
            }
        }

        private static void WipeoutEditOCM_Documents_DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document != null)
            {
                e.Document.ImpliedSelectionChanged -= WipeoutEditOCM_Document_ImpliedSelectionChanged;
                e.Document.ImpliedSelectionChanged += WipeoutEditOCM_Document_ImpliedSelectionChanged;
            }
        }

        private static void WipeoutEditOCM_Documents_DocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document != null)
            {
                e.Document.ImpliedSelectionChanged -= WipeoutEditOCM_Document_ImpliedSelectionChanged;
                e.Document.ImpliedSelectionChanged += WipeoutEditOCM_Document_ImpliedSelectionChanged;
            }
        }

        private static void WipeoutEditOCM_Document_ImpliedSelectionChanged(object sender, EventArgs e)
        {
            PromptSelectionResult psr = AcApp.DocumentManager.MdiActiveDocument.Editor.SelectImplied();
            bool detach = true;
            if (psr.Value != null && psr.Value.Count == 1)
            {
                using (AcApp.DocumentManager.MdiActiveDocument.LockDocument())
                {
                    using (OpenCloseTransaction tr = new OpenCloseTransaction())
                    {
                        foreach (SelectedObject selectedObject in psr.Value)
                        {
                            if (selectedObject.ObjectId == ObjectId.Null)
                                continue;
                            var obj = tr.GetObject(selectedObject.ObjectId, OpenMode.ForRead);
                            if (obj is Wipeout)
                            {
                                MiniFunctionsContextMenuExtensions.WipeoutEditObjectContextMenu.Attach();
                                detach = false;
                            }
                        }

                        tr.Commit();
                    }
                }
            }

            if (detach)
                MiniFunctionsContextMenuExtensions.WipeoutEditObjectContextMenu.Detach();
        }
    }
}
