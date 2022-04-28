using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;

namespace CreationModelPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel:IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            CreateWalls(doc);
            //CreateWindows(doc);
           
            return Result.Succeeded;
        }
        public void CreateWalls(Document doc)
        {
            
            List<Level> listLevel = new FilteredElementCollector(doc)
                 .OfClass(typeof(Level))
                 .OfType<Level>()
                 .ToList();

            Level level1 = listLevel
                .Where(x => x.Name.Equals("Уровень 1"))
                .OfType<Level>()
                .FirstOrDefault();
            Level level2 = listLevel
               .Where(x => x.Name.Equals("Уровень 2"))
               .OfType<Level>()
               .FirstOrDefault();

            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();


            Transaction transaction = new Transaction(doc, "Построение стен");

            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }
          
            AddDoor(doc, level1, walls[0]);
           
           


            transaction.Commit();

            transaction.Start();
           
            AddWindow(doc, level1, walls[1]);
            AddWindow(doc, level1, walls[2]);
            AddWindow(doc, level1, walls[3]);

            transaction.Commit();
 
            transaction.Start();

           AddRoof(doc, listLevel, walls);

            transaction.Commit();

            return;
        }

        public void AddRoof(Document doc, List<Level> listLevel, List<Wall> walls)
        {
            // RoofType roofType = new FilteredElementCollector(doc)
            //     .OfClass(typeof(RoofType))
            //     .OfType<RoofType>()
            //     .Where(x => x.Name.Equals("Типовой - 125мм"))
            //     .Where(x => x.FamilyName.Equals("Базовая крыша"))
            //     .FirstOrDefault();

            // View view = new FilteredElementCollector(doc)
            //.OfClass(typeof(View))
            //     .OfType<View>()
            //     .Where(x => x.Name.Equals("Уровень 1"))
            //     .FirstOrDefault();

            // double wallWight = walls[0].Width;
            // double dt = wallWight / 2;

            // double extrusionStart = -width / 2 - dt;
            // double extrusionEnd = width / 2 + dt;

            // double curveStart = -depth / 2 - dt;
            // double curveEnd = depth / 2 + dt;

            // CurveArray curveArray = new CurveArray();
            // curveArray.Append(Line.CreateBound(new XYZ(0, curveStart, level2.Elevation), new XYZ(0, 0, level2.Elevation + 10)));
            // curveArray.Append(Line.CreateBound(new XYZ(0, 0, level2.Elevation + 10),new XYZ(0, curveStart, level2.Elevation) ));

            // ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20), new XYZ(0, 20, 0), view);
            // ExtrusionRoof extrusionRoof = doc.Create.NewExtrusionRoof(curveArray, plane, level2, roofType, extrusionStart, extrusionEnd);
            // extrusionRoof.EaveCuts = EaveCutterType.TwoCutSquare;

            var level = listLevel
                  .Where(x => x.Name.Equals("Уровень 2"))
                  .FirstOrDefault();

            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .OfType<RoofType>()
                .Where(x => x.Name.Equals("Типовой - 400мм"))
                .Where(x => x.FamilyName.Equals("Базовая крыша"))
                .FirstOrDefault();

            double wallWidth = walls[0].Width;
            double df = wallWidth / 2;
            double dh = level.get_Parameter(BuiltInParameter.LEVEL_ELEV).AsDouble();
            XYZ dt = new XYZ(-df, -df, dh);
            XYZ dz = new XYZ(0, 0, 20);
            XYZ dy = new XYZ(0, 20, 0);
            LocationCurve locationCurve = walls[0].Location as LocationCurve;
            XYZ point = locationCurve.Curve.GetEndPoint(0);
            double l = (walls[0].Location as LocationCurve).Curve.Length + df * 2;
            double w = ((walls[1].Location as LocationCurve).Curve.Length / 2) + df;
            XYZ origin = point + dt;
            XYZ vy = XYZ.BasisY;
            XYZ vz = XYZ.BasisZ;

            CurveArray curve = new CurveArray();
            curve.Append(Line.CreateBound(origin, origin + new XYZ(0, w, 5)));
            curve.Append(Line.CreateBound(origin + new XYZ(0, w, 5), origin + new XYZ(0, w * 2, 0)));


            var av = doc.ActiveView;
            Transaction transaction = new Transaction(doc, "Создание крыши");
            transaction.Start();

            ReferencePlane plane = doc.Create.NewReferencePlane2(origin, origin + vz, origin + vy, av);

            ExtrusionRoof extrusionRoof = doc.Create.NewExtrusionRoof(curve, plane, level, roofType, 0, 3);

            transaction.Commit();
        }

        private void AddWindow(Document doc, Level level1, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0610 x 1830 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            //LocationCurve hostCurve = wall.Location as LocationCurve;
            //XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            //XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            //XYZ point = (point1 + point2 / 2);
            XYZ point = GetElementCenter(wall);


            if (!windowType.IsActive)
            {
                windowType.Activate();
            }

            doc.Create.NewFamilyInstance(point, windowType, wall, level1, StructuralType.NonStructural);
        }

        public XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }

        private void AddDoor(Document doc, Level level1, Wall wall)
        {
           FamilySymbol doorType =  new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2)/2;

            if (!doorType.IsActive)
            {
                doorType.Activate();
            }

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
        }
    }  
   
}