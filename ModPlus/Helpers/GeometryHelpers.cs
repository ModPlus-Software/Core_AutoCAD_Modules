namespace ModPlus.Helpers
{
    using Autodesk.AutoCAD.Geometry;
    using JetBrains.Annotations;

    /// <summary>
    /// Вспомогательные методы построения геометрии
    /// </summary>
    [PublicAPI]
    public static class GeometryHelpers
    {
        /// <summary>3Д точка по направлению. Направление берется как единичный вектор из точки pt2 к точке pt1, перемножается на указанную
        /// длину и откладывается от указанной точки</summary>
        /// <param name="pt1">Первая точка для получения единичного вектора</param>
        /// <param name="pt2">Вторая точка для получения единичного вектора</param>
        /// <param name="ptFrom">Точка от которой откладывается расстояние</param>
        /// <param name="length">Расстояние на котором нужно получить точку</param>
        /// <returns>3Д точка</returns>
        public static Point3d Point3dAtDirection(Point3d pt1, Point3d pt2, Point3d ptFrom, double length)
        {
            var pt3 = ptFrom + ((pt2 - pt1).GetNormal() * length);
            return pt3;
        }

        /// <summary>2Д точка по направлению. Направление берется как единичный вектор из точки pt2 к точке pt1, перемножается на указанную
        /// длину и откладывается от указанной точки</summary>
        /// <param name="pt1">Первая точка для получения единичного вектора</param>
        /// <param name="pt2">Вторая точка для получения единичного вектора</param>
        /// <param name="ptFrom">Точка от которой откладывается расстояние</param>
        /// <param name="length">Расстояние на котором нужно получить точку</param>
        /// <returns>2Д точка</returns>
        public static Point2d Point2dAtDirection(Point3d pt1, Point3d pt2, Point3d ptFrom, double length)
        {
            var pt3 = ptFrom + ((pt2 - pt1).GetNormal() * length);

            return ConvertPoint3dToPoint2d(pt3);
        }

        /// <summary>Получение 2d точки на продолжении условного отрезка</summary>
        /// <param name="p1">Первая точка условного отрезка</param>
        /// <param name="p2">Вторая точка условного отрезка</param>
        /// <param name="distance">Расстояние</param>
        public static Point2d GetPointToExtendLine(Point3d p1, Point3d p2, double distance)
        {
            return (p1 + ((p2 - p1).DivideBy((p2 - p1).Length) * distance)).Convert2d(new Plane());
        }

        /// <summary>Получение 2d точки на указанном расстоянии от условного отрезка, получаемого точками p1 и p2</summary>
        /// <param name="p1">Первая точка условного отрезка</param>
        /// <param name="p2">Вторая точка условного отрезка</param>
        /// <param name="distance">Расстояние в перпендикулярном направлении от отрезка</param>
        public static Point2d GetPerpendicularPoint2d(Point3d p1, Point3d p2, double distance)
        {
            return (p2 + ((p2 - p1).GetPerpendicularVector() * distance)).Convert2d(new Plane());
        }

        /// <summary>Получение 3d точки на указанном расстоянии от условного отрезка, получаемого точками p1 и p2</summary>
        /// <param name="p1">Первая точка условного отрезка</param>
        /// <param name="p2">Вторая точка условного отрезка</param>
        /// <param name="distance">Расстояние в перпендикулярном направлении от отрезка</param>
        public static Point3d GetPerpendicularPoint3d(Point3d p1, Point3d p2, double distance)
        {
            return p2 + ((p2 - p1).GetPerpendicularVector() * distance);
        }

        /// <summary>Конвертирование 2Д точки в 3Д точку с нулевой ординатой z</summary>
        /// <param name="point2D">Исходная 2Д точка</param>
        /// <returns>Результирующая 3Д точка</returns>
        public static Point3d ConvertPoint2DToPoint3D(Point2d point2D)
        {
            return new Point3d(point2D.X, point2D.Y, 0.0);
        }

        /// <summary>Конвертирование 3Д точки в 2Д точку</summary>
        /// <param name="point3d">Исходная 3Д точка</param>
        /// <returns>Результирующая 2Д точка</returns>
        public static Point2d ConvertPoint3dToPoint2d(Point3d point3d)
        {
            return new Point2d(point3d.X, point3d.Y);
        }
    }
}
