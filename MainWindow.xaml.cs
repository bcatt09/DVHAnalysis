using System;
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
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using OxyPlot.Wpf;

namespace VMS.TPS
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : UserControl
	{
		public int prevSelectedRow = -1;

		public MainWindow()
		{
			InitializeComponent();
			DVHCanvas.Children.Add(CreatePlotView());
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
	}
}
