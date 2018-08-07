namespace SimplePdfReport.Reporting
{
    public class ReportData
    {
        public Patient Patient { get; set; }
		public Plan Plan { get; set; }
		public User User { get; set; }
        public DVHTable DvhTable { get; set; }
    }
}