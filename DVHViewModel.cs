using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using VMS.TPS.Common.Model.API;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using VMS.TPS.Common.Model.Types;
using OxyPlot;
using OxyPlot.Axes;

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
		public string CourseID { get { return _contextInfo.SelectedCourse == null ? "" :_contextInfo.SelectedCourse.Id; } }
		public string PlanID { get { return _contextInfo.SelectedPlanningItem.Id; } }
		public string CurrentUser { get { return _contextInfo.CurrentUser.Id; } }
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
		//constraint table
		public ObservableCollection<DVHTableRow> DVHTable;
		public bool InitialTableLoad { get { return _initialTableLoad; } }
		//DVH
		public IEnumerable<DVHStructure> DVHStructures { get; set; }
		public PlotModel PlotModel { get; private set; }
		//misc
		public string Username { get; set; }

		public DVHViewModel(Patient pat, PlanningItem pItem, Course course, StructureSet ss, User user)
		{
			//patient information
			_contextInfo = new ContextInformation
			{
				SelectedPatient = pat,
				SelectedPlanningItem = pItem,
				SelectedCourse = course,
				SelectedStructureSet = pItem is PlanSetup ? ss : (pItem as PlanSum).StructureSet,
				CurrentUser = user
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

			//DVH
			PlotModel = CreatePlotModel();
			DVHStructures = GetContouredStructures();

			//constraints table
			DVHTable = new ObservableCollection<DVHTableRow>();

			//misc
			Username = _contextInfo.CurrentUser.Id;
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

		//DVH Functions

		private PlotModel CreatePlotModel()
		{
			var plotModel = new PlotModel();
			AddAxes(plotModel, _contextInfo);
			plotModel.IsLegendVisible = true;
			plotModel.LegendPlacement = LegendPlacement.Outside;
			plotModel.LegendOrientation = LegendOrientation.Vertical;
			plotModel.LegendPosition = LegendPosition.BottomCenter;
			plotModel.LegendMaxHeight = 75;
			return plotModel;
		}

		private static void AddAxes(PlotModel plotModel, ContextInformation context)
		{
			plotModel.Axes.Add(new LinearAxis
			{
				Title = "Dose [Gy]",
				Position = AxisPosition.Bottom,
				MajorGridlineStyle = LineStyle.Solid,
				MinorGridlineStyle = LineStyle.Dot,
				AbsoluteMinimum = 0,
				AbsoluteMaximum = context.SelectedPlanningItem.Dose.DoseMax3D.Unit == DoseValue.DoseUnit.Percent ? context.SelectedPlanningItem.Dose.DoseMax3D.Dose * (context.SelectedPlanningItem as PlanSetup).TotalPrescribedDose.Dose / 100 : context.SelectedPlanningItem.Dose.DoseMax3D.Dose
			});

			plotModel.Axes.Add(new LinearAxis
			{
				Title = "Volume [%]",
				Position = AxisPosition.Left,
				MajorGridlineStyle = LineStyle.Solid,
				MinorGridlineStyle = LineStyle.Dot,
				AbsoluteMinimum = 0,
				AbsoluteMaximum = 100
			});
		}

		private IEnumerable<DVHStructure> GetContouredStructures()
		{
			List<DVHStructure> list = new List<DVHStructure>();

			foreach (Structure struc in SelectedStructureSet.Structures)
			{
				if (!struc.IsEmpty && struc.DicomType != "SUPPORT" && struc.DicomType != "MARKER")
					list.Add(new DVHStructure(struc, false));
			}

			return list as IEnumerable<DVHStructure>;
		}

		public void AddDvhCurve(Structure structure)
		{
			DVHData dvh = CalculateDvh(structure);
			PlotModel.Series.Insert(0, CreateDvhSeries(structure, dvh));
			UpdatePlot();
		}

		public void RemoveDvhCurve(Structure structure)
		{
			var series = FindSeries(structure.Id);
			PlotModel.Series.Remove(series);
			UpdatePlot();
		}

		private DVHData CalculateDvh(Structure structure)
		{
			return _contextInfo.SelectedPlanningItem.GetDVHCumulativeData(structure,
				DoseValuePresentation.Absolute,
				VolumePresentation.Relative, 0.01);
		}

		private OxyPlot.Series.Series CreateDvhSeries(Structure structure, DVHData dvh)
		{
			var series = new OxyPlot.Series.LineSeries { Tag = structure.Id, Title = structure.Id, Color = OxyColor.FromRgb(structure.Color.R,structure.Color.G,structure.Color.B) };
			var points = dvh.CurveData.Select(CreateDataPoint);
			series.Points.AddRange(points);
			series.TrackerFormatString = "{0} " + Environment.NewLine + "{1}: {2:0.000} " + Environment.NewLine + "{3}: {4:0.0} ";
			return series;
		}

		private DataPoint CreateDataPoint(DVHPoint p)
		{
			return new DataPoint(p.DoseValue.Dose, p.Volume);
		}

		private OxyPlot.Series.Series FindSeries(string structureId)
		{
			return PlotModel.Series.FirstOrDefault(x => (string)x.Tag == structureId);
		}

		private void UpdatePlot()
		{
			PlotModel.InvalidatePlot(true);
		}
	}

	public class DVHStructure : INotifyPropertyChanged
	{
		private Boolean _onDVH;

		public Structure structure { get; set; }
		public Boolean OnDVH { get { return _onDVH; } set { _onDVH = value; OnPropertyChanged("OnDVH"); } }

		public DVHStructure(Structure struc, Boolean DVH)
		{
			structure = struc;
			_onDVH = DVH;
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
	}
}
