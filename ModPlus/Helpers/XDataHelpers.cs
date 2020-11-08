namespace ModPlus.Helpers
{
    using System;
    using Autodesk.AutoCAD.DatabaseServices;
    using JetBrains.Annotations;
    using ModPlusAPI.Windows;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    /// <summary>
    /// Вспомогательные методы для работы с расширенными данными (XData)
    /// </summary>
    [PublicAPI]
    public static class XDataHelpers
    {
        /// <summary>Добавление (регистрация) имени приложения</summary>
        /// <param name="regAppName">Регистрационное имя приложения</param>
        public static void AddRegAppTableRecord(string regAppName)
        {
            var db = HostApplicationServices.WorkingDatabase;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead, false);
                if (!rat.Has(regAppName))
                {
                    rat.UpgradeOpen();
                    var tableRecord = new RegAppTableRecord { Name = regAppName };
                    rat.Add(tableRecord);
                    tr.AddNewlyCreatedDBObject(tableRecord, true);
                }

                tr.Commit();
            }
        }

        /// <summary>Добавление текстовых расширенных данных в именованный словарь</summary>
        /// <param name="dictionaryName">Словарь</param>
        /// <param name="value">Добавляемое значение</param>
        public static void SetStringXData(string dictionaryName, string value)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                var db = doc.Database;
                try
                {
                    using (doc.LockDocument())
                    {
                        using (var tr = db.TransactionManager.StartTransaction())
                        {
                            var rec = new Xrecord
                            {
                                Data = new ResultBuffer(new TypedValue(Convert.ToInt32(DxfCode.Text), value))
                            };

                            var dict = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite, false);
                            dict.SetAt(dictionaryName, rec);
                            tr.AddNewlyCreatedDBObject(rec, true);
                            tr.Commit();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExceptionBox.Show(ex);
                }
            }
        }

        /// <summary>Получение текстовых расширенных данных из указанного именованного словаря</summary>
        /// <param name="dictionaryName">Словарь</param>
        /// <returns>Строковое значение. В случае ошибки или отсутствия словаря возвращается string.Empty</returns>
        public static string GetStringXData(string dictionaryName)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                var db = doc.Database;
                try
                {
                    using (doc.LockDocument())
                    {
                        using (var tr = db.TransactionManager.StartTransaction())
                        {
                            var dict = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite, true);
                            var id = dict.GetAt(dictionaryName);
                            var rec = tr.GetObject(id, OpenMode.ForWrite, true) as Xrecord;
                            var value = string.Empty;
                            if (rec != null)
                            {
                                foreach (var rb in rec.Data.AsArray())
                                {
                                    value = rb.Value.ToString();
                                }
                            }

                            tr.Commit();
                            return value;
                        }
                    }
                } // try
                catch
                {
                    return string.Empty;
                }
            }

            return string.Empty;
        }

        /// <summary>Проверка наличия именованного словаря расширенных данных по имени</summary>
        /// <param name="dictionaryName">Словарь</param>
        /// <returns>True - словарь существует, False - словарь отсутствует или произошла ошибка</returns>
        public static bool HasXDataDictionary(string dictionaryName)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                var db = doc.Database;
                try
                {
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        using (doc.LockDocument())
                        {
                            var dict = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite, true);
                            if (dict.Contains(dictionaryName))
                            {
                                tr.Commit();
                                return true;
                            }

                            tr.Commit();
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExceptionBox.Show(ex);
                    return false;
                }
            }

            return false;
        }

        /// <summary>Удаление значения расширенных данных из именованного словаря. Поиск словаря происходит по указанному значению</summary>
        /// <param name="value">Удаляемое значение</param>
        public static void DeleteStringXData(string value)
        {
            var database = AcApp.DocumentManager.MdiActiveDocument.Database;
            try
            {
                using (AcApp.DocumentManager.MdiActiveDocument.LockDocument())
                {
                    using (var tr = database.TransactionManager.StartTransaction())
                    {
                        var newValue = new Xrecord();
                        var values = new[] { new TypedValue(Convert.ToInt32(DxfCode.XRefPath), value) };
                        newValue.Data = new ResultBuffer(values);
                        var dictionary = (DBDictionary)tr.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForWrite, false);
                        foreach (var obj in dictionary)
                        {
                            if (obj.Value.GetObject(OpenMode.ForRead) is Xrecord)
                            {
                                var rec = obj.Value.GetObject(OpenMode.ForRead) as Xrecord;
                                var rb = rec?.Data;
                                var tv = rb?.AsArray();
                                var rb2 = newValue.Data;
                                var tv2 = rb2.AsArray();
                                if (((TypedValue)tv.GetValue(0)).Value.Equals(
                                    ((TypedValue)tv2.GetValue(0)).Value))
                                {
                                    dictionary.Remove(obj.Key);
                                    break;
                                }
                            }
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
    }
}
