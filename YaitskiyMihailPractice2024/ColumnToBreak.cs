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
namespace YaitskiyMihailPractice2024
{
    public class ColumnToBreak
    {

        public ColumnToBreak(Element baseColumn, int counter)
        {
            this.baseColumn = baseColumn;
            this.counter = counter;
            columnName = baseColumn.Name;
        }
        public Element baseColumn { get; set; }
        public int counter { get; set; }
        public string columnName { get; set; }

    }
}
