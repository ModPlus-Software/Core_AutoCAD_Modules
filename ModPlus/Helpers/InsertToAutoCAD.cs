namespace ModPlus.Helpers
{
    using System;
    using System.Collections.Generic;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.GraphicsInterface;
    using JetBrains.Annotations;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    /// <summary>
    /// Функции вставки/добавления в AutoCAD
    /// </summary>
    [PublicAPI]
    public class InsertToAutoCad
    {
        private const string LangItem = "AutocadDlls";

        /// <summary>
        /// Вставить строковое значение в ячейку таблицы AutoCAD
        /// </summary>
        /// <param name="firstStr">Первая строка. Замена разделителя действует только для неё</param>
        /// <param name="secondString">Вторая строка. Необязательно. Замена разделителя не действует</param>
        /// <param name="useSeparator">Использовать разделитель (для цифровых значений).
        /// Заменяет запятую на точку, а затем на текущий разделитель</param>
        public static void AddStringToAutoCadTableCell(string firstStr, string secondString, bool useSeparator)
        {
            try
            {
                var db = AcApp.DocumentManager.MdiActiveDocument.Database;
                var ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
                using (AcApp.DocumentManager.MdiActiveDocument.LockDocument())
                {
                    var peo = new PromptEntityOptions($"\n{Language.GetItem(LangItem, "msg11")}");
                    peo.SetRejectMessage($"\n{Language.GetItem(LangItem, "msg13")}");
                    peo.AddAllowedClass(typeof(Table), false);
                    var per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                    {
                        return;
                    }

                    var tr = db.TransactionManager.StartTransaction();
                    using (tr)
                    {
                        try
                        {
                            var entId = per.ObjectId;
                            var tbl = (Table)tr.GetObject(entId, OpenMode.ForWrite);
                            var ppo = new PromptPointOptions($"\n{Language.GetItem(LangItem, "msg12")}");
                            var end = false;
                            var vector = new Vector3d(0.0, 0.0, 1.0);
                            while (end == false)
                            {
                                var ppr = ed.GetPoint(ppo);
                                if (ppr.Status != PromptStatus.OK)
                                    return;
                                try
                                {
                                    var tableHitTestInfo = tbl.HitTest(ppr.Value.TransformBy(ed.CurrentUserCoordinateSystem), vector);
                                    if (tableHitTestInfo.Type == TableHitTestType.Cell)
                                    {
                                        var cell = new Cell(tbl, tableHitTestInfo.Row, tableHitTestInfo.Column);
                                        if (useSeparator)
                                        {
                                            cell.TextString =
                                                ModPlusAPI.IO.String.ReplaceSeparator(firstStr) + secondString;
                                        }
                                        else
                                        {
                                            cell.TextString = firstStr + secondString;
                                        }

                                        end = true;
                                    }
                                }
                                catch
                                {
                                    MessageBox.Show(Language.GetItem(LangItem, "msg14"));
                                }
                            }

                            tr.Commit();
                        }
                        catch (Exception ex)
                        {
                            ExceptionBox.Show(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionBox.Show(ex);
            }
        }
        
        /// <summary>
        /// Вставка элемента спецификации в строку таблицы AutoCAD с выбором таблицы и указанием строки
        /// </summary>
        /// <param name="specificationItemForTable">Экземпляр вспомогательного элемента для заполнения строительной спецификации</param>
        public static void AddSpecificationItemToTableRow(SpecificationItemForTable specificationItemForTable)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            using (doc.LockDocument())
            {
                var options = new PromptEntityOptions($"\n{Language.GetItem(LangItem, "msg11")}");
                options.SetRejectMessage($"\n{Language.GetItem(LangItem, "msg13")}");
                options.AddAllowedClass(typeof(Table), false);
                var entity = ed.GetEntity(options);
                if (entity.Status == PromptStatus.OK)
                {
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var table = (Table)tr.GetObject(entity.ObjectId, OpenMode.ForWrite);
                        var selectedRow = 2;
                        var ppo = new PromptPointOptions($"\n{Language.GetItem(LangItem, "msg12")}");
                        var end = false;
                        var vector = new Vector3d(0.0, 0.0, 1.0);
                        while (end == false)
                        {
                            var ppr = ed.GetPoint(ppo);
                            if (ppr.Status != PromptStatus.OK)
                                return;
                            try
                            {
                                var tableHitTestInfo = table.HitTest(ppr.Value.TransformBy(ed.CurrentUserCoordinateSystem), vector);
                                if (tableHitTestInfo.Type == TableHitTestType.Cell)
                                {
                                    selectedRow = tableHitTestInfo.Row;
                                    end = true;
                                }
                            }
                            catch
                            {
                                MessageBox.Show(Language.GetItem(LangItem, "msg14"));
                            }
                        }

                        TableHelpers.AddSpecificationItemToTableRow(table, selectedRow, specificationItemForTable);
                        tr.Commit();
                    }
                }
            }
        }

        /// <summary>
        /// Вставка элемента спецификации в указанную строку выбранной таблицы AutoCAD
        /// </summary>
        /// <param name="specificationItemForTable">Экземпляр вспомогательного элемента для заполнения строительной спецификации</param>
        /// <param name="table">Таблица AutoCAD</param>
        /// <param name="rowNumber">Номер строки для вставки элемента спецификации</param>
        /// <remarks>Метод нужно вызывать внутри открытой транзакции и открытой на запись таблицы</remarks>
        public static void AddSpecificationItemToTableRow(
            SpecificationItemForTable specificationItemForTable, Table table, int rowNumber)
        {
            TableHelpers.AddSpecificationItemToTableRow(table, rowNumber, specificationItemForTable);
        }

        /// <summary>
        /// Вставка нескольких элементов спецификации в таблицу AutoCAD с выбором таблицы
        /// </summary>
        /// <param name="specificationItemsForTable">Список экземпляров вспомогательного элемента для заполнения
        /// строительной спецификации</param>
        /// <param name="askForSelectRow">Указание пользователем строки, с которой начинается заполнение</param>
        /// <remarks>Метод проверяет в таблице наличие нужного количества пустых строк и в случае их нехватки
        /// добавляет новые</remarks>
        public static void AddSpecificationItemsToTable(
            List<SpecificationItemForTable> specificationItemsForTable,
            bool askForSelectRow)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            using (doc.LockDocument())
            {
                var options = new PromptEntityOptions($"\n{Language.GetItem(LangItem, "msg11")}");
                options.SetRejectMessage($"\n{Language.GetItem(LangItem, "msg13")}");
                options.AddAllowedClass(typeof(Table), false);
                var entity = ed.GetEntity(options);
                if (entity.Status == PromptStatus.OK)
                {
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var table = (Table)tr.GetObject(entity.ObjectId, OpenMode.ForWrite);
                        var startRow = 2;
                        if (!askForSelectRow)
                        {
                            if (table.TableStyleName.Equals("Mp_GOST_P_21.1101_F8"))
                            {
                                startRow = 3;
                            }

                            TableHelpers.CheckAndAddRowCount(table, startRow, specificationItemsForTable.Count, out var firstEmptyRow);
                            TableHelpers.FillTableRows(table, firstEmptyRow, specificationItemsForTable);
                        }
                        else
                        {
                            var ppo = new PromptPointOptions($"\n{Language.GetItem(LangItem, "msg15")}");
                            var end = false;
                            var vector = new Vector3d(0.0, 0.0, 1.0);
                            while (end == false)
                            {
                                var ppr = ed.GetPoint(ppo);
                                if (ppr.Status != PromptStatus.OK)
                                    return;
                                try
                                {
                                    var tableHitTestInfo = table.HitTest(ppr.Value.TransformBy(ed.CurrentUserCoordinateSystem), vector);
                                    if (tableHitTestInfo.Type == TableHitTestType.Cell)
                                    {
                                        startRow = tableHitTestInfo.Row;
                                        end = true;
                                    }
                                }
                                catch
                                {
                                    MessageBox.Show(Language.GetItem(LangItem, "msg14"));
                                }
                            }

                            TableHelpers.CheckAndAddRowCount(table, startRow, specificationItemsForTable.Count, out _);
                            TableHelpers.FillTableRows(table, startRow, specificationItemsForTable);
                        }

                        tr.Commit();
                    }
                }
            }
        }

        /// <summary>Вставка нескольких элементов спецификации в выбранную таблицу AutoCAD</summary>
        /// <param name="table">Таблица AutoCAD</param>
        /// <param name="rowNumber">Номер строки с которой начинается заполнение таблицы</param>
        /// <param name="specificationItemsForTable">Список экземпляров вспомогательного элемента для заполнения строительной спецификации</param>
        /// <remarks>Метод проверяет в таблице наличие нужного количества пустых строк и в случае их нехватки добавляет новые</remarks>
        /// <remarks>Метод нужно вызывать внутри открытой транзакции и открытой на запись таблицы</remarks>
        public static void AddSpecificationItemsToTable(Table table, int rowNumber,
            List<SpecificationItemForTable> specificationItemsForTable)
        {
            TableHelpers.FillTableRows(table, rowNumber, specificationItemsForTable);
        }

        /// <summary>Элемент для заполнения строительной спецификации</summary>
        public class SpecificationItemForTable
        {
            /// <summary>Инициализация экземпляра вспомогательного элемента для заполнения спецификации</summary>
            /// <param name="position">Позиция</param>
            /// <param name="designation">Обозначение</param>
            /// <param name="name">Наименование</param>
            /// <param name="mass">Масса</param>
            /// <param name="count">Количество</param>
            /// <param name="note">Примечание</param>
            public SpecificationItemForTable(
                string position, string designation, string name,
                string mass, string count, string note)
            {
                Position = position;
                Designation = designation;
                Name = name;
                Mass = mass;
                Count = count;
                Note = note;
            }

            /// <summary>Позиция</summary>
            public string Position { get; set; }

            /// <summary>Обозначение</summary>
            public string Designation { get; set; }

            /// <summary>Наименование</summary>
            public string Name { get; set; }

            /// <summary>Масса</summary>
            public string Mass { get; set; }

            /// <summary>Количество</summary>
            public string Count { get; set; }

            /// <summary>Примечание</summary>
            public string Note { get; set; }
        }

        /// <summary>Вставка однострочного текста</summary>
        /// <param name="text">Содержимое однострочного текста</param>
        public static void InsertDbText(string text)
        {
            try
            {
                var doc = AcApp.DocumentManager.MdiActiveDocument;
                var db = doc.Database;
                var ed = doc.Editor;
                using (AcApp.DocumentManager.MdiActiveDocument.LockDocument())
                {
                    var tr = doc.TransactionManager.StartTransaction();
                    using (tr)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false);
                        var dbText = new DBText();
                        dbText.SetDatabaseDefaults();
                        dbText.TextString = text;
                        dbText.TransformBy(ed.CurrentUserCoordinateSystem);
                        var jig = new DTextJig(dbText);
                        var pr = ed.Drag(jig);
                        if (pr.Status == PromptStatus.OK)
                        {
                            var ent = jig.GetEntity();
                            btr.AppendEntity(ent);
                            tr.AddNewlyCreatedDBObject(ent, true);
                            doc.TransactionManager.QueueForGraphicsFlush();
                        }

                        tr.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionBox.Show(ex);
            }
        }

        /// <summary>Вставка мультивыноски</summary>
        /// <param name="txt">Содержимое мультивыноски</param>
        /// <param name="standardArrowhead">Стандартная стрелка. По умолчанию значение "_NONE"</param>
        public static void InsertMLeader(string txt, AutocadHelpers.StandardArrowhead standardArrowhead = AutocadHelpers.StandardArrowhead._NONE)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = HostApplicationServices.WorkingDatabase;
            using (doc.LockDocument())
            {
                var ppo = new PromptPointOptions($"\n{Language.GetItem(LangItem, "msg16")}") { AllowNone = true };
                var ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK)
                    return;

                // arrowHead
                var arrowHead = "_NONE";
                if (standardArrowhead != AutocadHelpers.StandardArrowhead.closedFilled)
                    arrowHead = standardArrowhead.ToString();

                // Создаем текст
                var jig = new MLeaderJig
                {
                    FirstPoint = AutocadHelpers.UcsToWcs(ppr.Value),
                    MlText = txt,
                    ArrowName = arrowHead
                };
                var res = ed.Drag(jig);
                if (res.Status == PromptStatus.OK)
                {
                    using (var tr = doc.TransactionManager.StartTransaction())
                    {
                        var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                        btr.AppendEntity(jig.MLeader());
                        tr.AddNewlyCreatedDBObject(jig.MLeader(), true);
                        tr.Commit();
                    }
                }

                doc.TransactionManager.QueueForGraphicsFlush();
            }
        }

        #region Jigs

        private class DTextJig : EntityJig
        {
            private Point3d _mCenterPt;
            private Point3d _mActualPoint;

            public DTextJig(DBText dbtxt)
                : base(dbtxt)
            {
                _mCenterPt = dbtxt.Position;
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                var jigOpts = new JigPromptPointOptions
                {
                    UserInputControls = 
                        UserInputControls.Accept3dCoordinates |
                        UserInputControls.NoZeroResponseAccepted |
                        UserInputControls.AcceptOtherInputString |
                        UserInputControls.NoNegativeResponseAccepted,
                    Message = $"\n{Language.GetItem(LangItem, "msg8")}"
                };
                var acquirePoint = prompts.AcquirePoint(jigOpts);
                if (_mActualPoint == acquirePoint.Value)
                {
                    return SamplerStatus.NoChange;
                }

                _mActualPoint = acquirePoint.Value;
                return SamplerStatus.OK;
            }

            protected override bool Update()
            {
                _mCenterPt = _mActualPoint;
                try
                {
                    ((DBText)Entity).Position = _mCenterPt;
                }
                catch (Exception)
                {
                    return false;
                }

                return true;
            }

            public Entity GetEntity()
            {
                return Entity;
            }
        }

        private class MLeaderJig : DrawJig
        {
            private MLeader _mleader;
            public Point3d FirstPoint;
            private Point3d _secondPoint;
            private Point3d _prevPoint;
            public string MlText;
            public string ArrowName;

            public MLeader MLeader()
            {
                return _mleader;
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                var jpo = new JigPromptPointOptions
                {
                    BasePoint = FirstPoint,
                    UseBasePoint = true,
                    UserInputControls = UserInputControls.Accept3dCoordinates |
                                        UserInputControls.GovernedByUCSDetect,
                    Message = $"\n{Language.GetItem(LangItem, "msg8")}"
                };

                var res = prompts.AcquirePoint(jpo);
                _secondPoint = res.Value;
                if (res.Status != PromptStatus.OK)
                    return SamplerStatus.Cancel;
                if (CursorHasMoved())
                {
                    _prevPoint = _secondPoint;
                    return SamplerStatus.OK;
                }

                return SamplerStatus.NoChange;
            }

            private bool CursorHasMoved()
            {
                return _secondPoint.DistanceTo(_prevPoint) > 1e-6;
            }

            protected override bool WorldDraw(WorldDraw draw)
            {
                var wg = draw.Geometry;
                if (wg != null)
                {
                    ObjectId arrId = AutocadHelpers.GetArrowObjectId(ArrowName);

                    var mtxt = new MText();
                    mtxt.SetDatabaseDefaults();
                    mtxt.Contents = MlText;
                    mtxt.Location = _secondPoint;
                    mtxt.Annotative = AnnotativeStates.True;
                    mtxt.TransformBy(AcApp.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem);

                    _mleader = new MLeader();
                    var ldNum = _mleader.AddLeader();
                    _mleader.AddLeaderLine(ldNum);
                    _mleader.SetDatabaseDefaults();
                    _mleader.ContentType = ContentType.MTextContent;
                    _mleader.ArrowSymbolId = arrId;
                    _mleader.MText = mtxt;
                    _mleader.TextAlignmentType = TextAlignmentType.LeftAlignment;
                    _mleader.TextAttachmentType = TextAttachmentType.AttachmentBottomOfTopLine;
                    _mleader.TextAngleType = TextAngleType.HorizontalAngle;
                    _mleader.EnableAnnotationScale = true;
                    _mleader.Annotative = AnnotativeStates.True;
                    _mleader.AddFirstVertex(ldNum, FirstPoint);
                    _mleader.AddLastVertex(ldNum, _secondPoint);
                    _mleader.LeaderLineType = LeaderType.StraightLeader;
                    _mleader.EnableDogleg = false;
                    _mleader.DoglegLength = 0.0;
                    _mleader.LandingGap = 1.0;
                    _mleader.TextHeight = double.Parse(AcApp.GetSystemVariable("TEXTSIZE").ToString());

                    draw.Geometry.Draw(_mleader);
                }

                return true;
            }
        }
        #endregion
    }

    internal static class TableHelpers
    {
        private const string LangItem = "AutocadDlls";

        public static bool CheckColumnsCount(int columns, int need)
        {
            return columns == need || MessageBox.ShowYesNo(
                Language.GetItem(LangItem, "msg17"), MessageBoxIcon.Question);
        }

        public static void CheckAndAddRowCount(Table table, int startRow, int sItemsCount, out int firstEmptyRow)
        {
            var rows = table.Rows.Count;
            var firstRow = startRow;
            firstEmptyRow = startRow; // Первая пустая строка

            // Пробегаем по всем ячейкам и проверяем "чистоту" таблицы
            var empty = true;
            var stopLoop = false;
            for (var i = startRow; i <= table.Rows.Count - 1; i++)
            {
                for (var j = 0; j < table.Columns.Count; j++)
                {
                    if (!table.Cells[i, j].TextString.Equals(string.Empty))
                    {
                        empty = false;
                        stopLoop = true;
                        break;
                    }
                }

                if (stopLoop)
                    break;
            }

            // Если не пустая
            if (!empty)
            {
                if (!MessageBox.ShowYesNo(
                    Language.GetItem(LangItem, "msg18"),
                    MessageBoxIcon.Question))
                {
                    // Если "Нет", тогда ищем последнюю пустую строку
                    // Если последняя строка не пуста, то добавляем 
                    // еще строчку, иначе...
                    var findEmpty = true;
                    for (var j = 0; j < table.Columns.Count; j++)
                    {
                        if (!string.IsNullOrEmpty(table.Cells[rows - 1, j].TextString))
                        {
                            // table.InsertRows(rows, 8, 1);
                            table.InsertRowsAndInherit(rows, rows - 1, 1);
                            rows++;
                            firstRow = rows - 1; // Так как таблица не обновляется
                            findEmpty = false; // чтобы не искать последнюю пустую
                            break;
                        }
                    }

                    if (findEmpty)
                    {
                        // идем по таблице в обратном порядке.
                        stopLoop = false;
                        for (var i = rows - 1; i >= 2; i--)
                        {
                            // Сделаем счетчик k
                            // Если ячейка пустая - будем увеличивать, а иначе - обнулять
                            var k = 1;
                            for (var j = 0; j < table.Columns.Count; j++)
                            {
                                if (table.Cells[i, j].TextString.Equals(string.Empty))
                                {
                                    firstRow = i;
                                    k++;

                                    // Если счетчик k равен количеству колонок
                                    // значит вся строка пустая и можно тормозить цикл
                                    if (k == table.Columns.Count)
                                    {
                                        stopLoop = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    stopLoop = true;
                                    break;
                                }
                            }

                            if (stopLoop)
                                break;
                        }

                        // Разбиваем ячейки
                        ////////////////////////////////////////
                    }
                }

                // Если "да", то очищаем таблицу
                else
                {
                    for (var i = startRow; i <= rows - 1; i++)
                    {
                        for (var j = 0; j < table.Columns.Count; j++)
                        {
                            table.Cells[i, j].TextString = string.Empty;
                            table.Cells[i, j].IsMergeAllEnabled = false;
                        }
                    }
                }
            }

            // Если в таблице мало строк
            if (sItemsCount > rows - firstRow)
                table.InsertRowsAndInherit(firstRow, firstRow, sItemsCount - (rows - firstRow) + 1);

            // После всех манипуляций ищем первую пустую строчку
            for (var j = 0; j < rows; j++)
            {
                var isEmpty = table.Rows[j].IsEmpty;
                if (isEmpty != null && isEmpty.Value)
                {
                    firstEmptyRow = j;
                    break;
                }
            }
        }

        public static void FillTableRows(Table table, int firstRow, 
            List<InsertToAutoCad.SpecificationItemForTable> specificationItemsForTable)
        {
            for (var i = 0; i < specificationItemsForTable.Count; i++)
            {
                AddSpecificationItemToTableRow(table, firstRow + i, specificationItemsForTable[i]);
            }
        }

        public static void AddSpecificationItemToTableRow(
            Table table, int rowNum, InsertToAutoCad.SpecificationItemForTable specificationItemForTable)
        {
            var position = specificationItemForTable.Position?.Trim() ?? string.Empty;
            var designation = specificationItemForTable.Designation?.Trim() ?? string.Empty;
            var name = specificationItemForTable.Name?.Trim() ?? string.Empty;
            var count = specificationItemForTable.Count;
            var mass = specificationItemForTable.Mass?.Trim() ?? string.Empty;
            var note = specificationItemForTable.Note?.Trim() ?? string.Empty;
            
            // Если это таблица ModPlus
            if (table.TableStyleName.Contains("Mp_"))
            {
                if (table.TableStyleName.Equals("Mp_GOST_P_21.1101_F7") |
                    table.TableStyleName.Equals("Mp_DSTU_B_A.2.4-4_F7") |
                    table.TableStyleName.Equals("Mp_STB_2255_Z1"))
                {
                    if (CheckColumnsCount(table.Columns.Count, 6))
                    {
                        table.Cells[rowNum, 0].TextString = position;
                        table.Cells[rowNum, 1].TextString = designation;
                        table.Cells[rowNum, 2].TextString = name;
                        table.Cells[rowNum, 3].TextString = count;
                        table.Cells[rowNum, table.Columns.Count - 2].TextString = mass;
                        table.Cells[rowNum, table.Columns.Count - 1].TextString = note;
                    }
                }
                else if (table.TableStyleName.Equals("Mp_GOST_P_21.1101_F8"))
                {
                    table.Cells[rowNum, 0].TextString = position;
                    table.Cells[rowNum, 1].TextString = designation;
                    table.Cells[rowNum, 2].TextString = name;
                    table.Cells[rowNum, 3].TextString = count;
                    table.Cells[rowNum, table.Columns.Count - 2].TextString = mass;
                    table.Cells[rowNum, table.Columns.Count - 1].TextString = note;
                }
                else if (table.TableStyleName.Equals("Mp_GOST_21.501_F7"))
                {
                    if (CheckColumnsCount(table.Columns.Count, 4))
                    {
                        table.Cells[rowNum, 0].TextString = position;
                        table.Cells[rowNum, 1].TextString = name;
                        table.Cells[rowNum, 2].TextString = count;
                        table.Cells[rowNum, table.Columns.Count - 1].TextString = mass;
                    }
                }
                else if (table.TableStyleName.Equals("Mp_GOST_21.501_F8"))
                {
                    if (CheckColumnsCount(table.Columns.Count, 6))
                    {
                        table.Cells[rowNum, 1].TextString = position;
                        table.Cells[rowNum, 2].TextString = name;
                        table.Cells[rowNum, 3].TextString = count;
                        table.Cells[rowNum, table.Columns.Count - 2].TextString = mass;
                    }
                }
                else if (table.TableStyleName.Equals("Mp_GOST_2.106_F1"))
                {
                    if (CheckColumnsCount(table.Columns.Count, 7))
                    {
                        table.Cells[rowNum, 2].TextString = position;
                        table.Cells[rowNum, 3].TextString = designation;
                        table.Cells[rowNum, 4].TextString = name;
                        table.Cells[rowNum, 5].TextString = count;
                        table.Cells[rowNum, table.Columns.Count - 1].TextString = note;
                    }
                }
                else if (table.TableStyleName.Equals("Mp_GOST_2.106_F1a"))
                {
                    if (CheckColumnsCount(table.Columns.Count, 5))
                    {
                        table.Cells[rowNum, 0].TextString = position;
                        table.Cells[rowNum, 1].TextString = designation;
                        table.Cells[rowNum, 2].TextString = name;
                        table.Cells[rowNum, 3].TextString = count;
                        table.Cells[rowNum, table.Columns.Count - 1].TextString = note;
                    }
                }
            }

            // Если таблица не из плагина
            else
            {
                if (MessageBox.ShowYesNo(Language.GetItem(LangItem, "msg19"), MessageBoxIcon.Question))
                {
                    if (table.Columns.Count == 4)
                    {
                        table.Cells[rowNum, 0].TextString = position;
                        table.Cells[rowNum, 1].TextString = name;
                        table.Cells[rowNum, 2].TextString = count;
                        table.Cells[rowNum, table.Columns.Count - 1].TextString = mass;
                    }

                    if (table.Columns.Count == 5)
                    {
                        table.Cells[rowNum, 0].TextString = position;
                        table.Cells[rowNum, 1].TextString = designation;
                        table.Cells[rowNum, 2].TextString = name;
                        table.Cells[rowNum, 3].TextString = count;
                    }

                    if (table.Columns.Count >= 6)
                    {
                        table.Cells[rowNum, 0].TextString = position;
                        table.Cells[rowNum, 1].TextString = designation;
                        table.Cells[rowNum, 2].TextString = name;
                        table.Cells[rowNum, 3].TextString = count;
                        table.Cells[rowNum, table.Columns.Count - 2].TextString = mass;
                        table.Cells[rowNum, table.Columns.Count - 1].TextString = note;
                    }
                }
            }
        }
    }
}
