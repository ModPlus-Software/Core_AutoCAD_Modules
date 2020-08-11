namespace ModPlus.MiniPlugins
{
    using System;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.GraphicsInterface;
    using Autodesk.AutoCAD.Runtime;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
    using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

    /// <summary>
    /// Редактировать маскировку
    /// </summary>
    public class EditWipeout
    {
        private const string LangItem = "AutocadDlls";

        /// <summary>
        /// Добавить вершину маскировке
        /// </summary>
        [CommandMethod("ModPlus", "mpAddVertexToWipeout", CommandFlags.UsePickSet)]
        public void AddVertexToWipeout()
        {
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                var ed = doc.Editor;

                var selectedObjects = ed.SelectImplied();
                ObjectId selectedId;
                if (selectedObjects.Value == null || selectedObjects.Value.Count > 1)
                {
                    var peo = new PromptEntityOptions($"\n{Language.GetItem(LangItem, "msg20")}:");
                    peo.SetRejectMessage("\nWrong!");
                    peo.AllowNone = false;
                    peo.AddAllowedClass(typeof(Wipeout), true);

                    var ent = ed.GetEntity(peo);
                    if (ent.Status != PromptStatus.OK)
                        return;

                    selectedId = ent.ObjectId;
                }
                else
                {
                    selectedId = selectedObjects.Value[0].ObjectId;
                }

                if (selectedId != ObjectId.Null)
                    AddVertexToCurrentWipeout(selectedId);
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        /// <summary>
        /// Удалить вершину маскировки
        /// </summary>
        [CommandMethod("ModPlus", "mpRemoveVertexFromWipeout", CommandFlags.UsePickSet)]
        public void RemoveVertexToWipeout()
        {
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                var ed = doc.Editor;

                var selectedObjects = ed.SelectImplied();
                ObjectId selectedId;
                if (selectedObjects.Value == null || selectedObjects.Value.Count > 1)
                {
                    var peo = new PromptEntityOptions($"\n{Language.GetItem(LangItem, "msg20")}:");
                    peo.SetRejectMessage("\nWrong!");
                    peo.AllowNone = false;
                    peo.AddAllowedClass(typeof(Wipeout), true);

                    var ent = ed.GetEntity(peo);
                    if (ent.Status != PromptStatus.OK)
                        return;

                    selectedId = ent.ObjectId;
                }
                else
                {
                    selectedId = selectedObjects.Value[0].ObjectId;
                }

                if (selectedId != ObjectId.Null)
                    RemoveVertexFromCurrentWipeout(selectedId);
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private static void AddVertexToCurrentWipeout(ObjectId wipeoutId)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var loop = true;
            while (loop)
            {
                using (doc.LockDocument())
                {
                    using (var tr = doc.TransactionManager.StartTransaction())
                    {
                        var wipeout = tr.GetObject(wipeoutId, OpenMode.ForWrite) as Wipeout;
                        if (wipeout != null)
                        {
                            var points3D = wipeout.GetVertices();

                            var polyline = new Polyline();
                            for (var i = 0; i < points3D.Count; i++)
                            {
                                polyline.AddVertexAt(i, new Point2d(points3D[i].X, points3D[i].Y), 0.0, 0.0, 0.0);
                            }

                            var jig = new AddVertexJig();
                            var result = jig.StartJig(polyline);
                            if (result.Status != PromptStatus.OK)
                            {
                                loop = false;
                            }
                            else
                            {
                                polyline.AddVertexAt(jig.Vertex() + 1, jig.PickedPoint(), 0.0, 0.0, 0.0);
                                var new2DPoints = new Point2dCollection();
                                for (var i = 0; i < polyline.NumberOfVertices; i++)
                                {
                                    new2DPoints.Add(polyline.GetPoint2dAt(i));
                                }

                                wipeout.SetFrom(new2DPoints, polyline.Normal);
                            }
                        }

                        tr.Commit();
                    }
                }
            }
        }

        private static void RemoveVertexFromCurrentWipeout(ObjectId wipeoutId)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            var loop = true;
            while (loop)
            {
                using (doc.LockDocument())
                {
                    using (var tr = doc.TransactionManager.StartTransaction())
                    {
                        var wipeout = tr.GetObject(wipeoutId, OpenMode.ForWrite) as Wipeout;
                        if (wipeout != null)
                        {
                            var points3D = wipeout.GetVertices();
                            if (points3D.Count > 3)
                            {
                                var polyline = new Polyline();
                                for (var i = 0; i < points3D.Count; i++)
                                {
                                    polyline.AddVertexAt(i, new Point2d(points3D[i].X, points3D[i].Y), 0.0, 0.0, 0.0);
                                }

                                var pickedPt = ed.GetPoint($"\n{Language.GetItem(LangItem, "msg22")}:");
                                if (pickedPt.Status != PromptStatus.OK)
                                {
                                    loop = false;
                                }
                                else
                                {
                                    var pt = polyline.GetClosestPointTo(pickedPt.Value, false);
                                    var param = polyline.GetParameterAtPoint(pt);
                                    var vertex = Convert.ToInt32(Math.Truncate(param));
                                    polyline.RemoveVertexAt(vertex);
                                    var new2DPoints = new Point2dCollection();
                                    for (var i = 0; i < polyline.NumberOfVertices; i++)
                                    {
                                        new2DPoints.Add(polyline.GetPoint2dAt(i));
                                    }

                                    wipeout.SetFrom(new2DPoints, polyline.Normal);
                                }
                            }
                            else
                            {
                                // message
                                loop = false;
                            }
                        }

                        tr.Commit();
                    }
                }
            }
        }

        private class AddVertexJig : DrawJig
        {
            private Point3d _prevPoint;
            private Point3d _currentPoint;
            private Point3d _startPoint;
            private Polyline _pLine;
            private int _vertex;

            public PromptResult StartJig(Polyline pLine)
            {
                _pLine = pLine;
                _prevPoint = _pLine.GetPoint3dAt(0);
                _startPoint = _pLine.GetPoint3dAt(0);

                return Application.DocumentManager.MdiActiveDocument.Editor.Drag(this);
            }

            public int Vertex()
            {
                return _vertex;
            }

            public Point2d PickedPoint()
            {
                return new Point2d(_currentPoint.X, _currentPoint.Y);
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                var ppo = new JigPromptPointOptions($"\n{Language.GetItem(LangItem, "msg21")}:")
                {
                    BasePoint = _startPoint,
                    UseBasePoint = true,
                    UserInputControls = UserInputControls.Accept3dCoordinates
                                        | UserInputControls.NullResponseAccepted
                                        | UserInputControls.AcceptOtherInputString
                                        | UserInputControls.NoNegativeResponseAccepted
                };

                var ppr = prompts.AcquirePoint(ppo);

                if (ppr.Status != PromptStatus.OK)
                    return SamplerStatus.Cancel;

                if (ppr.Status == PromptStatus.OK)
                {
                    _currentPoint = ppr.Value;

                    if (CursorHasMoved())
                    {
                        _prevPoint = _currentPoint;
                        return SamplerStatus.OK;
                    }

                    return SamplerStatus.NoChange;
                }

                return SamplerStatus.NoChange;
            }

            protected override bool WorldDraw(WorldDraw draw)
            {
                var mods = System.Windows.Forms.Control.ModifierKeys;
                var control = (mods & System.Windows.Forms.Keys.Control) > 0;
                var pt = _pLine.GetClosestPointTo(_currentPoint, false);
                var param = _pLine.GetParameterAtPoint(pt);
                _vertex = Convert.ToInt32(Math.Truncate(param));
                var maxVx = _pLine.NumberOfVertices - 1;
                if (control)
                {
                    if (_vertex < maxVx)
                        _vertex++;
                }

                if (_vertex != maxVx)
                {
                    // Если вершина не последня
                    var line1 = new Line(_pLine.GetPoint3dAt(_vertex), _currentPoint);
                    draw.Geometry.Draw(line1);
                    var line2 = new Line(_pLine.GetPoint3dAt(_vertex + 1), _currentPoint);
                    draw.Geometry.Draw(line2);
                }
                else
                {
                    var line1 = new Line(_pLine.GetPoint3dAt(_vertex), _currentPoint);
                    draw.Geometry.Draw(line1);
                    if (_pLine.Closed)
                    {
                        // Если полилиния замкнута, то рисуем отрезок к первой вершине
                        var line2 = new Line(_pLine.GetPoint3dAt(0), _currentPoint);
                        draw.Geometry.Draw(line2);
                    }
                }

                return true;
            }

            private bool CursorHasMoved()
            {
                return _currentPoint.DistanceTo(_prevPoint) > Tolerance.Global.EqualPoint;
            }
        }
    }
}
