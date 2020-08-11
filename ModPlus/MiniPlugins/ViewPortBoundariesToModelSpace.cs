namespace ModPlus.MiniPlugins
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Autodesk.AutoCAD.ApplicationServices.Core;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Создать границы видового экрана в пространстве модели
    /// </summary>
    public class ViewPortBoundariesToModelSpace
    {
        private const string LangItem = "AutocadDlls";

        [DllImport("accore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "acedTrans")]
        private static extern int acedTrans(double[] point, IntPtr fromRb, IntPtr toRb, int disp, double[] result);

        [CommandMethod("ModPlus", "mpVPtoMS", CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public void Start()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;
            try
            {
                var objectId = ObjectId.Null;
                var selection = ed.SelectImplied();
                if (selection.Value.Count == 2)
                {
                    using (var tr = doc.TransactionManager.StartTransaction())
                    {
                        foreach (SelectedObject selectedObject in selection.Value)
                        {
                            if (tr.GetObject(selectedObject.ObjectId, OpenMode.ForRead) is Viewport)
                                continue;
                            objectId = selectedObject.ObjectId;
                        }

                        tr.Dispose();
                    }
                }
                else if (selection.Value.Count == 1)
                {
                    using (var tr = doc.TransactionManager.StartTransaction())
                    {
                        if (tr.GetObject(selection.Value[0].ObjectId, OpenMode.ForRead) is Viewport)
                            objectId = selection.Value[0].ObjectId;
                        tr.Dispose();
                    }
                }
                else
                {
                    var peo = new PromptEntityOptions($"\n{Language.GetItem(LangItem, "msg3")}");
                    peo.SetRejectMessage("\nReject");
                    peo.AllowNone = false;
                    peo.AllowObjectOnLockedLayer = true;
                    peo.AddAllowedClass(typeof(Viewport), true);
                    peo.AddAllowedClass(typeof(Polyline), true);
                    peo.AddAllowedClass(typeof(Polyline2d), true);
                    peo.AddAllowedClass(typeof(Curve), true);
                    var per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                        return;
                    objectId = per.ObjectId;
                }

                if (objectId == ObjectId.Null)
                    return;
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    Viewport viewport;
                    var ent = tr.GetObject(objectId, OpenMode.ForRead);
                    if (ent is Viewport)
                    {
                        viewport = ent as Viewport;
                    }
                    else
                    {
                        var lm = LayoutManager.Current;
                        var vpid = lm.GetNonRectangularViewportIdFromClipId(objectId);
                        if (vpid != ObjectId.Null)
                        {
                            var clipVp = tr.GetObject(vpid, OpenMode.ForRead);
                            if (clipVp is Viewport)
                                viewport = clipVp as Viewport;
                            else
                                return;
                        }
                        else
                        {
                            MessageBox.Show(Language.GetItem(LangItem, "msg4"), MessageBoxIcon.Alert);
                            return;
                        }
                    }

                    // Переключаемся в пространство листа
                    ed.SwitchToPaperSpace();

                    // Номер текущего видового экрана
                    var vpNumber = (short)viewport.Number;

                    // Получаем точки границ текущего ВЭ
                    var viewportContourPoints = GetViewportContourPoints(viewport, tr);

                    // Если есть точки, продолжаем
                    if (viewportContourPoints.Count == 0)
                        return;

                    // Обновляем вид
                    ed.UpdateScreen();

                    // Переходим внутрь активного ВЭ
                    ed.SwitchToModelSpace();

                    // Проверяем состояние ВЭ
                    if (viewport.Number > 0)
                    {
                        // Переключаемся в обрабатываемый ВЭ                                            
                        Application.SetSystemVariable("CVPORT", vpNumber);

                        // Переводим в модель граничные точки текущего ВЭ
                        var modelSpacePoints = ConvertViewportContourPointsToModelSpacePoints(viewportContourPoints);

                        var polyline = new Polyline();
                        for (var i = 0; i < modelSpacePoints.Count; i++)
                        {
                            polyline.AddVertexAt(i, new Point2d(modelSpacePoints[i].X, modelSpacePoints[i].Y), 0.0, 0.0, 0.0);
                        }

                        polyline.Closed = true;

                        // свойства
                        polyline.Layer = "0";

                        var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        var btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                        btr?.AppendEntity(polyline);
                        tr.AddNewlyCreatedDBObject(polyline, true);
                    }

                    // now switch back to PS
                    ed.SwitchToPaperSpace();

                    tr.Commit();
                }

                // clear selection
                ed.SetImpliedSelection(new ObjectId[0]);
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private Point3dCollection GetViewportContourPoints(Viewport viewport, Transaction tr)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            // Коллекция для точек видового экрана
            var points = new Point3dCollection();

            // Если видовой экран стандартный прямоугольный
            if (!viewport.NonRectClipOn)
            {
                // Получаем его точки
                viewport.GetGripPoints(points, new IntegerCollection(), new IntegerCollection());

                for (var i = points.Count - 1; i >= 0; i--)
                {
                    if (i >= 4)
                        points.RemoveAt(i);
                    else 
                        break;
                }
                
                // Выстраиваем точки в правильном порядке, по умолчанию они крест-накрест
                var tmp = points[2];
                points[2] = points[1];
                points[1] = tmp;
            }

            // Если видовой экран подрезанный - получаем примитив, по которому он подрезан
            else
            {
                using (var ent = tr.GetObject(viewport.NonRectClipEntityId, OpenMode.ForRead) as Entity)
                {
                    // Если это полилиния - извлекаем ее точки
                    if (ent is Polyline polyline)
                    {
                        for (var i = 0; i < polyline.NumberOfVertices; i++)
                            points.Add(polyline.GetPoint3dAt(i));
                    }
                    else if (ent is Polyline2d polyline2d)
                    {
                        foreach (ObjectId vertexId in polyline2d)
                        {
                            using (var vertex2d = tr.GetObject(vertexId, OpenMode.ForRead) as Vertex2d)
                            {
                                if (!points.Contains(vertex2d.Position))
                                    points.Add(vertex2d.Position);
                            }
                        }
                    }
                    else if (ent is Curve curve)
                    {
                        double
                            startParam = curve.StartParam,
                            endParam = curve.EndParam,
                            delParam = (endParam - startParam) / 100;

                        for (var curParam = startParam; curParam < endParam; curParam += delParam)
                        {
                            var curPt = curve.GetPointAtParameter(curParam);
                            points.Add(curPt);
                            curParam += delParam;
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            $"{Language.GetItem(LangItem, "msg5")}\n{Language.GetItem(LangItem, "msg6")}{viewport.Number}, {Language.GetItem(LangItem, "msg7")}{ent}\n");
                    }
                }
            }

            return points;
        }

        private Point3dCollection ConvertViewportContourPointsToModelSpacePoints(Point3dCollection viewPortPoints)
        {
            var points = new Point3dCollection();

            // Преобразование точки из PS в MS
            var rbPSDCS = new ResultBuffer(new TypedValue(5003, 3));
            var rbDCS = new ResultBuffer(new TypedValue(5003, 2));
            var rbWCS = new ResultBuffer(new TypedValue(5003, 0));
            var retPoint = new double[] { 0, 0, 0 };

            foreach (Point3d pnt in viewPortPoints)
            {
                // преобразуем из DCS пространства Листа (PSDCS) RTSHORT=3
                // в DCS пространства Модели текущего Видового Экрана RTSHORT=2
                acedTrans(pnt.ToArray(), rbPSDCS.UnmanagedObject, rbDCS.UnmanagedObject, 0,
                    retPoint);

                // Преобразуем из DCS пространства Модели текущего Видового Экрана RTSHORT=2
                // в WCS RTSHORT=0
                acedTrans(retPoint, rbDCS.UnmanagedObject, rbWCS.UnmanagedObject, 0, retPoint);

                // Добавляем точку в коллекцию
                var newPt = new Point3d(retPoint);
                if (!points.Contains(newPt))
                    points.Add(newPt);
            }

            return points;
        }
    }
}
