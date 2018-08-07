using System;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

namespace SimplePdfReport.Reporting.MigraDoc.Internal
{
    internal class PlanInfo
    {
        public static readonly Color Shading = new Color(243, 243, 243);

        public void Add(Section section, ReportData data)
        {
            var table = AddPlanInfoTable(section);

            AddLeftInfo(table.Rows[0].Cells[0], data);
            AddRightInfo(table.Rows[0].Cells[1], data);
        }

        private Table AddPlanInfoTable(Section section)
        {
            var table = section.AddTable();
            table.Shading.Color = Shading;

            table.Rows.LeftIndent = 0;

            table.LeftPadding = Size.TableCellPadding;
            table.TopPadding = Size.TableCellPadding;
            table.RightPadding = Size.TableCellPadding;
            table.BottomPadding = Size.TableCellPadding;

            // Use two columns of equal width
            var columnWidth = Size.GetWidth(section) / 2.0;
            table.AddColumn(columnWidth);
            table.AddColumn(columnWidth);

            // Only one row is needed
            table.AddRow();

            return table;
        }

        private void AddLeftInfo(Cell cell, ReportData data)
        {
            // Add patient name and sex symbol
            var p1 = cell.AddParagraph();
            p1.Style = CustomStyles.PatientName;
            p1.AddText(data.Patient.Name);
            p1.AddSpace(2);
			//AddHospitalLogo(p1);

            // Add patient ID
            var p2 = cell.AddParagraph();
            p2.AddText("ID: ");
            p2.AddFormattedText(data.Patient.Id, TextFormat.Bold);
        }

        private void AddHospitalLogo(Paragraph p)
        {
            p.AddImage(HospitalLogo.GetMigraDocFileName());
        }

        private void AddRightInfo(Cell cell, ReportData data)
        {
            var p = cell.AddParagraph();

			// Add course
			p.AddText("Course: ");
			p.AddFormattedText(data.Plan.Course, TextFormat.Bold);

			p.AddLineBreak();

			// Add plan
			p.AddText("Plan: ");
			p.AddFormattedText(data.Plan.Id, TextFormat.Bold);
		}
    }
}