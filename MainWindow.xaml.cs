using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using OxyPlot.Wpf;
//using System.Drawing;
using System.Drawing.Printing;
using SimplePdfReport.Reporting;
using SimplePdfReport.Reporting.MigraDoc;
using MigraDoc.Rendering.Printing;

namespace VMS.TPS
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : UserControl
	{
		// Create dummy PlotView to force OxyPlot.Wpf to be loaded
		private static readonly PlotView PlotView = new PlotView();

		private readonly DVHViewModel _vm;

		public int prevSelectedRow = -1;

		public MainWindow(DVHViewModel viewModel)
		{
			_vm = viewModel;
			InitializeComponent();
		}

		// draw the DVH
		private PlotView CreatePlotView()
		{
			return new PlotView();
		}

		// toggle the row details when clicking on the same row
		private void dataGridMouseLeftButton(object sender, MouseButtonEventArgs e)
		{
			DataGrid dg = sender as DataGrid;
			if (dg == null)
				return;

			if (dg.SelectedIndex == prevSelectedRow)
			{
				DVHDataGrid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;
				dg.SelectedIndex = -1;
				prevSelectedRow = -1;
			}
			else
			{
				DVHDataGrid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.VisibleWhenSelected;
				prevSelectedRow = dg.SelectedIndex;
			}
		}
		
		// make the row details not show for a combobox click
		private void ComboBox_DropDownOpened(object sender, EventArgs e)
		{
			DVHDataGrid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;
			DVHDataGrid.SelectedIndex = -1;
		}

		private void ComboBox_DropDownClosed(object sender, EventArgs e)
		{
			DVHDataGrid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;
			DVHDataGrid.SelectedIndex = -1;
		}

		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{

			if (e.Key == Key.Enter)
			{
				((TextBox)sender).MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
				Keyboard.ClearFocus();
			}
		}

		private void Structure_OnChecked(object checkBoxObject, RoutedEventArgs e)
		{
			_vm.AddDvhCurve(GetStructure(checkBoxObject));
		}

		private void Structure_OnUnchecked(object checkBoxObject, RoutedEventArgs e)
		{
			_vm.RemoveDvhCurve(GetStructure(checkBoxObject));
		}

		private Common.Model.API.Structure GetStructure(object checkBoxObject)
		{
			var checkbox = (CheckBox)checkBoxObject;
			var structure = (checkbox.DataContext as DVHStructure).structure;
			return structure;
		}



		void printButton_Click(object sender, RoutedEventArgs e)
		{
			if(_vm.SelectedProtocol == null)
			{
				MessageBox.Show("Please select an analysis protocol first", "No Protocol Selected", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			var reportService = new ReportPdf();
			var reportData = CreateReportData();

			System.Windows.Forms.PrintDialog printDlg = new System.Windows.Forms.PrintDialog();
			MigraDocPrintDocument printDoc = new MigraDocPrintDocument();
			printDoc.Renderer = new MigraDoc.Rendering.DocumentRenderer(reportService.CreateReport(reportData));
			printDoc.Renderer.PrepareDocument();
			
			printDoc.DocumentName = Window.GetWindow(this).Title;
			//printDoc.PrintPage += new PrintPageEventHandler(printDoc_PrintPage);
			printDlg.Document = printDoc;
			printDlg.AllowSelection = true;
			printDlg.AllowSomePages = true;
			//Call ShowDialog
			if (printDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				printDoc.Print();
		}
		
		private ReportData CreateReportData()
		{
			ReportData reportData = new ReportData();

			reportData.Patient = new SimplePdfReport.Reporting.Patient
			{
				Id = _vm.PatientID,
				Name = _vm.PatientName
			};

			reportData.User = new SimplePdfReport.Reporting.User
			{
				Username = _vm.CurrentUser
			};

			reportData.Plans = new SimplePdfReport.Reporting.Plans
			{

				Id = _vm.PlanID,
				Course = _vm.CourseID ?? "",
				Protocol = ConstraintList.GetProtocolName(_vm.SelectedProtocol),
				PlanList = new List<Plan>()
			};

			foreach(PlanInformation plan in _vm.Plans)
			{
				SimplePdfReport.Reporting.Plan newPlan = new SimplePdfReport.Reporting.Plan
				{
					Id = plan.PlanID,
					TotalDose = plan.TotalPlannedDose,
					DosePerFx = plan.DosePerFraction,
					Fractions = plan.NumberOfFractions
				};

				reportData.Plans.PlanList.Add(newPlan);
			}

			reportData.DvhTable = new DVHTable
			{
				Title = "DVH Analysis Report"
			};

			foreach (DVHTableRow row in _vm.DVHTable)
			{
				SimplePdfReport.Reporting.DVHTableRow newRow = new SimplePdfReport.Reporting.DVHTableRow();

				newRow.StructureId = row.Structure ?? "";
				newRow.PlanStructureId = row.SelectedStructure != null ? row.SelectedStructure.Id : "";
				newRow.Constraint = row.ConstraintText ?? "";
				newRow.VariationConstraint = row.VariationConstraintText ?? "";
				newRow.Limit = row.LimitText ?? "";
				newRow.VariationLimit = row.VariationLimitText ?? "";
				newRow.PlanValue = row.PlanValueText ?? "";
				newRow.PlanResult = row.PlanResult ?? "";
				newRow.PlanResultColor = row.PlanResultColor;

				reportData.DvhTable.Rows.Add(newRow);
			}

			return reportData;
		}
	}
}
