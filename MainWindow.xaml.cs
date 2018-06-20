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
using System.Drawing;
using System.Drawing.Printing;

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
			//DVHCanvas.Children.Add(CreatePlotView());
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













		//just for the print screen button but can be removed later for print to PDF

		Bitmap memoryImage;
		Window myWindow;

		void printButton_Click(object sender, RoutedEventArgs e)
		{
			//getst the window to take a screenshot of
			myWindow = Window.GetWindow(this);

			//grab screen image and print it
			memoryImage = new Bitmap((int)myWindow.Width, (int)myWindow.Height);
			Graphics gfxScreenshot = Graphics.FromImage(memoryImage);
			gfxScreenshot.CopyFromScreen((int)myWindow.Left, (int)myWindow.Top, 0, 0, new System.Drawing.Size((int)myWindow.Width, (int)myWindow.Height), CopyPixelOperation.SourceCopy);

			System.Windows.Forms.PrintDialog printDlg = new System.Windows.Forms.PrintDialog();
			PrintDocument printDoc = new PrintDocument();
			printDoc.DocumentName = myWindow.Title;
			printDoc.PrintPage += new PrintPageEventHandler(printDoc_PrintPage);
			printDlg.Document = printDoc;
			printDlg.AllowSelection = true;
			printDlg.AllowSomePages = true;
			//Call ShowDialog
			if (printDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				printDoc.Print();
		}

		private void printDoc_PrintPage(Object sender, PrintPageEventArgs e)
		{
			System.Drawing.Rectangle printArea;

			//scale image
			if (memoryImage.Width / e.MarginBounds.Width > memoryImage.Height / e.MarginBounds.Height)
				printArea = new System.Drawing.Rectangle(0, 0, (int)((double)memoryImage.Width / (double)memoryImage.Width * (double)e.MarginBounds.Width), (int)((double)memoryImage.Height / (double)memoryImage.Width * (double)e.MarginBounds.Width));
			else
				printArea = new System.Drawing.Rectangle(0, 0, (int)((double)memoryImage.Width / (double)memoryImage.Height * (double)e.MarginBounds.Height), (int)((double)memoryImage.Height / (double)memoryImage.Height * (double)e.MarginBounds.Height));

			int marginCenterX = e.MarginBounds.Left + e.MarginBounds.Width / 2;
			int marginCenterY = e.MarginBounds.Top + e.MarginBounds.Height / 2;
			int imageCenterX = printArea.Width / 2;
			int imageCenterY = printArea.Height / 2;

			//shift it to the center of the page
			printArea.Offset(marginCenterX - imageCenterX, marginCenterY - imageCenterY);

			e.Graphics.DrawImage(memoryImage, printArea);
		}
	}
}
