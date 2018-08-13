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
			//add odd numbered plans
			int i = 0;

			while(i<data.Plans.PlanList.Count)
			{
				var plan = data.Plans.PlanList[i];

				var p1 = cell.AddParagraph();
				p1.Style = CustomStyles.PatientName;
				p1.AddFormattedText(plan.Id, TextFormat.Bold);

				var p2 = cell.AddParagraph();
				p2.AddText($"Total Planned Dose: {plan.TotalDose}");

				var p3 = cell.AddParagraph();
				p3.AddText($"Dose per Fraction: {plan.DosePerFx}");

				var p4 = cell.AddParagraph();
				p4.AddText($"Fractions: {plan.Fractions}");

				if(i+2 < data.Plans.PlanList.Count)
					cell.AddParagraph();

				i += 2;
			}
        }

        private void AddHospitalLogo(Paragraph p)
        {
            p.AddImage(HospitalLogo.GetMigraDocFileName());
        }

        private void AddRightInfo(Cell cell, ReportData data)
		{
			//add even numbered plans
			int i = 1;

			while (i < data.Plans.PlanList.Count)
			{
				var plan = data.Plans.PlanList[i];

				var p1 = cell.AddParagraph();
				p1.Style = CustomStyles.PatientName;
				p1.AddFormattedText(plan.Id, TextFormat.Bold);

				var p2 = cell.AddParagraph();
				p2.AddText($"Total Planned Dose: {plan.TotalDose}");

				var p3 = cell.AddParagraph();
				p3.AddText($"Dose per Fraction: {plan.DosePerFx}");

				var p4 = cell.AddParagraph();
				p4.AddText($"Fractions: {plan.Fractions}");

				if (i + 2 < data.Plans.PlanList.Count)
					cell.AddParagraph();

				i += 2;
			}
		}
    }
}