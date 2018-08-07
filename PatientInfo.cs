using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace VMS.TPS
{
	/// <summary>
	/// Current context information to be displayed on screen during report
	/// </summary>
	public class ContextInformation : INotifyPropertyChanged
	{
		private Patient _selectedPatient;
		private List<PlanningItem> _planningItemsInScope;
		private PlanningItem _selectedPlanningItem;
		private Course _selectedCourse;
		private StructureSet _selectedStructureSet;
		private User _currentUser;

		public Patient SelectedPatient { get { return _selectedPatient; } set { _selectedPatient = value; OnPropertyChanged("SelectedPatient"); } }
		public List<PlanningItem> PlanningItemsInScope { get { return _planningItemsInScope; } set { _planningItemsInScope = value; OnPropertyChanged("PlanningItemsInScope"); } }
		public PlanningItem SelectedPlanningItem { get { return _selectedPlanningItem; } set { _selectedPlanningItem = value; OnPropertyChanged("SelectedPlanningItem"); } }
		public Course SelectedCourse { get { return _selectedCourse; } set { _selectedCourse = value; OnPropertyChanged("SelectedCourse"); } }
		public StructureSet SelectedStructureSet { get { return _selectedStructureSet; } set { _selectedStructureSet = value; OnPropertyChanged("SelectedStructureSet"); } }
		public User CurrentUser { get; set; }

		/// <summary>
		/// this event triggers the UI to update the table, when a row changes
		/// </summary>
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

	/// <summary>
	/// Plan information to be displayed on screen during report
	/// </summary>
	public class PlanInformation : INotifyPropertyChanged
	{
		private PlanSetup _planSetup;

		public String PlanID { get { return _planSetup.Id; } }
		public String Technique { get { return GetTechnique(_planSetup); } }
		public String TargetVolumeID { get { return _planSetup.TargetVolumeID; } }
		public String PrescribedPercentage { get { return (_planSetup.PrescribedPercentage * 100).ToString() + "%"; } }
		public String DosePerFraction { get { return _planSetup.UniqueFractionation.PrescribedDosePerFraction.ToString(); } }
		public String NumberOfFractions { get { return _planSetup.UniqueFractionation.NumberOfFractions.ToString(); } }
		public String TotalPlannedDose { get { return _planSetup.TotalPrescribedDose.ToString(); } }
		public String ApprovalStatus { get { return _planSetup.ApprovalStatus.ToString(); } }
		public String ModifiedBy { get { return _planSetup.HistoryUserName; } }
		public String ModifiedTime { get { return _planSetup.HistoryDateTime.ToString(); } }

		/// <summary>
		/// Create a new PlanInformation object using the given PlanSetup
		/// </summary>
		/// <param name="ps">PlanSetup object to retrieve information from</param>
		public PlanInformation(PlanSetup ps)
		{
			_planSetup = ps;
		}

		/// <summary>
		/// Get the technique of the PlanSetup object
		/// </summary>
		/// <param name="ps">PlanSetup object to get the technique of</param>
		/// <returns>VMAT, ARC, IMRT, or STATIC</returns>
		static string GetTechnique(PlanSetup ps)
		{
			if (ps is BrachyPlanSetup)
			{
				BrachyPlanSetup brachy = ps as BrachyPlanSetup;
				if (brachy.NumberOfPdrPulses != null)
				{
					return "PDR";
				}
				else
				{
					Catheter c = brachy.Catheters.FirstOrDefault();
					if (c != null)
					{
						return c.TreatmentUnit.DoseRateMode;
					}
				}
			}
			else
			{
				Beam beam = ps.Beams.FirstOrDefault();
				if (beam != null)
				{
					if (beam.GantryDirection != VMS.TPS.Common.Model.Types.GantryDirection.None)
					{
						return (beam.MLCPlanType == VMS.TPS.Common.Model.Types.MLCPlanType.VMAT) ? "VMAT" : "ARC";
					}
					else
					{
						return (beam.MLCPlanType == VMS.TPS.Common.Model.Types.MLCPlanType.DoseDynamic) ? "IMRT" : "STATIC";
					}
				}
			}

			return "";
		}

		/// <summary>
		/// this event triggers the UI to update the table, when a row changes
		/// </summary>
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
