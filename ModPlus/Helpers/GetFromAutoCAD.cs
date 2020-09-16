namespace ModPlus.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    /// <summary>
    /// Методы получения различных данных из AutoCAD
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class GetFromAutoCAD
    {
        private const string LangItem = "AutocadDlls";

        /// <summary>Получения расстояния между двумя указанными точками в виде double</summary>
        /// <param name="showError">Отображать окно ошибки (Exception) в случае возникновения</param>
        /// <returns>Полученное расстояние или double.NaN в случае отмены или ошибки</returns>
        public static double GetLenByTwoPoint(bool showError = false)
        {
            try
            {
                using (AcApp.DocumentManager.MdiActiveDocument.LockDocument())
                {
                    var ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
                    var pdo = new PromptDistanceOptions($"\n{Language.GetItem(LangItem, "msg10")}");
                    var pdr = ed.GetDistance(pdo);
                    return pdr.Status != PromptStatus.OK ? double.NaN : pdr.Value;
                }
            }
            catch (Exception ex)
            {
                if (showError)
                    ExceptionBox.Show(ex);
                return double.NaN;
            }
        }

        /// <summary>Получение суммы длин выбранных примитивов: отрезки, полилинии, дуги, сплайны, эллипсы</summary>
        /// <param name="sumLen">Сумма длин выбранных примитивов</param>
        public static void GetLenFromEntities(out double sumLen)
        {
            // Сумма длин
            sumLen = 0.0;
            var lens = new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            try
            {
                var selRes = ed.SelectImplied();

                // Если сначала ничего не выбрано, просим выбрать:
                if (selRes.Status == PromptStatus.Error)
                {
                    var selOpts = new PromptSelectionOptions
                    {
                        MessageForAdding = $"\n{Language.GetItem(LangItem, "msg1")}"
                    };
                    TypedValue[] values =
                    {
                        new TypedValue((int)DxfCode.Operator, "<OR"),
                        new TypedValue((int)DxfCode.Start, "LINE"),
                        new TypedValue((int)DxfCode.Start, "POLYLINE"),
                        new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                        new TypedValue((int)DxfCode.Start, "CIRCLE"),
                        new TypedValue((int)DxfCode.Start, "ARC"),
                        new TypedValue((int)DxfCode.Start, "SPLINE"),
                        new TypedValue((int)DxfCode.Start, "ELLIPSE"),
                        new TypedValue((int)DxfCode.Operator, "OR>")
                    };
                    var selectionFilter = new SelectionFilter(values);
                    selRes = ed.GetSelection(selOpts, selectionFilter);
                }
                else
                {
                    ed.SetImpliedSelection(new ObjectId[0]);
                }

                if (selRes.Status == PromptStatus.OK)
                {
                    using (var tr = doc.TransactionManager.StartTransaction())
                    {
                        try
                        {
                            var objIds = selRes.Value.GetObjectIds();
                            foreach (var objId in objIds)
                            {
                                var ent = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                                switch (ent.GetType().Name)
                                {
                                    case "Line":
                                        lens[0] += ((Line)ent).Length;
                                        break;
                                    case "Circle":
                                        lens[1] += ((Circle)ent).Circumference;
                                        break;
                                    case "Polyline":
                                        lens[2] += ((Polyline)ent).Length;
                                        break;
                                    case "Arc":
                                        lens[3] += ((Arc)ent).Length;
                                        break;
                                    case "Spline":
                                        lens[4] +=
                                        ((Curve)ent).GetDistanceAtParameter(((Curve)ent).EndParam) -
                                        ((Curve)ent).GetDistanceAtParameter(((Curve)ent).StartParam);
                                        break;
                                    case "Ellipse":
                                        lens[5] +=
                                        ((Curve)ent).GetDistanceAtParameter(((Curve)ent).EndParam) -
                                        ((Curve)ent).GetDistanceAtParameter(((Curve)ent).StartParam);
                                        break;
                                }

                                ent.Dispose();
                            }

                            // Общая сумма длин
                            sumLen += lens.Sum();
                            tr.Commit();
                        }
                        catch (Exception ex)
                        {
                            ExceptionBox.Show(ex);
                            tr.Commit();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionBox.Show(ex);
            }
        }

        /// <summary>Получение суммы длин выбранных примитивов: отрезки, полилинии, дуги, сплайны, эллипсы</summary>
        /// <param name="sumLen">Сумма длин всех примитивов</param>
        /// <param name="entities">Поддерживаемые примитивы</param>
        /// <param name="count">Количество примитивов</param>
        /// <param name="lens">Сумма длин для каждого примитива</param>
        /// <param name="objectIds">Список ObjectId выбранных примитивов</param>
        [Obsolete]
        public static void GetLenFromEntities(
            out double sumLen,
            out List<string> entities,
            out List<int> count,
            out List<double> lens,
            out List<List<ObjectId>> objectIds)
        {
            // Поддерживаемые примитивы
            entities = new List<string> { "Line", "Circle", "Polyline", "Arc", "Spline", "Ellipse" };

            // Выбранное количество
            count = new List<int> { 0, 0, 0, 0, 0, 0 };

            // Сумма длин
            sumLen = 0.0;
            lens = new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };

            // Список ObjectId
            objectIds = new List<List<ObjectId>>
            {
                new List<ObjectId>(),
                new List<ObjectId>(),
                new List<ObjectId>(),
                new List<ObjectId>(),
                new List<ObjectId>(),
                new List<ObjectId>()
            };

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            try
            {
                var selRes = ed.SelectImplied();

                // Если сначала ничего не выбрано, просим выбрать:
                if (selRes.Status == PromptStatus.Error)
                {
                    var selOpts = new PromptSelectionOptions
                    {
                        MessageForAdding =
                            $"\n{Language.GetItem(LangItem, "msg1")}"
                    };
                    TypedValue[] values =
                    {
                        new TypedValue((int)DxfCode.Operator, "<OR"),
                        new TypedValue((int)DxfCode.Start, "LINE"),
                        new TypedValue((int)DxfCode.Start, "POLYLINE"),
                        new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                        new TypedValue((int)DxfCode.Start, "CIRCLE"),
                        new TypedValue((int)DxfCode.Start, "ARC"),
                        new TypedValue((int)DxfCode.Start, "SPLINE"),
                        new TypedValue((int)DxfCode.Start, "ELLIPSE"),
                        new TypedValue((int)DxfCode.Operator, "OR>")
                    };
                    var selectionFilter = new SelectionFilter(values);
                    selRes = ed.GetSelection(selOpts, selectionFilter);
                }
                else
                {
                    ed.SetImpliedSelection(new ObjectId[0]);
                }

                if (selRes.Status == PromptStatus.OK)
                {
                    using (var tr = doc.TransactionManager.StartTransaction())
                    {
                        try
                        {
                            var objIds = selRes.Value.GetObjectIds();
                            foreach (var objId in objIds)
                            {
                                var ent = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                                switch (ent.GetType().Name)
                                {
                                    case "Line":
                                        count[0]++;
                                        objectIds[0].Add(objId);
                                        lens[0] += ((Line)ent).Length;
                                        break;
                                    case "Circle":
                                        count[1]++;
                                        objectIds[1].Add(objId);
                                        lens[1] += ((Circle)ent).Circumference;
                                        break;
                                    case "Polyline":
                                        count[2]++;
                                        objectIds[2].Add(objId);
                                        lens[2] += ((Polyline)ent).Length;
                                        break;
                                    case "Arc":
                                        count[3]++;
                                        objectIds[3].Add(objId);
                                        lens[3] += ((Arc)ent).Length;
                                        break;
                                    case "Spline":
                                        count[4]++;
                                        objectIds[4].Add(objId);
                                        lens[4] +=
                                        ((Curve)ent).GetDistanceAtParameter(((Curve)ent).EndParam) -
                                        ((Curve)ent).GetDistanceAtParameter(((Curve)ent).StartParam);
                                        break;
                                    case "Ellipse":
                                        count[5]++;
                                        objectIds[5].Add(objId);
                                        lens[5] +=
                                        ((Curve)ent).GetDistanceAtParameter(((Curve)ent).EndParam) -
                                        ((Curve)ent).GetDistanceAtParameter(((Curve)ent).StartParam);
                                        break;
                                }

                                ent.Dispose();
                            }

                            // Общая сумма длин
                            sumLen += lens.Sum();
                            tr.Commit();
                        }
                        catch (Exception ex)
                        {
                            ExceptionBox.Show(ex);
                            tr.Commit();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionBox.Show(ex);
            }
        }
    }
}
