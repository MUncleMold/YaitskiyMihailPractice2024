using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YaitskiyMihailPractice2024;
using Autodesk.Revit.DB.Structure;
using System.Windows.Controls;
using Autodesk.Revit.DB.Structure.StructuralSections;
namespace Yaitskiy_Mihail_Practice
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal class BreakColumnsCommand : IExternalCommand
    {
        //Получить точку элемента
        public XYZ GetPoint(Element el)
        {
            Location loc = el.Location;
            LocationPoint lp = loc as LocationPoint;
            return lp.Point;
        }
        public void RotateColumn(FamilyInstance el1, FamilyInstance el2)
        {
            XYZ point1 = el1.FacingOrientation, point2 = el2.FacingOrientation;
            XYZ xYZ = GetPoint(el1);
            XYZ center = xYZ.Add(XYZ.BasisZ);
            el2.Location.Rotate(Line.CreateBound(xYZ, center), point1.AngleTo(point2));

        }
        
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uiapp.ActiveUIDocument.Document;
            ColumnPickFilter colFilter = new ColumnPickFilter();
            Selection sel = uiapp.ActiveUIDocument.Selection;
            
            IList<Reference> columns = sel.PickObjects(ObjectType.Element, colFilter, "Выберите колонны для разрезания");
            IList<Element> columns2 = new List<Element>();
            
            foreach (Reference re in columns)
            {
                Element el = doc.GetElement(re);
                if (el.Category.Name == "Structural Columns")
                {
                    columns2.Add(el);
                }
            }
            if (columns2.Count != 0)
            {
                ColumnToBreak colToB;
                Transaction trans = new Transaction(doc);
                BreakColumnsView view = new BreakColumnsView();
                trans.Start("col");
                foreach (Element el1 in columns2)
                {
                    //Поиск всех полов, которые пересекает выбранный пилон
                    FilteredElementCollector levelCollecor = new FilteredElementCollector(doc);
                    Outline bound = new Outline(el1.get_BoundingBox(doc.ActiveView).Min, el1.get_BoundingBox(doc.ActiveView).Max);
                    levelCollecor.WherePasses(new ElementClassFilter(typeof(Floor)));
                    levelCollecor = levelCollecor.WherePasses(new BoundingBoxIntersectsFilter(bound));
                    IList<Element> floorList = levelCollecor.ToElements();
                    floorList.Cast<Floor>();
                    //Составление списка уровней, на которых находятся полученные балки
                    IList<Level> levelList = new List<Level>();
                    foreach (Floor floor in floorList)
                    {
                        Level level = doc.GetElement(floor.LevelId) as Level;
                        levelList.Add(level);
                    }
                    if (floorList.Count > 0)
                    {
                        Element el2;
                        Floor currentFloor;
                        int cutCounter = 0;
                        XYZ xYZ = GetPoint(el1);
                        FamilyInstance familyInstance = el1 as FamilyInstance;
                        //Создание пилона под первым уровнем
                        if (el1.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).AsDouble() < 0)
                        {
                            el2 = doc.Create.NewFamilyInstance(xYZ, familyInstance.Symbol, levelList[0], StructuralType.Column);
                            el2.LookupParameter("Dv_Пилон_Длина").Set(el1.LookupParameter("Dv_Пилон_Длина").AsDouble());
                            el2.LookupParameter("Dv_Пилон_Ширина").Set(el1.LookupParameter("Dv_Пилон_Ширина").AsDouble());
                            el2.LookupParameter("Top Offset").Set(0 - floorList[0].get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble());
                            el2.LookupParameter("Base Offset").Set(el1.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).AsDouble() - levelList[0].Elevation);
                            RotateColumn(el1 as FamilyInstance, el2 as FamilyInstance);
                            cutCounter++;
                        }
                        //Создание пилона на последнем уровне
                        if (el1.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).AsDouble() > 0)
                        {
                            el2 = doc.Create.NewFamilyInstance(xYZ, familyInstance.Symbol, levelList[levelList.Count - 1], StructuralType.Column);
                            el2.LookupParameter("Dv_Пилон_Длина").Set(el1.LookupParameter("Dv_Пилон_Длина").AsDouble());
                            el2.LookupParameter("Dv_Пилон_Ширина").Set(el1.LookupParameter("Dv_Пилон_Ширина").AsDouble());
                            el2.LookupParameter("Base Offset").Set(0);
                            Level topLevel = doc.GetElement(el1.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).AsElementId()) as Level;
                            el2.LookupParameter("Top Offset").Set(el1.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).AsDouble() + (topLevel.Elevation - levelList[levelList.Count - 1].Elevation));
                            RotateColumn(el1 as FamilyInstance, el2 as FamilyInstance);
                            cutCounter++;
                        }
                        for (int i = 0; i < levelList.Count - 1; i++)
                        {
                            //Расчет расстояния между этажом, на который необходимо поставить балку, и следующим.
                            Level l1 = levelList[i], l2 = levelList[i + 1];
                            currentFloor = floorList[i + 1] as Floor;
                            double fThic = currentFloor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble();
                            double topOffset = l2.Elevation - l1.Elevation - fThic;
                            el2 = doc.Create.NewFamilyInstance(xYZ, familyInstance.Symbol, l1, StructuralType.Column);
                            //Модификация созданной балки
                            el2.LookupParameter("Top Offset").Set(topOffset);
                            el2.LookupParameter("Base Offset").Set(0);
                            el2.LookupParameter("Dv_Пилон_Длина").Set(el1.LookupParameter("Dv_Пилон_Длина").AsDouble());
                            el2.LookupParameter("Dv_Пилон_Ширина").Set(el1.LookupParameter("Dv_Пилон_Ширина").AsDouble());
                            RotateColumn(el1 as FamilyInstance, el2 as FamilyInstance);
                            cutCounter++;

                        }
                        colToB = new ColumnToBreak(el1, cutCounter);

                        view.columnsToBreak.Add(colToB);
                        doc.Delete(el1.Id);
                    }
                }
                doc.Regenerate();
                view.Show();
                trans.Commit();
                return Result.Succeeded;
            }
            else { TaskDialog.Show("Ошибка!", "Не было выбрано ни одного пилона!"); return Result.Failed; }
            
        }
    }
}
