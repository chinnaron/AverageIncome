using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Model
    {
        public int AccountNo { get; set; }
        public string AccountName { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public DateTime TxnDate { get; set; }
        public string TxnDescription { get; set; }
        public string Type { get; set; }
        public float TxnAmount { get; set; }
        public float RemainingBalance { get; set; }
        public string Remark { get; set; }

        public string ToLineString()
        {
            return AccountNo + "," + AccountName + "," + From.ToShortDateString() + "," + To.ToShortDateString() + ","
                + TxnDate.ToShortDateString() + "," + TxnDescription + "," + Type + "," + TxnAmount + "," + RemainingBalance + "," + Remark;
        }

        public Model CopyModel()
        {
            if (this == null) return new Model();
            return new Model
            {
                AccountNo = this.AccountNo,
                AccountName = this.AccountName,
                From = this.From,
                To = this.To,
                TxnDate = this.TxnDate,
                TxnDescription = this.TxnDescription,
                Type = this.Type,
                TxnAmount = this.TxnAmount,
                RemainingBalance = this.RemainingBalance,
                Remark = this.Remark
            };
        }
    }
}
