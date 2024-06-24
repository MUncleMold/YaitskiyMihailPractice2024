using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Yaitskiy_Mihail_Practice
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            RibbonPanel ribbonPanel = application.CreateRibbonPanel("PillowCreater");
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            PushButtonData buttonData = new PushButtonData("Start", "Start", thisAssemblyPath, "Yaitskiy_Mihail_Practice.BreakColumnsCommand");
            PushButton pushbutton = ribbonPanel.AddItem(buttonData) as PushButton;
            
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {

        return Result.Succeeded; 
        }

       
    }
    
    
    
 }

