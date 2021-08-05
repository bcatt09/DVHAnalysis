using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace VMS.TPS
{
	//class that handles getting values of constraints from the xml file for comparing doses
	public static class  ConstraintList
	{
		private static String exeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
		private static String xmlFileLocation = $@"{exeDirectory}\Constraints.xml";
		private static XElement _protocolListRoot = XElement.Load(xmlFileLocation);

		/// <summary>
		/// Gets a list of categories to choose from
		/// </summary>
		public static IEnumerable<String> GetCategories()
		{
			IEnumerable<String> categoryNames = from el in _protocolListRoot.Elements()
												select el.Attribute("category").Value;
			categoryNames = new[] { "(All)" }.Concat(categoryNames);    //add an "All" category at the beginning
			categoryNames = categoryNames.Distinct().ToList();          //only keep one of each category for the list

			return categoryNames;
		}

		/// <summary>
		/// Gets a list of protocol to choose from given a category
		/// </summary>
		public static IEnumerable<String> GetProtocols(string category = "(All)")
		{
			if (category == "(All)")
				return from el in _protocolListRoot.Elements() select el.Attribute("name").Value;
			else
				return (from el in _protocolListRoot.Elements()
						where el.Attribute("category").Value == category
						select el.Attribute("name").Value);
		}

		/// <summary>
		/// Gets the protocol node
		/// </summary>
		public static XElement GetProtocol(string protocol)
		{
			return (from prot in _protocolListRoot.Elements("protocol")
					where (string)prot.Attribute("name") == protocol
					select prot).Single();
		}

		/// <summary>
		/// Gets the name of the given protocol
		/// </summary>
		public static string GetProtocolName(string protocol)
		{
			//if selected protocol has a "study" name return that
			if ((from prot in _protocolListRoot.Elements("protocol")
				 where (string)prot.Attribute("name") == protocol
				 select prot).Single().Attribute("study").Value != null)
				return (from prot in _protocolListRoot.Elements("protocol")
						where (string)prot.Attribute("name") == protocol
						select prot).Single().Attribute("study").Value;
			//otherwise just return the name in the dropdown list
			else
				return protocol;
		}

		/// <summary>
		/// Gets the structure node
		/// </summary>
		public static XElement GetStructure(string structure, string protocol)
		{
			return (from struc in GetProtocol(protocol).Elements("structure")
					where (string)struc.Attribute("name") == structure
					select struc).Single();
		}

		/// <summary>
		/// Gets the constraint node
		/// </summary>
		public static XElement GetConstraint(string constraint, string structure, string protocol)
		{
			return (from constr in GetStructure(structure, protocol).Elements("constraint")
					where (string)constr.Attribute("constraint") == constraint
					select constr).Single();
		}

		/// <summary>
		/// Gets a list of constraint nodes for the given structure and protocol
		/// </summary>
		public static IEnumerable<XElement> GetConstraints(string structure, string protocol)
		{
			return GetStructure(structure, protocol).Elements();
		}

		/// <summary>
		/// Gets the value of the constraint in string format
		/// </summary>
		public static string GetConstraintText(XElement constraint)
		{
			return constraint.Attribute("constraint").Value;
		}

		/// <summary>
		/// Gets the acceptable variation value of the constraint in string format
		/// </summary>
		public static string GetVariationConstraintText(XElement constraint)
		{
			return constraint.Attribute("variation-constraint").Value;
		}

		/// <summary>
		/// Gets the limit for the constraint in double format
		/// </summary>
		public static double GetConstraintLimit(XElement constraint)
		{
			return Double.Parse(constraint.Attribute("limit").Value);
		}

		/// <summary>
		/// Gets the acceptable variation limit for the constraint in double format
		/// </summary>
		public static double GetConstraintVariationLimit(XElement constraint)
		{
			return Double.Parse(constraint.Attribute("variation-limit").Value);
		}

		/// <summary>
		/// Gets the goal of the constraint (true for >, false for <)
		/// </summary>
		public static bool GetConstraintGoal(XElement constraint)
		{
			return constraint.Attribute("goal").Value == "greater" ? true : false;
		}
	}
}
