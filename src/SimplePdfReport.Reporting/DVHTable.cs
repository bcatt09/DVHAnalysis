using System.Collections.Generic;

namespace SimplePdfReport.Reporting
{
    public class DVHTable
    {
        public string Title { get; set; }
        public List<DVHTableRow> Rows { get; set; }

		public DVHTable()
		{
			Rows = new List<DVHTableRow>();
		}
    }
}