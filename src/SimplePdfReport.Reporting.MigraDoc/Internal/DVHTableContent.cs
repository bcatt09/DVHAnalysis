using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using System.Collections.Generic;

namespace SimplePdfReport.Reporting.MigraDoc.Internal
{
    internal class DVHTableContent
    {
        public void Add(Section section, ReportData data)
        {
            AddHeading(section, data.DvhTable);
            AddStructures(section, data.Plan, data.DvhTable.Rows);
        }

        private void AddHeading(Section section, DVHTable dvhTable)
        {
            section.AddParagraph(dvhTable.Title, StyleNames.Heading1);
        }

        private void AddStructures(Section section, Plan plan, List<DVHTableRow> structures)
        {
            AddTableTitle(section, plan.Protocol);
            AddStructureTable(section, structures);
        }

        private void AddTableTitle(Section section, string title)
        {
            var p = section.AddParagraph(title, StyleNames.Heading2);
            p.Format.KeepWithNext = true;
        }

        private void AddStructureTable(Section section, List<DVHTableRow> structures)
        {
            var table = section.AddTable();

            AddColumnsAndHeaders(table);
            AddStructureRows(table, structures);
			FormatTable(table, section);

			AddLastRowBorder(table);
            AlternateRowShading(table);
        }

        private static void FormatTable(Table table, Section section)
        {
            table.LeftPadding = 5;
            table.TopPadding = 5;
            table.RightPadding = 5;
            table.BottomPadding = 5;
			table.Format.LeftIndent = Size.TableCellPadding;
			table.Format.RightIndent = Size.TableCellPadding;
			//get size info
			PageSetup.GetPageSize(PageFormat.Letter, out Unit width, out Unit height);
			Unit tableWidth = 0;
			foreach (Column col in table.Columns)
				tableWidth += col.Width;
			//indent to center table
			table.Rows.LeftIndent = Unit.FromCentimeter(width.Centimeter - section.PageSetup.LeftMargin.Centimeter - section.PageSetup.RightMargin.Centimeter - tableWidth.Centimeter) / 2;
		}

        private void AddColumnsAndHeaders(Table table)
        {
        }

        private void AddHeader(Cell cell, string header)
        {
            var p = cell.AddParagraph(header);
            p.Style = CustomStyles.ColumnHeader;
        }

        private void AddStructureRows(Table table, List<DVHTableRow> DVHRows)
        {
			bool constraintFlag = false;
			bool limitFlag = false;
			List<string> structuresInTable = new List<string>();

			//create cols and header row first
			var width = Size.GetWidth(table.Section) - 50;
			table.AddColumn(width * 0.125);
			table.AddColumn(width * 0.125);
			table.AddColumn(width * 0.125);
			table.AddColumn(width * 0.125);
			table.AddColumn(width * 0.125);
			table.AddColumn(width * 0.125);
			table.AddColumn(width * 0.125);
			table.AddColumn(width * 0.125);

			var headerRow = table.AddRow();
			headerRow.Borders.Bottom.Width = 1;

			//DVH table content
			foreach (var DVHRow in DVHRows)
            {
				if (DVHRow.PlanResult != "Structure is empty")
				{
					var row = table.AddRow();
					row.VerticalAlignment = VerticalAlignment.Center;
					row.Style = CustomStyles.Table;

					//check to see if this structure has been added to the table already
					if (structuresInTable.Contains(DVHRow.StructureId))
					{
						row.Cells[0].AddParagraph("");
						row.Cells[1].AddParagraph("");
					}
					else
					{
						row.Cells[0].AddParagraph(DVHRow.StructureId);
						row.Cells[1].AddParagraph(DVHRow.PlanStructureId);
						structuresInTable.Add(DVHRow.StructureId);
					}
					row.Cells[2].AddParagraph(DVHRow.Constraint);
					row.Cells[3].AddParagraph(DVHRow.VariationConstraint);
					row.Cells[4].AddParagraph(DVHRow.Limit);
					row.Cells[5].AddParagraph(DVHRow.VariationLimit);
					row.Cells[6].AddParagraph(DVHRow.PlanValue);
					row.Cells[7].AddParagraph(DVHRow.PlanResult);

					if (DVHRow.PlanResultColor == System.Windows.Media.Brushes.LimeGreen)
						row.Cells[7].Shading.Color = new Color(170, 234, 170);
					else if (DVHRow.PlanResultColor == System.Windows.Media.Brushes.OrangeRed)
						row.Cells[7].Shading.Color = new Color(255, 179 ,149);
					else if (DVHRow.PlanResultColor == System.Windows.Media.Brushes.Gold)
						row.Cells[7].Shading.Color = new Color(255, 239, 149);						

					if (DVHRow.VariationConstraint != "")
						constraintFlag = true;

					if (DVHRow.VariationLimit != "")
						limitFlag = true;
				}
			}


			//add headers which are necessary
			AddHeader(headerRow.Cells[0], "Structure");
			AddHeader(headerRow.Cells[1], "Plan Structure");
			AddHeader(headerRow.Cells[2], "Constraint");

			if(!constraintFlag)
			{
				table.Columns[3].Width = 0;
				AddHeader(headerRow.Cells[3], "");
			}
			else
				AddHeader(headerRow.Cells[3], "Variation\nConstraint");

			AddHeader(headerRow.Cells[4], "Limit");

			if(!limitFlag)
			{
				table.Columns[5].Width = 0;
				AddHeader(headerRow.Cells[5], "");
			}
			else
				AddHeader(headerRow.Cells[5], "Variation Limit");

			AddHeader(headerRow.Cells[6], "Plan Value");
			AddHeader(headerRow.Cells[7], "Plan Result");

			ResizeColumns(table);
        }

        private void AddLastRowBorder(Table table)
        {
            var lastRow = table.Rows[table.Rows.Count - 1];
            lastRow.Borders.Bottom.Width = 2;
        }

        private void AlternateRowShading(Table table)
        {
            // Start at i = 1 to skip column headers
            for (var i = 1; i < table.Rows.Count; i++)
            {
                if (i % 2 == 0)  // Even rows
                {
                    table.Rows[i].Shading.Color = Color.FromCmyk(10, 4, 0, 0);
                }
            }
        }

		private void ResizeColumns(Table table)
		{
			int cols = 0;

			foreach (Column col in table.Columns)
			{
				if (col.Width > 0)
					cols++;
			}
			
			foreach (Column col in table.Columns)
			{
				if (col.Width > 0)
					col.Width = col.Width * 8 / cols;
			}
		}
    }
}