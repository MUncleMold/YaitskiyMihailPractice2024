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
//25.06 новорожденные балки теперь учитывают вышестоящий уровень и не упираются в него, теперь появляются все балки, от подвала до крыши.
//24.06.2024 15:00 Реализована простейшая версия класса BreakColumnsCommand. Механизм работы примерно следующий - 
//пользователь выбирает балку, после чего программа создает на каждом этаже (буквально на каждом) балки идиентичной ширины и длины, упирающиеся в следующий уровень.
//на данный момент балки не поварачиваются по материнской балке, балки не учитывают пол (проходят его насквозь) и не обрабатывают последний и нулевой (я бы даже сказал минус первый этажи).
//Об обработке исключений пока не задумывался.
namespace Yaitskiy_Mihail_Practice
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]

    internal class  BreakColumnsCommand : IExternalCommand
    {
        //Список всех уровней
        public IList<Element> GetLevels(Document doc)
        {
            var levelCollecor = new FilteredElementCollector(doc);
            levelCollecor.WherePasses(new ElementClassFilter(typeof(Level)));
            IList<Element> levels = levelCollecor.ToElements();
            return levels;
        }
        //Получить точку элемента
        public XYZ GetPoint(Element el)
        {
            Location loc = el.Location;
            LocationPoint lp = loc as LocationPoint;
            return lp.Point;
        }
        //Найти уровень в списке
        public Element FindLevel(Parameter param, IList<Element> ll, out int n)
        {
            for(n = 0; n < ll.Count; n++)
            {
                if (ll[n].Id == param.AsElementId())
                {   
                    return ll[n];
                }
                if(n > ll.Count) { break; }
            }
            n = 0;
            return null;
            //foreach(Element element in ll)
            //{
            //    if(element.Id == param.AsElementId())
            //    {
            //        return element;
            //    }
            //}
            //return null;
        }
        //Найти высочайший уровень элемента
        public Element GetTopLevel(Element el, IList<Element> ll, out int n)
        {
            Parameter par1 = el.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
          

            return FindLevel(par1, ll, out n);
        }
        //Найти базовый уровень элемента
        public Element GetBaseLevel(Element el, IList<Element> ll, out int n)
        {
            Parameter par1 = el.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
            return FindLevel(par1, ll, out n);
        }
        //public Element GetTheFloor(Element el, Document doc)
        //{
        //    FilteredElementCollector floorCol = new FilteredElementCollector(doc);
        //    floorCol = floorCol.WherePasses(new ElementIntersectsElementFilter(el));
        //    return floorCol.FirstElement();
        //}
        public IList<Element> GetFloors(Document doc)
        {
            var floorCollecor = new FilteredElementCollector(doc);
            floorCollecor.WherePasses(new ElementClassFilter(typeof(Floor)));
            IList<Element> floors = floorCollecor.ToElements();
            return floors;
        }

        public void RotateColumn(Element el1, Element el2)
        {
            FamilyInstance fI1 = el1 as FamilyInstance, fI2 = el2 as FamilyInstance;
            XYZ point1 = fI1.FacingOrientation, point2 = fI2.FacingOrientation;
            XYZ xYZ0 = new XYZ(0, 0, 100);
            XYZ xYZ = GetPoint(el1);
            XYZ center = xYZ.Add(xYZ0);
            el2.Location.Rotate(Line.CreateBound(xYZ, center), point1.AngleTo(point2));

        }
        public class ColumnPickFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
             {
                if(elem.Category.Name == "Structural Columns")
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }

       
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uiapp.ActiveUIDocument.Document;
            //Список этажей
            IList<Element> levels = GetLevels(doc);
            //Список полов
            IList<Element> floors = GetFloors(doc);

            ColumnPickFilter colFilter = new ColumnPickFilter();
            Selection sel = uiapp.ActiveUIDocument.Selection;

            IList<Reference> columns = sel.PickObjects(ObjectType.Element, "Выберите колонны для разрезания");
            IList<Element> columns2 = new List<Element>();

            foreach(Reference re in columns)
            {
                
                Element el = doc.GetElement(re);
                if (el.Category.Name == "Structural Columns")
                {
                    columns2.Add(el);
                }
                
            }


            //Element el1 = doc.GetElement(pickedref1);
            //s = el1.Category.Name;
            //TaskDialog.Show("Skibidi", s);


            //Данная переменная необходима для функции создания

            //Получение точки выбранной балки
            int baseLevelInt = 0, topLevelInt = 0;

            Element el2;
            Level l1, l2;
            Floor currentFloor;

            ColumnToBreak colToB;

            Transaction trans = new Transaction(doc);
            BreakColumnsView view = new BreakColumnsView();
            trans.Start("col");
            foreach (Element el1 in columns2)
            {
                int cutCounter = 0;
                Level topLevel = GetTopLevel(el1, levels, out topLevelInt) as Level;
                Level baseLevel = GetBaseLevel(el1, levels, out baseLevelInt) as Level;
                XYZ xYZ = GetPoint(el1);
                FamilyInstance familyInstance = el1 as FamilyInstance;

                l1 = levels[baseLevelInt] as Level;

                //Создание пилона под первым уровнем
                currentFloor = floors[baseLevelInt] as Floor;
                el2 = doc.Create.NewFamilyInstance(xYZ, familyInstance.Symbol, l1, StructuralType.Column);
                el2.LookupParameter("Dv_Пилон_Длина").Set(el1.LookupParameter("Dv_Пилон_Длина").AsDouble());
                el2.LookupParameter("Dv_Пилон_Ширина").Set(el1.LookupParameter("Dv_Пилон_Ширина").AsDouble());
                el2.LookupParameter("Top Offset").Set(0 - currentFloor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble());
                el2.LookupParameter("Base Offset").Set(el1.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).AsDouble());
                RotateColumn(el1, el2);
                cutCounter++;

                //Создание пилона на последнем уровне
                l1 = levels[topLevelInt] as Level;
                el2 = doc.Create.NewFamilyInstance(xYZ, familyInstance.Symbol, l1, StructuralType.Column);
                el2.LookupParameter("Dv_Пилон_Длина").Set(el1.LookupParameter("Dv_Пилон_Длина").AsDouble());
                el2.LookupParameter("Dv_Пилон_Ширина").Set(el1.LookupParameter("Dv_Пилон_Ширина").AsDouble());
                el2.LookupParameter("Base Offset").Set(0);
                el2.LookupParameter("Top Offset").Set(el1.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).AsDouble());
                RotateColumn(el1, el2);
                cutCounter++;

                for (int i = baseLevelInt; i < topLevelInt; i++)
                {
                    //Расчет расстояния между этажом, на который необходимо поставить балку, и следующим.
                    l1 = levels[i] as Level; l2 = levels[i + 1] as Level;
                    currentFloor = floors[i + 1] as Floor;
                    double fThic = currentFloor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble();
                    double topOffset = l2.Elevation - l1.Elevation - fThic;
                    el2 = doc.Create.NewFamilyInstance(xYZ, familyInstance.Symbol, l1, StructuralType.Column);
                    //Модификация созданной балки
                    el2.LookupParameter("Top Offset").Set(topOffset);
                    el2.LookupParameter("Base Offset").Set(0);
                    el2.LookupParameter("Dv_Пилон_Длина").Set(el1.LookupParameter("Dv_Пилон_Длина").AsDouble());
                    el2.LookupParameter("Dv_Пилон_Ширина").Set(el1.LookupParameter("Dv_Пилон_Ширина").AsDouble());
                    RotateColumn(el1, el2);
                    cutCounter++;

                }
                colToB = new ColumnToBreak(el1, cutCounter);

                view.columnsToBreak.Add(colToB);
                doc.Delete(el1.Id);
            }
            doc.Regenerate();
            view.Show();

            trans.Commit();
            return Result.Succeeded;
        }
    }
}
