using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace VMS.TPS
{
	public class Script
	{
		Window myWindow;

		public Script()
		{
		}

		public void Execute(ScriptContext context, Window window)
		{
			if (context.PlanSetup == null && context.PlanSumsInScope == null)
			{
				throw new ApplicationException("Please open a plan or plan sum before running this script");
			}
			if (context.PlanSetup == null && context.PlanSumsInScope.Count() > 1)
			{
				throw new ApplicationException("Please right click and close all other plan sums before running this script");
			}
			List<PlanningItem> PItemsInScope = new List<PlanningItem>();
			foreach (var pitem in context.PlansInScope)
				PItemsInScope.Add(pitem);
			foreach (var pitem in context.PlanSumsInScope)
				PItemsInScope.Add(pitem);

			PlanningItem openedPItem = null;
			if (context.PlanSetup != null)
				openedPItem = context.PlanSetup;
			else
				openedPItem = context.PlanSumsInScope.First();


			myWindow = window;
			window.KeyDown += KeyPressed;
			window.Background = System.Windows.Media.Brushes.DarkGray;

			Start(context.Patient, PItemsInScope, openedPItem, context.Course, context.StructureSet, context.CurrentUser, window);
		}

		private void KeyPressed(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Escape)
				myWindow.Close();
		}

		/// <summary>
		/// Starts execution of script. This method can be called directly from PluginTester or indirectly from Eclipse
		/// through the Execute method.
		/// </summary>
		/// <param name="patient">Opened patient</param>
		/// <param name="PItemsInScope">Planning Items in scope</param>
		/// <param name="pItem">Opened Planning Item</param>
		/// <param name="currentUser">Current user</param>
		/// <param name="window">WPF window</param>
		public static void Start(Patient pat, List<PlanningItem> PItemsInScope, PlanningItem pItem, Course course, StructureSet ss, User currentUser, Window window)
		{
			if (pat == null || pItem == null)
			{
				MessageBox.Show("Please open a plan or plan sum before running this script", "No Active Plan", MessageBoxButton.OK, MessageBoxImage.Error);
				throw new ApplicationException("Please open a plan or a plan sum before running this script");
			}
			window.Title = "DVH Analysis - " + pat.LastName + ", " + pat.FirstName + " (" + pat.Id + ")";
			window.SizeToContent = SizeToContent.WidthAndHeight;

			DVHViewModel viewModel = new DVHViewModel(pat, pItem, course, ss, currentUser);
			MainWindow userControl = new MainWindow(viewModel);

			window.Content = userControl;
			window.DataContext = viewModel;
			userControl.PlanInfoListBox.DataContext = viewModel.Plans;
			userControl.DVHDataGrid.DataContext = viewModel.DVHTable;
		}
	}
}

