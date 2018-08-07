using System.Windows.Media;

namespace SimplePdfReport.Reporting
{
    public class DVHTableRow
    {
        public string StructureId { get; set; }
        public string PlanStructureId { get; set; }
        public string Constraint { get; set; }
		public string Limit { get; set; }
		public string VariationConstraint { get; set; }
		public string VariationLimit { get; set; }
		public string PlanValue { get; set; }
		public string PlanResult { get; set; }
		public Brush PlanResultColor { get; set; }
    }
}