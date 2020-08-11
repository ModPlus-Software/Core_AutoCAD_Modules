namespace ModPlus.MiniPlugins
{
    using System;
    using System.IO;
    using System.Linq;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using Autodesk.AutoCAD.Windows;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    public class MiniFunctionsContextMenuExtensions
    {
        private const string LangItem = "AutocadDlls";

        public static class EntByBlockObjectContextMenu
        {
            public static ContextMenuExtension ContextMenu;

            public static void Attach()
            {
                if (ContextMenu == null)
                {
                    // For Entity
                    ContextMenu = new ContextMenuExtension();
                    var miEnt = new MenuItem(Language.GetItem(LangItem, "h49"));
                    miEnt.Click += StartFunction;
                    ContextMenu.MenuItems.Add(miEnt);

                    var rxcEnt = RXObject.GetClass(typeof(BlockReference));
                    Application.AddObjectContextMenuExtension(rxcEnt, ContextMenu);
                }
            }

            public static void Detach()
            {
                if (ContextMenu != null)
                {
                    var rxcEnt = RXObject.GetClass(typeof(BlockReference));
                    Application.RemoveObjectContextMenuExtension(rxcEnt, ContextMenu);
                    ContextMenu = null;
                }
            }

            public static void StartFunction(object o, EventArgs e)
            {
                Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.SendStringToExecute(
                    "_.mpEntByBlock ", false, false, false);
            }
        }

        public static class NestedEntLayerObjectContextMenu
        {
            public static ContextMenuExtension ContextMenu;

            public static void Attach()
            {
                if (ContextMenu == null)
                {
                    // For Entity
                    ContextMenu = new ContextMenuExtension();
                    var miEnt = new MenuItem(Language.GetItem(LangItem, "h59"));
                    miEnt.Click += StartFunction;
                    ContextMenu.MenuItems.Add(miEnt);

                    var rxcEnt = RXObject.GetClass(typeof(BlockReference));
                    Application.AddObjectContextMenuExtension(rxcEnt, ContextMenu);
                }
            }

            public static void Detach()
            {
                if (ContextMenu != null)
                {
                    var rxcEnt = RXObject.GetClass(typeof(BlockReference));
                    Application.RemoveObjectContextMenuExtension(rxcEnt, ContextMenu);
                    ContextMenu = null;
                }
            }

            public static void StartFunction(object o, EventArgs e)
            {
                Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.SendStringToExecute(
                    "_.mpNestedEntLayer ", false, false, false);
            }
        }

        public static class FastBlockContextMenu
        {
            public static ContextMenuExtension ContextMenu;

            public static void Attach()
            {
                if (ContextMenu == null)
                {
                    if (File.Exists(UserConfigFile.FullFileName))
                    {
                        var configXml = UserConfigFile.ConfigFileXml;
                        var settingsXml = configXml?.Element("Settings");
                        var fastBlocksXml = settingsXml?.Element("mpFastBlocks");
                        if (fastBlocksXml != null)
                        {
                            if (fastBlocksXml.Elements("FastBlock").Any())
                            {
                                ContextMenu = new ContextMenuExtension { Title = Language.GetItem(LangItem, "h50") };
                                foreach (var fbXml in fastBlocksXml.Elements("FastBlock"))
                                {
                                    var mi = new MenuItem(fbXml.Attribute("Name").Value);
                                    mi.Click += Mi_Click;
                                    ContextMenu.MenuItems.Add(mi);
                                }

                                Application.AddDefaultContextMenuExtension(ContextMenu);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show(Language.GetItem(LangItem, "err4"), MessageBoxIcon.Close);
                    }
                }
            }

            public static void Detach()
            {
                if (ContextMenu != null)
                {
                    Application.RemoveDefaultContextMenuExtension(ContextMenu);
                    ContextMenu = null;
                }
            }

            private static void Mi_Click(object sender, EventArgs e)
            {
                try
                {
                    if (sender is MenuItem mi)
                    {
                        if (File.Exists(UserConfigFile.FullFileName))
                        {
                            var configXml = UserConfigFile.ConfigFileXml;
                            var settingsXml = configXml?.Element("Settings");
                            var fastBlocksXml = settingsXml?.Element("mpFastBlocks");
                            if (fastBlocksXml != null)
                            {
                                if (fastBlocksXml.Elements("FastBlock").Any())
                                {
                                    foreach (var fbXml in fastBlocksXml.Elements("FastBlock"))
                                    {
                                        if (fbXml.Attribute("Name").Value.Equals(mi.Text))
                                        {
                                            InsertBlock(
                                                fbXml.Attribute("File").Value,
                                                fbXml.Attribute("BlockName").Value);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show(Language.GetItem(LangItem, "err4"), MessageBoxIcon.Close);
                        }
                    }
                }
                catch (System.Exception exception)
                {
                    ExceptionBox.Show(exception);
                }
            }

            private static void InsertBlock(string file, string blockName)
            {
                var dm = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager;
                var ed = dm.MdiActiveDocument.Editor;
                var destDb = dm.MdiActiveDocument.Database;
                var sourceDb = new Database(false, true);

                // Read the DWG into a side database
                sourceDb.ReadDwgFile(file, FileShare.Read, true, String.Empty);

                // Create a variable to store the list of block identifiers
                var blockIds = new ObjectIdCollection();
                using (dm.MdiActiveDocument.LockDocument())
                {
                    using (var sourceT = sourceDb.TransactionManager.StartTransaction())
                    {
                        // Open the block table
                        var bt = (BlockTable)sourceT.GetObject(sourceDb.BlockTableId, OpenMode.ForRead, false);

                        // Check each block in the block table
                        foreach (var btrId in bt)
                        {
                            var btr = (BlockTableRecord)sourceT.GetObject(btrId, OpenMode.ForRead, false);

                            // Only add named & non-layout blocks to the copy list
                            if (btr.Name.Equals(blockName))
                            {
                                blockIds.Add(btrId);
                                break;
                            }

                            btr.Dispose();
                        }
                    }

                    // Copy blocks from source to destination database
                    var mapping = new IdMapping();
                    sourceDb.WblockCloneObjects(
                        blockIds,
                        destDb.BlockTableId,
                        mapping,
                        DuplicateRecordCloning.Replace,
                        false);
                    sourceDb.Dispose();

                    // Вставка

                    using (var tr = destDb.TransactionManager.StartTransaction())
                    {
                        var bt = (BlockTable)tr.GetObject(destDb.BlockTableId, OpenMode.ForRead, false);
                        BlockInsertion.InsertBlockRef(0, tr, destDb, ed, bt[blockName]);

                        tr.Commit();
                    }
                }
            }
        }

        public static class BlockInsertion
        {
            /// <summary>
            /// Вставка блока с атрибутами
            /// </summary>
            /// <param name="promptCounter">0 - только вставка, 1 - с поворотом</param>
            /// <param name="tr">Транзакция</param>
            /// <param name="db">База данных чертежа</param>
            /// <param name="ed">Editor</param>
            /// <param name="blkdefid">ObjectId блока</param>
            public static ObjectId InsertBlockRef(
                int promptCounter,
                Transaction tr,
                Database db,
                Editor ed,
                ObjectId blkdefid)
            {
                var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                var blkref = new BlockReference(Point3d.Origin, blkdefid);
                var id = btr.AppendEntity(blkref);
                tr.AddNewlyCreatedDBObject(blkref, true);
                var jig = new BlockRefJig(blkref);
                jig.SetPromptCounter(0);
                var res = ed.Drag(jig);
                if (res.Status == PromptStatus.OK)
                {
                    if (promptCounter == 1)
                    {
                        jig.SetPromptCounter(promptCounter);
                        res = ed.Drag(jig);
                        if (res.Status == PromptStatus.OK)
                        {
                            return id;
                        }
                    }
                    else
                    {
                        return id;
                    }
                }

                blkref.Erase();
                return ObjectId.Null;
            }

            internal class BlockRefJig : EntityJig
            {
                Point3d m_Position, m_BasePoint;
                double m_Angle;
                int m_PromptCounter;
                Matrix3d m_Ucs;
                Matrix3d m_Mat;
                Editor ed = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor;

                public BlockRefJig(BlockReference blkref)
                    : base(blkref)
                {
                    m_Position = new Point3d();
                    m_Angle = 0;
                    m_Ucs = ed.CurrentUserCoordinateSystem;
                    Update();
                }

                protected override SamplerStatus Sampler(JigPrompts prompts)
                {
                    switch (m_PromptCounter)
                    {
                        case 0:
                        {
                            var jigOpts = new JigPromptPointOptions(
                                $"\n{Language.GetItem(LangItem, "msg8")}");
                            jigOpts.UserInputControls =
                                UserInputControls.Accept3dCoordinates |
                                UserInputControls.NoZeroResponseAccepted |
                                UserInputControls.NoNegativeResponseAccepted;
                            var res = prompts.AcquirePoint(jigOpts);
                            var pnt = res.Value;
                            if (pnt != m_Position)
                            {
                                m_Position = pnt;
                                m_BasePoint = m_Position;
                            }
                            else
                            {
                                return SamplerStatus.NoChange;
                            }

                            if (res.Status == PromptStatus.Cancel)
                                return SamplerStatus.Cancel;

                            return SamplerStatus.OK;
                        }

                        case 1:
                        {
                            var jigOpts = new JigPromptAngleOptions(
                                $"\n{Language.GetItem(LangItem, "msg9")}");
                            jigOpts.UserInputControls =
                                UserInputControls.Accept3dCoordinates |
                                UserInputControls.NoNegativeResponseAccepted |
                                UserInputControls.GovernedByUCSDetect |
                                UserInputControls.UseBasePointElevation;
                            jigOpts.Cursor = CursorType.RubberBand;
                            jigOpts.UseBasePoint = true;
                            jigOpts.BasePoint = m_BasePoint;
                            var res = prompts.AcquireAngle(jigOpts);
                            var angleTemp = res.Value;
                            if (angleTemp != m_Angle)
                                m_Angle = angleTemp;
                            else
                                return SamplerStatus.NoChange;
                            if (res.Status == PromptStatus.Cancel)
                                return SamplerStatus.Cancel;

                            return SamplerStatus.OK;
                        }

                        default:
                            return SamplerStatus.NoChange;
                    }
                }

                protected override bool Update()
                {
                    try
                    {
                        /*Ucs?Jig???:
                            * 1.?????Wcs???,?//xy??
                            * 2.?????????Ucs?
                            * 3.????Ucs???
                            * 4.?????Wcs
                            */
                        var blkref = (BlockReference)Entity;
                        blkref.Normal = Vector3d.ZAxis;
                        blkref.Position = m_Position.TransformBy(ed.CurrentUserCoordinateSystem);
                        blkref.Rotation = m_Angle;
                        blkref.TransformBy(m_Ucs);
                    }
                    catch
                    {
                        return false;
                    }

                    return true;
                }

                public void SetPromptCounter(int i)
                {
                    if (i == 0 || i == 1)
                    {
                        m_PromptCounter = i;
                    }
                }
            }
        }

        public static class VPtoMSObjectContextMenu
        {
            public static ContextMenuExtension ContextMenuForVP;
            public static ContextMenuExtension ContextMenuForCurve;

            public static void Attach()
            {
                if (ContextMenuForVP == null)
                {
                    ContextMenuForVP = new ContextMenuExtension();
                    var miEnt = new MenuItem(Language.GetItem(LangItem, "h51"));
                    miEnt.Click += StartFunction;
                    ContextMenuForVP.MenuItems.Add(miEnt);

                    var rxcEnt = RXObject.GetClass(typeof(Viewport));
                    Application.AddObjectContextMenuExtension(rxcEnt, ContextMenuForVP);
                }

                if (ContextMenuForCurve == null)
                {
                    ContextMenuForCurve = new ContextMenuExtension();
                    var miEnt = new MenuItem(Language.GetItem(LangItem, "h51"));
                    miEnt.Click += StartFunction;
                    ContextMenuForCurve.MenuItems.Add(miEnt);

                    var rxcEnt = RXObject.GetClass(typeof(Curve));
                    Application.AddObjectContextMenuExtension(rxcEnt, ContextMenuForCurve);
                }
            }

            public static void Detach()
            {
                if (ContextMenuForVP != null)
                {
                    var rxcEnt = RXObject.GetClass(typeof(Viewport));
                    Application.RemoveObjectContextMenuExtension(rxcEnt, ContextMenuForVP);
                    ContextMenuForVP = null;
                }

                if (ContextMenuForCurve != null)
                {
                    var rxcEnt = RXObject.GetClass(typeof(Curve));
                    Application.RemoveObjectContextMenuExtension(rxcEnt, ContextMenuForCurve);
                    ContextMenuForCurve = null;
                }
            }

            public static void StartFunction(object o, EventArgs e)
            {
                Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.SendStringToExecute(
                    "_.mpVPtoMS ", false, false, false);
            }
        }

        public static class WipeoutEditObjectContextMenu
        {
            public static ContextMenuExtension ContextMenu;

            public static void Attach()
            {
                if (ContextMenu == null)
                {
                    ContextMenu = new ContextMenuExtension();
                    var mi1 = new MenuItem(Language.GetItem(LangItem, "h54"));
                    mi1.Click += Mi1_Click;
                    ContextMenu.MenuItems.Add(mi1);
                    var mi2 = new MenuItem(Language.GetItem(LangItem, "h55"));
                    mi2.Click += Mi2_Click;
                    ContextMenu.MenuItems.Add(mi2);
                }

                var rxClass = RXObject.GetClass(typeof(Entity));
                Application.AddObjectContextMenuExtension(rxClass, ContextMenu);
            }

            public static void Detach()
            {
                if (ContextMenu != null)
                {
                    var rxcEnt = RXObject.GetClass(typeof(Entity));
                    Application.RemoveObjectContextMenuExtension(rxcEnt, ContextMenu);

                    // ContextMenu = null;
                }
            }

            private static void Mi2_Click(object sender, EventArgs e)
            {
                Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.SendStringToExecute(
                    "_.mpRemoveVertexFromWipeout ", false, false, false);
            }

            private static void Mi1_Click(object sender, EventArgs e)
            {
                Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.SendStringToExecute(
                    "_.mpAddVertexToWipeout ", false, false, false);
            }
        }
    }
}