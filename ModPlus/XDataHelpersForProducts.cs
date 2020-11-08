namespace ModPlus
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using Autodesk.AutoCAD.DatabaseServices;
    using JetBrains.Annotations;

    /// <summary>
    /// Вспомогательные методы работы с расширенными данными для функций из раздела "Продукты ModPlus"
    /// </summary>
    [PublicAPI]
    public static class XDataHelpersForProducts
    {
        private const string AppName = "ModPlusProduct";

        public static bool IsModPlusProduct(this Entity ent)
        {
            using (var rb = ent.GetXDataForApplication(AppName))
            {
                return rb != null;
            }
        }

        public static void SaveDataToEntity(object product, DBObject ent, Transaction tr)
        {
            var regTable = (RegAppTable)tr.GetObject(ent.Database.RegAppTableId, OpenMode.ForWrite);
            if (!regTable.Has(AppName))
            {
                var app = new RegAppTableRecord
                {
                    Name = AppName
                };
                regTable.Add(app);
                tr.AddNewlyCreatedDBObject(app, true);
            }

            using (var resBuf = SaveToResBuf(product))
            {
                ent.XData = resBuf;
            }
        }

        public static object NewFromEntity(Entity ent)
        {
            using (var resBuf = ent.GetXDataForApplication(AppName))
            {
                return resBuf == null 
                    ? null 
                    : NewFromResBuf(resBuf);
            }
        }

        private static object NewFromResBuf(ResultBuffer resBuf)
        {
            var bf = new BinaryFormatter { Binder = new MyBinder() };

            var ms = MyUtil.ResBufToStream(resBuf);

            var mbc = bf.Deserialize(ms);

            return mbc;
        }

        private static ResultBuffer SaveToResBuf(object product)
        {
            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, product);
            ms.Position = 0;

            var resBuf = MyUtil.StreamToResBuf(ms, AppName);

            return resBuf;
        }

        public sealed class MyBinder : SerializationBinder
        {
            public override Type BindToType(
                string assemblyName,
                string typeName)
            {
                return Type.GetType($"{typeName}, {assemblyName}");
            }
        }

        public class MyUtil
        {
            const int KMaxChunkSize = 127;

            public static ResultBuffer StreamToResBuf(
                Stream ms, string appName)
            {
                var resBuf = new ResultBuffer(
                    new TypedValue(
                        (int)DxfCode.ExtendedDataRegAppName, appName));
                for (var i = 0; i < ms.Length; i += KMaxChunkSize)
                {
                    var length = (int)Math.Min(ms.Length - i, KMaxChunkSize);
                    var datachunk = new byte[length];
                    ms.Read(datachunk, 0, length);
                    resBuf.Add(
                        new TypedValue(
                            (int)DxfCode.ExtendedDataBinaryChunk, datachunk));
                }

                return resBuf;
            }

            public static MemoryStream ResBufToStream(ResultBuffer resBuf)
            {
                var ms = new MemoryStream();
                var values = resBuf.AsArray();

                // Start from 1 to skip application name

                for (var i = 1; i < values.Length; i++)
                {
                    var datachunk = (byte[])values[i].Value;
                    ms.Write(datachunk, 0, datachunk.Length);
                }

                ms.Position = 0;

                return ms;
            }
        }
    }
}