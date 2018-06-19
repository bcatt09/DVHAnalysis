using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace VMS.TPS
{
	public enum ConstraintType
	{
		Dose = 0,
		Volume = 1,
		Mean = 2,
		Max = 3,
		Min = 4
	}

	public class DVHTableRow : INotifyPropertyChanged
	{
		private string _structure;				//structure name from protocol
		private Structure _selectedStructure;   //selected structure from structure set
		private string _constraintText;         //string holding constraint text in format D95% or V20Gy
		private string _variationConstraintText;//if the acceptable variation is evaluated with a different constraint, it is stored here
		private string _limitText;              //text form of the constraint limit
		private string _variationLimitText;     //text form of the acceptable variation limit
		private ConstraintType _constraintType; //dose, volume, mean, max, or min constraint
		private double _constraint;				//value number of the constraint (eg 95 for D95%)
		private double _variationConstraint;    //if the acceptable variation is evaluated with a different constraint, it is stored here
		private string _constraintUnits;		//units of the constraint itself
		private bool _goalGreaterThan;          //true for greater, false for less
		private double _limit;                  //limit for the constraint
		private double _variationLimit;         //limit for the acceptable variation
		private string _limitUnits;             //units of the constraint
		private double _planValue;              //the result of the constraint evaluation
		private string _planValueText;          //plan value converted to text with units
		private double _planVariationValue;     //the result of the variation constraint evaluation
		private string _planVariationValueText; //plan variation value converted to text with units
		private string _planResult;             //did the plan pass the constraint?
		private string _endpoint;               //biological endpoint associated with constraint
		private string _reference;				//where did the constraint come from?
		private DVHViewModel _viewModel;        //reference to the ViewModel
		private Visibility _structureVisibility;//should the structure name and dropdown be visible?  (only for the first occurance of the structure in the table)
		private System.Windows.Media.Brush _planResultColor;	//what color to display the result on

		public string Structure { get { return _structure; } set { _structure = value; OnPropertyChanged("Structure"); } }
		public Structure SelectedStructure { get { return _selectedStructure; } set { _selectedStructure = value; StructureDropdownChanged(); OnPropertyChanged("SelectedStructure"); } }
		public string ConstraintText { get { return _constraintText; } set { _constraintText = value; OnPropertyChanged("ConstraintText"); } }
		public string VariationConstraintText { get { return _variationConstraintText; } set { _variationConstraintText = value; OnPropertyChanged("VariationConstraintText"); } }
		public string LimitText { get { return _limitText; } set { _limitText = value; OnPropertyChanged("LimitText"); } }
		public string VariationLimitText { get { return _variationLimitText; } set { _variationLimitText = value; OnPropertyChanged("VariationLimitText"); } }
		public ConstraintType ConstraintType { get { return _constraintType; } set { _constraintType = value; OnPropertyChanged("ConstraintType"); } }
		public double Constraint { get { return _constraint; } set { _constraint = value; OnPropertyChanged("Constraint"); } }
		public double VariationConstraint { get { return _variationConstraint; } set { _variationConstraint = value; OnPropertyChanged("VariationConstraint"); } }
		public string ConstraintUnits { get { return _constraintUnits; } set { _constraintUnits = value; OnPropertyChanged("ConstraintUnits"); } }
		public bool GoalGreaterThan { get { return _goalGreaterThan; } set { _goalGreaterThan = value; OnPropertyChanged("GoalGreaterThan"); } }
		public double Limit { get { return _limit; } set { _limit = value; OnPropertyChanged("Limit"); } }
		public double VariationLimit { get { return _variationLimit; } set { _variationLimit = value; OnPropertyChanged("VariationLimit"); } }
		public string LimitUnits { get { return _limitUnits; } set { _limitUnits = value; OnPropertyChanged("LimitUnits"); } }
		public double PlanValue { get { return _planValue; } set { _planValue = value; OnPropertyChanged("PlanValue"); } }
		public string PlanValueText { get { return _planValueText; } set { _planValueText = value; OnPropertyChanged("PlanValueText"); } }
		public double PlanVariationValue { get { return _planVariationValue; } set { _planVariationValue = value; OnPropertyChanged("PlanVariationValue"); } }
		public string PlanVariationValueText { get { return _planVariationValueText; } set { _planVariationValueText = value; OnPropertyChanged("PlanVariationValueText"); } }
		public string PlanResult { get { return _planResult; } set { _planResult = value; OnPropertyChanged("PlanResult"); } }
		public string Endpoint { get { return _endpoint; } set { _endpoint = value; OnPropertyChanged("Endpoint"); } }
		public string Reference { get { return _reference; } set { _reference = value; OnPropertyChanged("Reference"); } }
		public Visibility StructureVisibility { get { return _structureVisibility; } set { _structureVisibility = value; OnPropertyChanged("StructureVisibility"); } }
		public System.Windows.Media.Brush PlanResultColor { get { return _planResultColor; } set { _planResultColor = value; OnPropertyChanged("PlanResultColor"); } }

		public DVHTableRow(XElement constraint, DVHViewModel viewModel)
		{
			_viewModel = viewModel;

			//set structures
			Structure = constraint.Parent.Attribute("name").Value;
			SelectedStructure = GetBestStructureMatch(Structure);

			//get info from xml
			ConstraintText = constraint.Attribute("constraint").Value;
			VariationConstraintText = constraint.Attribute("variation-constraint") == null ? "" : constraint.Attribute("goal").Value == "greater" ? "> " + constraint.Attribute("constraint").Value : "< " + constraint.Attribute("constraint").Value;
			LimitText = constraint.Attribute("goal").Value == "greater" ? "> " + constraint.Attribute("limit").Value : "< " + constraint.Attribute("limit").Value;
			VariationLimitText = constraint.Attribute("variation-limit") == null ? "" : constraint.Attribute("goal").Value == "greater" ? "> " + constraint.Attribute("variation-limit").Value : "< " + constraint.Attribute("variation-limit").Value;
			ConstraintType = GetConstraintType(constraint);
			Constraint = (ConstraintType == ConstraintType.Dose || ConstraintType == ConstraintType.Volume) ? ConstraintParsing.ExtractNumber(constraint.Attribute("constraint").Value) : -1;
			VariationConstraint = constraint.Attribute("variation-constraint") == null ? -1 : ConstraintParsing.ExtractNumber(constraint.Attribute("variation-constraint").Value);
			ConstraintUnits = (ConstraintType == ConstraintType.Dose || ConstraintType == ConstraintType.Volume) ? ConstraintParsing.ExtractUnits(constraint.Attribute("constraint").Value) : "";
			GoalGreaterThan = ConstraintList.GetConstraintGoal(constraint);
			Limit = ConstraintParsing.ExtractNumber(constraint.Attribute("limit").Value);
			VariationLimit = constraint.Attribute("variation-limit") == null ? -1 : ConstraintParsing.ExtractNumber(constraint.Attribute("variation-limit").Value);
			LimitUnits = ConstraintParsing.ExtractUnits(constraint.Attribute("limit").Value);
			Endpoint = constraint.Attribute("endpoint") == null ? "" : "Endpoint: " + constraint.Attribute("endpoint").Value;
			Reference = constraint.Attribute("reference") == null ? "" : "Reference: " + constraint.Attribute("reference").Value;

			if (Endpoint != "" && Reference != "")
				Endpoint += "\n";

			//compute results
			ComputePlanValues();
			ComputePlanResult();

			StructureVisibility = viewModel.DVHTable.Where(s => s.Structure == Structure).Count() > 0 ? Visibility.Hidden : Visibility.Visible;
		}

		/// <summary>
		/// Computes the planned value of each constraint in the selected protocol
		/// </summary>
		private void ComputePlanValues()
		{
			//make sure structures are valid
			PlanResult = "";
			if (SelectedStructure.Name.Contains("Couch"))
				PlanResult = "Couch structure cannot be used";
			else if (SelectedStructure.IsEmpty)
				PlanResult = "Structure is empty";
			else if (SelectedStructure.DicomType == "MARKER")
				PlanResult = "Structure is a Patient Marker";
			else
			{
				if (ConstraintType == ConstraintType.Dose)
				{
					VolumePresentation volPres = ConstraintUnits == "%" ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3;
					DoseValuePresentation dosePres = LimitUnits == "%" ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute;
					
					DoseValue val = _viewModel.SelectedPlanningItem.GetDoseAtVolume(SelectedStructure, Constraint, volPres, dosePres, _viewModel.PlanSumTotalDose);
					DoseValue varVal = VariationConstraint != -1 ? _viewModel.SelectedPlanningItem.GetDoseAtVolume(SelectedStructure, VariationConstraint, volPres, dosePres, _viewModel.PlanSumTotalDose) : new DoseValue();

					//convert to correct units for display
					if (!LimitUnits.Contains("%"))
					{
						val = ConvertDoseUnits(val, LimitUnits);
						varVal = ConvertDoseUnits(varVal, LimitUnits);
					}

					PlanValue = val.Dose;
					PlanValueText = val.ToString();
					PlanVariationValue = varVal.Dose;
					PlanVariationValueText = varVal.ToString();
				}
				else if (ConstraintType == ConstraintType.Volume)
				{
					DoseValuePresentation dosePres = ConstraintUnits == "%" ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute;
					VolumePresentation volPres = LimitUnits == "%" ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3;
					
					DoseValue.DoseUnit doseUnit = ConstraintUnits.ToLower().Contains("cgy") ? DoseValue.DoseUnit.cGy : DoseValue.DoseUnit.Gy;
					
					double vol = dosePres == DoseValuePresentation.Absolute ? _viewModel.SelectedPlanningItem.GetVolumeAtDose(SelectedStructure, new DoseValue(Constraint, doseUnit), volPres) : _viewModel.SelectedPlanningItem.GetVolumeAtDose(SelectedStructure, new DoseValue(Constraint, DoseValue.DoseUnit.Percent), volPres, _viewModel.PlanSumTotalDose);
					double varVol = VariationConstraint != -1 ? _viewModel.SelectedPlanningItem.GetVolumeAtDose(SelectedStructure, new DoseValue(VariationConstraint, doseUnit), volPres, _viewModel.PlanSumTotalDose) : -1;

					PlanValue = Math.Round(vol, 1);
					PlanValueText = PlanValue.ToString() + (volPres == VolumePresentation.Relative ? " %" : " cc");
				}
				else if (ConstraintType == ConstraintType.Mean)
				{
					DoseValuePresentation dosePres = dosePres = LimitUnits == "%" ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute;

					DoseValue val = _viewModel.SelectedPlanningItem.GetMeanDose(SelectedStructure, dosePres);
					DoseValue varVal = VariationConstraint != -1 ? _viewModel.SelectedPlanningItem.GetMeanDose(SelectedStructure, dosePres) : new DoseValue();

					//convert to correct units for display
					if (!LimitUnits.Contains("%"))
					{
						val = ConvertDoseUnits(val, LimitUnits);
						varVal = ConvertDoseUnits(varVal, LimitUnits);
					}

					PlanValue = val.Dose;
					PlanValueText = val.ToString();
				}
				else if (ConstraintType == ConstraintType.Max)
				{
					DoseValuePresentation dosePres = dosePres = LimitUnits == "%" ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute;

					DoseValue val = _viewModel.SelectedPlanningItem.GetMaxDose(SelectedStructure, dosePres);
					DoseValue varVal = VariationConstraint != -1 ? _viewModel.SelectedPlanningItem.GetMaxDose(SelectedStructure, dosePres) : new DoseValue();

					//convert to correct units for display
					if (!LimitUnits.Contains("%"))
					{
						val = ConvertDoseUnits(val, LimitUnits);
						varVal = ConvertDoseUnits(varVal, LimitUnits);
					}

					PlanValue = val.Dose;
					PlanValueText = val.ToString();
				}
				else if (ConstraintType == ConstraintType.Min)
				{
					DoseValuePresentation dosePres = dosePres = LimitUnits == "%" ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute;

					DoseValue val = _viewModel.SelectedPlanningItem.GetMinDose(SelectedStructure, dosePres);
					DoseValue varVal = VariationConstraint != -1 ? _viewModel.SelectedPlanningItem.GetMinDose(SelectedStructure, dosePres) : new DoseValue();

					//convert to correct units for display
					if (!LimitUnits.Contains("%"))
					{
						val = ConvertDoseUnits(val, LimitUnits);
						varVal = ConvertDoseUnits(varVal, LimitUnits);
					}

					PlanValue = val.Dose;
					PlanValueText = val.ToString();
				}
				else
				{
					MessageBox.Show("Invalid constraint type: " + ConstraintType + " for structure " + Structure, "Invalid Constraint Type", MessageBoxButton.OK, MessageBoxImage.Error);
					throw new FormatException("Invalid constraint type: " + ConstraintType + " for structure " + Structure);
				}
			}
		}

		private void ComputePlanResult()
		{
			if (PlanResult == "")
			{
				//make comparisons
				if (GoalGreaterThan)
				{
					if (PlanValue >= Limit)
						PlanResult = "Pass";
					else if (VariationConstraint != -1)
					{
						if (PlanValue >= VariationLimit)
							PlanResult = "Acceptable Variation";
						else
							PlanResult = "Fail";
					}
					else
						PlanResult = "Fail";
				}
				else
				{
					if (PlanValue < Limit)
						PlanResult = "Pass";
					else if (VariationConstraint != -1)
					{
						if (PlanValue < VariationLimit)
							PlanResult = "Acceptable Variation";
						else
							PlanResult = "Fail";
					}
					else
						PlanResult = "Fail";
				}
			}

			//set colors
			if (PlanResult == "Pass")
				PlanResultColor = System.Windows.Media.Brushes.LimeGreen;
			else if (PlanResult == "Fail")
				PlanResultColor = System.Windows.Media.Brushes.OrangeRed;
			else
				PlanResultColor = System.Windows.Media.Brushes.Gold;
		}

		public void RecalcRow()
		{
			ComputePlanValues();
			ComputePlanResult();
		}

		/// <summary>
		/// Converts the units of a DoseValue between cGy and Gy
		/// </summary>
		/// <param name="dose">Dose</param>
		/// <param name="units">Requested units</param>
		/// <returns>Dose converted to the new units</returns>
		private DoseValue ConvertDoseUnits(DoseValue dose, DoseValue.DoseUnit units)
		{
			if (dose.Dose == 0 || dose.Unit == DoseValue.DoseUnit.Unknown)
				return dose;
			if (dose.Unit != DoseValue.DoseUnit.cGy && dose.Unit != DoseValue.DoseUnit.Gy)
			{
				MessageBox.Show("Only units of cGy and Gy can be converted\nVariable: dose.Unit = " + dose.Unit.ToString(), "Invalid Dose Unit", MessageBoxButton.OK, MessageBoxImage.Error);
				throw new FormatException("Invalid dose unit");
			}
			else if (units != DoseValue.DoseUnit.cGy && units != DoseValue.DoseUnit.Gy)
			{
				MessageBox.Show("Can only convert to units of cGy and Gy\nVariable: units = " + units.ToString(), "Invalid Dose Unit", MessageBoxButton.OK, MessageBoxImage.Error);
				throw new FormatException("Invalid dose unit");
			}

			if (dose.Unit == units)
				return dose;
			else if (dose.Unit == DoseValue.DoseUnit.cGy)
				return new DoseValue(dose.Dose / 100.0, DoseValue.DoseUnit.Gy);
			else
				return new DoseValue(dose.Dose * 100.0, DoseValue.DoseUnit.cGy);
		}

		/// <summary>
		/// Converts the units of a DoseValue between cGy and Gy
		/// </summary>
		/// <param name="dose">Dose</param>
		/// <param name="units">Requested units in string form</param>
		/// <returns>Dose converted to the new units</returns>
		private DoseValue ConvertDoseUnits(DoseValue dose, string units)
		{
			if (dose.Dose == 0 || dose.Unit == DoseValue.DoseUnit.Unknown)
				return dose;
			if (dose.Unit != DoseValue.DoseUnit.cGy && dose.Unit != DoseValue.DoseUnit.Gy)
			{
				MessageBox.Show("Only units of cGy and Gy can be converted\nVariable: dose.Unit = " + dose.Unit.ToString(), "Invalid Dose Unit", MessageBoxButton.OK, MessageBoxImage.Error);
				throw new FormatException("Invalid dose unit");
			}
			else if (units != "cGy" && units != "Gy")
			{
				MessageBox.Show("Can only convert to units of cGy and Gy\nVariable: units = " + units, "Invalid Dose Unit", MessageBoxButton.OK, MessageBoxImage.Error);
				throw new FormatException("Invalid dose unit");
			}

			if (dose.Unit.ToString() == units)
				return dose;
			else if (dose.Unit == DoseValue.DoseUnit.cGy)
				return new DoseValue(dose.Dose / 100.0, DoseValue.DoseUnit.Gy);
			else
				return new DoseValue(dose.Dose * 100.0, DoseValue.DoseUnit.cGy);
		}

		/// <summary>
		/// Determines the constraint type
		/// </summary>
		/// <param name="constraint">XML constraint node</param>
		/// <returns></returns>
		private ConstraintType GetConstraintType(XElement constraint)
		{
			string constr = constraint.Attribute("constraint").Value;

			//searches for the constraint type by checking the beginning of the string for a D or V followed by a number or Mean/Max/Min
			if (Regex.Match(constr, @"D\d").Success)
				return ConstraintType.Dose;
			else if (Regex.Match(constr, @"V\d").Success)
				return ConstraintType.Volume;
			else if (Regex.Match(constr, "Mean", RegexOptions.IgnoreCase).Success)
				return ConstraintType.Mean;
			else if (Regex.Match(constr, "Max", RegexOptions.IgnoreCase).Success)
				return ConstraintType.Max;
			else if (Regex.Match(constr, "Min", RegexOptions.IgnoreCase).Success)
				return ConstraintType.Min;
			else
			{
				MessageBox.Show("Invalid constraint: " + constr + " for structure " + constraint.Parent.Attribute("name").Value, "Invalid Constraint Type", MessageBoxButton.OK, MessageBoxImage.Error);
				throw new FormatException("Invalid constraint: " + constr + " for structure " + constraint.Parent.Attribute("name").Value);
			}
		}

		private DoseValue.DoseUnit GetDoseUnit(string unit)
		{
			if (unit.Contains("%") || unit.ToLower().Contains("percent"))
				return DoseValue.DoseUnit.Percent;
			else if (unit.ToLower().Contains("cgy"))
				return DoseValue.DoseUnit.cGy;
			else if (unit.ToLower().Contains("gy"))
				return DoseValue.DoseUnit.Gy;
			else
			{
				MessageBox.Show("Unknown dose unit: " + unit, "Unknown Dose Unit", MessageBoxButton.OK, MessageBoxImage.Warning);
				return DoseValue.DoseUnit.Unknown;
			}
		}

		/// <summary>
		/// Returns the Structure with the name most similar to the given string
		/// </summary>
		/// <param name="structure">Name to find the best match of</param>
		/// <returns>The Structure in the selected structure set with the most similar name to the given string</returns>
		private Structure GetBestStructureMatch(string structure)
		{
			Structure bestMatch = _viewModel.SelectedStructureSet.Structures.First();
			int bestMatchValue = 1000;

			foreach (Structure planStruc in _viewModel.SelectedStructureSet.Structures)
			{
				int tempDist = ComputeDistance(structure, planStruc.Id);

				//update best match of structure names, but don't want to select patient markers or couch structures
				if (tempDist <= bestMatchValue && planStruc.DicomType != "MARKER" && !planStruc.Id.Contains("Couch") && planStruc.Id.Length > 0)
				{
					if (tempDist == bestMatchValue && planStruc.IsEmpty)
						continue;
					else
					{
						bestMatchValue = tempDist;
						bestMatch = planStruc;
					}
				}
			}

			return bestMatch;
		}

		/// <summary>
		/// Compares how similar two strings are, lower number means more similarity
		/// </summary>
		private static int ComputeDistance(string s, string t)
		{
			//if they're equal
			if (s == t)
				return -2;

			//if one contains the full other one
			if (s.ToLower().Contains(t.ToLower()) || t.ToLower().Contains(s.ToLower()))
				return -1;

			var bounds = new { Height = s.Length + 1, Width = t.Length + 1 };

			int[,] matrix = new int[bounds.Height, bounds.Width];

			for (int height = 0; height < bounds.Height; height++) { matrix[height, 0] = height; };
			for (int width = 0; width < bounds.Width; width++) { matrix[0, width] = width; };

			for (int height = 1; height < bounds.Height; height++)
			{
				for (int width = 1; width < bounds.Width; width++)
				{
					int cost = (s[height - 1] == t[width - 1]) ? 0 : 1;
					int insertion = matrix[height, width - 1] + 1;
					int deletion = matrix[height - 1, width] + 1;
					int substitution = matrix[height - 1, width - 1] + cost;

					int distance = Math.Min(insertion, Math.Min(deletion, substitution));

					if (height > 1 && width > 1 && s[height - 1] == t[width - 2] && s[height - 2] == t[width - 1])
					{
						distance = Math.Min(distance, matrix[height - 2, width - 2] + cost);
					}

					matrix[height, width] = distance;
				}
			}

			//if one contains a word of the other one reduce the distance by a bit
			foreach (string str in s.Replace('_', ' ').Split(' '))
				foreach (string tri in t.Replace('_', ' ').Split(' '))
					if (str.ToLower().Contains(tri.ToLower()) || tri.ToLower().Contains(str.ToLower()))
						matrix[bounds.Height - 1, bounds.Width - 1] /= 2;

			return matrix[bounds.Height - 1, bounds.Width - 1];
		}

		/// <summary>
		/// Handle the plan structure being changed in the dropdown
		/// </summary>
		private void StructureDropdownChanged()
		{
			if(!_viewModel.InitialTableLoad)
			{
				//change other dropdowns
				foreach(DVHTableRow row in _viewModel.DVHTable)
				{
					if (Structure == row.Structure && SelectedStructure != row.SelectedStructure)
						row.SelectedStructure = SelectedStructure;
				}

				//recompute planned values
				ComputePlanValues();
				ComputePlanResult();

			}
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

	public static class ConstraintParsing
	{
		public static double ExtractNumber(String str)
		{
			Match match = Regex.Match(str, @"(?:D|V)?\x20*(\d+\.?\d*)\x20*(?:%|Gy|cGy|cc)", RegexOptions.IgnoreCase);

			if (!match.Success)
			{
				MessageBox.Show("Number could not be found: " + str, "Incorrect Constraint Format", MessageBoxButton.OK, MessageBoxImage.Error);
				throw new FormatException("Incorrect constraint format, number could not be found: " + str);
			}

			if (Double.TryParse(match.Groups[1].Value, out double returnVal))
				return returnVal;
			else
			{
				MessageBox.Show("Could not parse the extracted number, " + match.Value + ", to a double", "Incorrect Number Format", MessageBoxButton.OK, MessageBoxImage.Error);
				throw new FormatException("Could not parse the extracted number, " + match.Value + ", to a double");
			}
		}

		public static string ExtractUnits(String str)
		{
			Match match = Regex.Match(str, @"(?:D|V)?\x20*\d+\.?\d*\x20*(%|Gy|cGy|cc)", RegexOptions.IgnoreCase);

			if (!match.Success)
			{
				MessageBox.Show("Incorrect constraint format, units could not be found: " + str, "Invalid Constraint Format", MessageBoxButton.OK, MessageBoxImage.Error);
				throw new FormatException("Units could not be found: " + str);
			}

			return match.Groups[1].Value;
		}
	}
}
