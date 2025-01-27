using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PortfolioTracker.Models
{
    public class PortfolioItem
    {
        public string Stock { get; set; }
        public double Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal ChangePercentage { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal Investment { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercentage { get; set; } // אחוזי רווח/הפסד
        public DateTime DateAdded { get; set; }
    }

}
