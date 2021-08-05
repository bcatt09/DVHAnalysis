using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace VMS.TPS
{
	public static class DvhExtensions
	{
		public static DoseValue GetDoseAtVolume(this PlanningItem pitem, Structure structure, double volume, VolumePresentation volumePresentation, DoseValuePresentation requestedDosePresentation, DoseValue? planSumRx = null)
		{
			if (pitem is PlanSetup)
			{
				return ((PlanSetup)pitem).GetDoseAtVolume(structure, volume, volumePresentation, requestedDosePresentation);
			}
			else
			{
				DVHData dvh = pitem.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, volumePresentation, 0.001);

				if (requestedDosePresentation != DoseValuePresentation.Absolute)
				{
					//MessageBox.Show(String.Format("Only absolute dose is supported for Plan Sums.  A prescription dose of {1} will be assumed to convert the D{0}% for {2} into a percentage.", volume, planSumRx.ToString(), structure.Id), String.Format("Error during calculation of D{0}%", volume), MessageBoxButton.OK, MessageBoxImage.Warning);
					return DvhExtensions.DoseAtVolume(dvh, volume, planSumRx);
				}
				else
					return DvhExtensions.DoseAtVolume(dvh, volume);
			}
		}

		public static double GetVolumeAtDose(this PlanningItem pitem, Structure structure, DoseValue dose, VolumePresentation requestedVolumePresentation, DoseValue? planSumRx = null)
		{
			//Get units that the dose is represented in within Eclipse but can't call that function if it's requesting % for a plan sum, so if that's the case it must be set manually
			DoseValue.DoseUnit systemUnits = DoseValue.DoseUnit.Percent;
			if(dose.Unit != DoseValue.DoseUnit.Percent)
				systemUnits = pitem.GetDVHCumulativeData(structure, dose.Unit == DoseValue.DoseUnit.Percent ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute, VolumePresentation.Relative, 1).MeanDose.Unit;

			//When calling GetVolumeAtDose, the dose must be in the same units as the system is set to

			//If "dose" is in %, they should be equal, but we will convert them if they are not equal to each other
			if (dose.Unit != systemUnits)
			{
				if (dose.Unit == DoseValue.DoseUnit.cGy && systemUnits == DoseValue.DoseUnit.Gy)
					dose = new DoseValue(dose.Dose / 100.0, DoseValue.DoseUnit.Gy);
				else if (dose.Unit == DoseValue.DoseUnit.Gy && systemUnits == DoseValue.DoseUnit.cGy)
					dose = new DoseValue(dose.Dose * 100.0, DoseValue.DoseUnit.cGy);
				else
					MessageBox.Show(String.Format("There was an error converting {0}, with units of {1} into the units used by Eclipse, {2}\n\nExpected values were cGy and Gy", dose.ToString(), dose.Unit.ToString(), systemUnits.ToString()), String.Format("Error during calculation of V{0}", dose.ToString()), MessageBoxButton.OK, MessageBoxImage.Warning);
			}

			//Now functions can be called as normal
			if (pitem is PlanSetup)
			{
				return ((PlanSetup)pitem).GetVolumeAtDose(structure, dose, requestedVolumePresentation);
			}
			else
			{
				DVHData dvh = pitem.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, requestedVolumePresentation, 0.001);

				if (dose.Unit == DoseValue.DoseUnit.Percent)
				{
					//MessageBox.Show(String.Format("Only absolute dose is supported for Plan Sums.  A prescription dose of {1} will be assumed to convert the V{0} for {2} into a percentage.", dosecGy.ToString(), planSumRx.ToString(), structure.Id), String.Format("Error during calculation of V{0}", dosecGy.ToString()), MessageBoxButton.OK, MessageBoxImage.Warning);
					return DvhExtensions.VolumeAtDose(dvh, dose.Dose, planSumRx);
				}
				else
					return DvhExtensions.VolumeAtDose(dvh, dose.Dose);
			}
		}

		public static DoseValue GetMeanDose(this PlanningItem pitem, Structure structure, DoseValuePresentation requestedDosePresentation)
		{
			//if it's a plan we can get absolute or relative dose
			if (pitem is PlanSetup || requestedDosePresentation == DoseValuePresentation.Absolute)
				return pitem.GetDVHCumulativeData(structure, requestedDosePresentation, VolumePresentation.Relative, 1).MeanDose;

			//otherwise if it's a plan sum we can only get absolute and must convert to relative
			else
			{
				DoseValue planSumTotalDose = new DoseValue(0, pitem.Dose.DoseMax3D.Unit);

				foreach (PlanSetup psetup in ((PlanSum)pitem).PlanSetups)
					planSumTotalDose += psetup.TotalDose;

				DoseValue temp = pitem.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, VolumePresentation.Relative, 1).MeanDose;

				return new DoseValue(temp.Dose / planSumTotalDose.Dose * 100, DoseValue.DoseUnit.Percent);
			}
		}

		public static DoseValue GetMaxDose(this PlanningItem pitem, Structure structure, DoseValuePresentation requestedDosePresentation)
		{
			//if it's a plan we can get absolute or relative dose
			if (pitem is PlanSetup || requestedDosePresentation == DoseValuePresentation.Absolute)
				return pitem.GetDVHCumulativeData(structure, requestedDosePresentation, VolumePresentation.Relative, 1).MaxDose;

			//otherwise if it's a plan sum we can only get absolute and must convert to relative
			else
			{
				DoseValue planSumTotalDose = new DoseValue(0, pitem.Dose.DoseMax3D.Unit);

				foreach (PlanSetup psetup in ((PlanSum)pitem).PlanSetups)
					planSumTotalDose += psetup.TotalDose;

				DoseValue temp = pitem.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, VolumePresentation.Relative, 1).MaxDose;

				return new DoseValue(temp.Dose / planSumTotalDose.Dose * 100, DoseValue.DoseUnit.Percent);
			}
		}

		public static DoseValue GetMinDose(this PlanningItem pitem, Structure structure, DoseValuePresentation requestedDosePresentation)
		{
			//if it's a plan we can get absolute or relative dose
			if (pitem is PlanSetup || requestedDosePresentation == DoseValuePresentation.Absolute)
				return pitem.GetDVHCumulativeData(structure, requestedDosePresentation, VolumePresentation.Relative, 1).MinDose;

			//otherwise if it's a plan sum we can only get absolute and must convert to relative
			else
			{
				DoseValue planSumTotalDose = new DoseValue(0, pitem.Dose.DoseMax3D.Unit);

				foreach (PlanSetup psetup in ((PlanSum)pitem).PlanSetups)
					planSumTotalDose += psetup.TotalDose;

				DoseValue temp = pitem.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute, VolumePresentation.Relative, 1).MinDose;

				return new DoseValue(temp.Dose / planSumTotalDose.Dose * 100, DoseValue.DoseUnit.Percent);
			}
		}

		public static DoseValue DoseAtVolume(DVHData dvhData, double volume, DoseValue? psumTotalDose = null)
		{
			if (dvhData == null || dvhData.CurveData.Count() == 0)
				return DoseValue.UndefinedDose();
			double absVolume = dvhData.CurveData[0].VolumeUnit == "%" ? volume * dvhData.Volume * 0.01 : volume;
			if (volume < 0.0 || absVolume > dvhData.Volume)
				return DoseValue.UndefinedDose();

			DVHPoint[] hist = dvhData.CurveData;
			for (int i = 0; i < hist.Length; i++)
			{
				if (hist[i].Volume < volume)
				{
					if (psumTotalDose == null)
						return hist[i].DoseValue;
					else
						return new DoseValue(hist[i].DoseValue.Dose / ((DoseValue)psumTotalDose).Dose * 100, DoseValue.DoseUnit.Percent);
				}
			}
			return DoseValue.UndefinedDose();
		}

		public static double VolumeAtDose(DVHData dvhData, double dose, DoseValue? psumTotalDose = null)
		{
			if (psumTotalDose != null)
				dose = dose / 100 * ((DoseValue)psumTotalDose).Dose;

			if (dvhData == null)
				return Double.NaN;

			DVHPoint[] hist = dvhData.CurveData;
			int index = (int)(hist.Length * dose / dvhData.MaxDose.Dose);
			if (index < 0 || index > hist.Length)
				return 0.0;//Double.NaN;
			else
				return hist[index].Volume;
		}

		/// <summary>
		/// Returns true if dose1 > dose2
		/// </summary>
		/// <param name="dose1">dose1</param>
		/// <param name="dose2">dose2</param>
		/// <returns>dose1 > dose2</returns>
		public static bool DoseGreaterThan(DoseValue dose1, DoseValue dose2)
		{
			if (dose1.Unit == dose2.Unit)
				return dose1.Dose > dose2.Dose;
			if (dose1.Unit == DoseValue.DoseUnit.cGy && dose2.Unit == DoseValue.DoseUnit.Gy)
				return dose1.Dose > dose2.Dose * 100.0;
			if (dose1.Unit == DoseValue.DoseUnit.Gy && dose2.Unit == DoseValue.DoseUnit.cGy)
				return dose1.Dose * 100.0 > dose2.Dose;

			MessageBox.Show("Doses must be the same units\ndose1 = " + dose1.Unit.ToString() + "\ndose2 = " + dose2.Unit.ToString(), "Invalid dose types in comparison", MessageBoxButton.OK, MessageBoxImage.Error);
			throw new FormatException("Invalid dose types in comparison");
		}

		/// <summary>
		/// Returns true if dose1 < dose2
		/// </summary>
		/// <param name="dose1">dose1</param>
		/// <param name="dose2">dose2</param>
		/// <returns>dose1 < dose2</returns>
		public static bool DoseLessThan(DoseValue dose1, DoseValue dose2)
		{
			if (dose1.Unit == dose2.Unit)
				return dose1.Dose < dose2.Dose;
			if (dose1.Unit == DoseValue.DoseUnit.cGy && dose2.Unit == DoseValue.DoseUnit.Gy)
				return dose1.Dose < dose2.Dose * 100.0;
			if (dose1.Unit == DoseValue.DoseUnit.Gy && dose2.Unit == DoseValue.DoseUnit.cGy)
				return dose1.Dose * 100.0 < dose2.Dose;

			MessageBox.Show("Doses must be the same units\ndose1 = " + dose1.Unit.ToString() + "\ndose2 = " + dose2.Unit.ToString(), "Invalid dose types in comparison", MessageBoxButton.OK, MessageBoxImage.Error);
			throw new FormatException("Invalid dose types in comparison");
		}

		/// <summary>
		/// Returns dose1 + dose2
		/// </summary>
		/// <param name="dose1">dose1</param>
		/// <param name="dose2">dose2</param>
		/// <returns>dose1 + dose2</returns>
		public static DoseValue AddDoseValues(DoseValue dose1, DoseValue dose2)
		{
			if (dose1.Unit == DoseValue.DoseUnit.Percent || dose2.Unit == DoseValue.DoseUnit.Percent)
			{
				MessageBox.Show("Cannot add percentages\ndose1 = " + dose1.Unit.ToString() + "\ndose2 = " + dose2.Unit.ToString(), "Invalid dose types in comparison", MessageBoxButton.OK, MessageBoxImage.Error);
				throw new FormatException("Invalid dose types in comparison");
			}
			if (dose1.Unit == dose2.Unit)
				return new DoseValue(dose1.Dose + dose2.Dose,dose1.Unit);
			if (dose1.Unit == DoseValue.DoseUnit.cGy && dose2.Unit == DoseValue.DoseUnit.Gy)
				return new DoseValue(dose1.Dose + dose2.Dose * 100, DoseValue.DoseUnit.cGy);
			if (dose1.Unit == DoseValue.DoseUnit.Gy && dose2.Unit == DoseValue.DoseUnit.cGy)
				return new DoseValue(dose1.Dose * 100 + dose2.Dose, DoseValue.DoseUnit.cGy);

			MessageBox.Show("Doses must be the same units\ndose1 = " + dose1.Unit.ToString() + "\ndose2 = " + dose2.Unit.ToString(), "Invalid dose types in comparison", MessageBoxButton.OK, MessageBoxImage.Error);
			throw new FormatException("Invalid dose types in comparison");
		}
	}
}
