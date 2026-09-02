using System;

namespace Easy_Copier.Models
{
    public class SmartAdderHistoryRecord
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string EntriesJson { get; set; } = "[]";
        public double TotalSum { get; set; }
        public double Total
        {
            get => TotalSum;
            set => TotalSum = value;
        }
    }
}
