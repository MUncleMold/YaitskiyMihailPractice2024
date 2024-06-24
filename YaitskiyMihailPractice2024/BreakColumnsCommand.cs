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
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB.Structure;
//24.06.2024 15:00 Реализована простейшая версия класса BreakColumnsCommand. Механизм работы примерно следующий - 
//пользователь выбирает балку, после чего программа создает на каждом этаже (буквально на каждом) балки идиентичной ширины и длины, упирающиеся в следующий уровень.
//на данный момент балки не поварачиваются по материнской балке, балки не учитывают пол (проходят его насквозь) и не обрабатывают последний и нулевой (я бы даже сказал минус первый этажи).
//Об обработке исключений пока не задумывался.
namespace Yaitskiy_Mihail_Practice
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]

    internal class BreakColumnsCommand : IExternalCommand
    {
        //Список всех уровней
        public IList<Element> GetLevels(Autodesk.Revit.DB.Document doc)
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
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
           
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application.Create;
            Autodesk.Revit.Creation.Document docCreate = uiapp.ActiveUIDocument.Document.Create;
            Autodesk.Revit.DB.Document doc = uiapp.ActiveUIDocument.Document;
            //Список этажей
            IList<Element> levels = GetLevels(doc);
            //Выбор балки 
            Reference pickedref1 = null;
            Selection sel = uiapp.ActiveUIDocument.Selection;
            pickedref1 = sel.PickObject(ObjectType.Element, "First thing IDk XXDXDXDXD");
            Element el1 = doc.GetElement(pickedref1);
            //Данная переменная необходима для функции создания
            FamilyInstance familyInstance = el1 as FamilyInstance;
            //Получение точки выбранной балки
            XYZ xYZ = GetPoint(el1);

            Transaction trans = new Transaction(doc);
            trans.Start("col");
            for(int i = 0; i < levels.Count - 1; i++)
            {
                //Расчет расстояния между этажом, на который необходимо поставить балку, и следующим.
                Level l1 = levels[i] as Level, l2 = levels[i+1] as Level;
                double topOffset = l2.Elevation - l1.Elevation;
                Element el2 = docCreate.NewFamilyInstance(xYZ, familyInstance.Symbol, l1, StructuralType.Column);
                //Модификация созданной балки
                el2.LookupParameter("Top Offset").Set(topOffset);
                el2.LookupParameter("Base Offset").Set(0);
                el2.LookupParameter("Dv_Пилон_Длина").Set(el1.LookupParameter("Dv_Пилон_Длина").AsDouble());
                el2.LookupParameter("Dv_Пилон_Ширина").Set(el1.LookupParameter("Dv_Пилон_Ширина").AsDouble());
            }
            doc.Regenerate();
            trans.Commit();
            return Result.Succeeded;
        }
    }
}
