using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using VMS.TPS.Common.Model.API;
using PdfSharp.Pdf;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using VMS.TPS.Common.Model.Types;

namespace VMS.TPS
{
	public class DVHViewModel : INotifyPropertyChanged
	{
		private ContextInformation _contextInfo;
		private ObservableCollection<PlanInformation> _plans;
		private IEnumerable<string> _categories;
		private IEnumerable<string> _protocols;
		private string _selectedCategory;	//selected protocol category from the dropdowns
		private string _selectedProtocol;   //selected protocol from the dropdowns
		private DoseValue _planSumTotalDose;   //plan sum prescription to be used for converting to percentages
		private string _planSumTotalDoseText;   //plan sum prescription to be used for converting to percentages
		private bool _initialTableLoad;     //inital loading of the table?  for use with structure dropdowns
		private Visibility _variationConstraintColumnVisibility;	//visibility of the varaiation constraint column based on whether it exists in the selected protocol
		private Visibility _variationLimitColumnVisibility;         //visibility of the varaiation limit column based on whether it exists in the selected protocol
		private Visibility _planSumDoseVisibility;					//visibility of total dose for plan sums

		//patient and plan information to be displayed
		public string PatientID { get { return _contextInfo.SelectedPatient.Id; } }
		public string PatientName { get { return _contextInfo.SelectedPatient.LastName + ", " + _contextInfo.SelectedPatient.FirstName; } }
		public string CourseID { get { return _contextInfo.SelectedCourse.Id; } }
		public string PlanID { get { return _contextInfo.SelectedPlanningItem.Id; } }
		public StructureSet SelectedStructureSet { get { return _contextInfo.SelectedStructureSet; } }
		public PlanningItem SelectedPlanningItem { get { return _contextInfo.SelectedPlanningItem; } }
		public IEnumerable<Structure> Structures { get { return SelectedStructureSet.Structures; } }
		public ObservableCollection<PlanInformation> Plans { get { return _plans; } }
		//user options to be displayed
		public IEnumerable<string> Categories { get { return _categories; } }
		public IEnumerable<string> Protocols { get { return _protocols; } set { _protocols = value; OnPropertyChanged("Protocols"); } }
		public string SelectedCategory { get { return _selectedCategory; } set { _selectedCategory = value; Protocols = ConstraintList.GetProtocols(_selectedCategory); OnPropertyChanged("SelectedCategory"); } }
		public string SelectedProtocol { get { return _selectedProtocol; } set { _selectedProtocol = value; CreateNewDVHTable(); OnPropertyChanged("SelectedProtocol"); } }
		public DoseValue PlanSumTotalDose { get { return _planSumTotalDose; } set { _planSumTotalDose = value; _planSumTotalDoseText = _planSumTotalDose.ToString(); UpdateDVHTable(); OnPropertyChanged("PlanSumTotalDose"); } }
		public string PlanSumTotalDoseText { get { return _planSumTotalDoseText; } set { _planSumTotalDoseText = value; _planSumTotalDose = new DoseValue(Double.Parse(_planSumTotalDoseText), _planSumTotalDose.Unit); UpdateDVHTable(); OnPropertyChanged("PlanSumTotalDoseText"); } }
		public Visibility VariationConstraintColumnVisibility { get { return _variationConstraintColumnVisibility; } set { _variationConstraintColumnVisibility = value; OnPropertyChanged("VariationConstraintColumnVisibility"); } }
		public Visibility VariationLimitColumnVisibility { get { return _variationLimitColumnVisibility; } set { _variationLimitColumnVisibility = value; OnPropertyChanged("VariationLimitColumnVisibility"); } }
		public Visibility PlanSumDoseVisibility { get { return _planSumDoseVisibility; } set { _planSumDoseVisibility = value; OnPropertyChanged("PlanSumDoseVisibility"); } }
		//dvh
		public ObservableCollection<DVHTableRow> DVHTable;
		public bool InitialTableLoad { get { return _initialTableLoad; } }

		public DVHViewModel(Patient pat, PlanningItem pItem, Course course, StructureSet ss)
		{
			//patient information
			_contextInfo = new ContextInformation
			{
				SelectedPatient = pat,
				SelectedPlanningItem = pItem,
				SelectedCourse = course,
				SelectedStructureSet = pItem is PlanSetup ? ss : (pItem as PlanSum).PlanSetups.First().StructureSet
			};

			_plans = new ObservableCollection<PlanInformation>();

			//add plans to be displayed
			if (pItem is PlanSetup)
			{
				PlanSumDoseVisibility = Visibility.Hidden;
				_plans.Add(new PlanInformation(pItem as PlanSetup));
			}
			else //plan sum, so add all plans in it
			{
				PlanSumDoseVisibility = Visibility.Visible;
				DoseValue tempPlanSumTotalDose = new DoseValue(0, SelectedPlanningItem.Dose.DoseMax3D.Unit);

				foreach (PlanSetup plan in (pItem as PlanSum).PlanSetups)
				{
					_plans.Add(new PlanInformation(plan));
					tempPlanSumTotalDose = DvhExtensions.AddDoseValues(tempPlanSumTotalDose, plan.TotalPrescribedDose);
				}

				_planSumTotalDose = tempPlanSumTotalDose;
				_planSumTotalDoseText = tempPlanSumTotalDose.Dose.ToString();
			}

			//protocol selection dropdowns
			_categories = ConstraintList.GetCategories();
			_protocols = ConstraintList.GetProtocols();
			_selectedCategory = _categories.First();

			//dvh
			DVHTable = new ObservableCollection<DVHTableRow>();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(name));
			}
		}

		public void CreateNewDVHTable()
		{
			_initialTableLoad = true;
			DVHTable.Clear();

			//add new row for each constraint found in the new protocol
			foreach (XElement constr in ConstraintList.GetProtocol(SelectedProtocol).Descendants("constraint"))
			{
				DVHTableRow newRow = new DVHTableRow(constr, this);

				DVHTable.Add(newRow);
			}

			DetermineColumnVisibilities();

			_initialTableLoad = false;
		}

		public void UpdateDVHTable()
		{
			foreach (DVHTableRow row in DVHTable)
				row.RecalcRow();
		}

		private void DetermineColumnVisibilities()
		{
			//see if there are any nodes in the protocol that have a "variation-constraint" or "variation-limit" attribute
			if ((from el in ConstraintList.GetProtocol(SelectedProtocol).Descendants("constraint")
				 where el.Attribute("variation-constraint") != null
				 select el).Count() > 0)
			{
				VariationConstraintColumnVisibility = Visibility.Visible;
			}
			else
			{
				VariationConstraintColumnVisibility = Visibility.Collapsed;
			}


			if ((from el in ConstraintList.GetProtocol(SelectedProtocol).Descendants("constraint")
				 where el.Attribute("variation-limit") != null
				 select el).Count() > 0)
			{
				VariationLimitColumnVisibility = Visibility.Visible;
			}
			else
			{
				VariationLimitColumnVisibility = Visibility.Collapsed;
			}
		}
	}
}
