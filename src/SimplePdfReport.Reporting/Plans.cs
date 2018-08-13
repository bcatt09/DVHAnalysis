using System.Collections.Generic;

namespace SimplePdfReport.Reporting
{
    public class Plans
	{
		public string Id { get; set; }
		public string Course { get; set; }
		public string Protocol { get; set; }
		public List<Plan> PlanList { get; set; }
	}
}