using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PortfolioTracker.Models
{
    public class PortfolioItem
    {
        public int ID { get; set; } // מזהה ייחודי
        public string Stock { get; set; }
        public double Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal ChangePercentage { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal Investment { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercentage { get; set; } // אחוזי רווח/הפסד
        public DateTime DateAdded { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
    }
    public class MonthlyTransactionsViewModel
    {
        public string Month { get; set; } // שם החודש (למשל ינואר)
        public List<PortfolioItem> Items { get; set; } // רשימת העסקאות שבוצעו באותו חודש
    }
    public class YearlyProfitViewModel
    {
        public int Year { get; set; } // השנה (למשל 2024)
        public decimal TotalProfit { get; set; } // סך הרווחים לשנה
        public List<MonthlyTransactionsViewModel> Transactions { get; set; } // רשימת עסקאות לפי חודשים
    }


}
