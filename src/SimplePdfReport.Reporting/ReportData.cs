namespace SimplePdfReport.Reporting
{
    public class ReportData
    {
        public Patient Patient { get; set; }
		public Plans Plans { get; set; }
		public User User { get; set; }
        public DVHTable DvhTable { get; set; }
    }
}