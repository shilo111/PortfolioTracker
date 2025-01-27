using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using PortfolioTracker.Models;

namespace PortfolioTracker.Controllers
{
    public class PortfolioController : Controller
    {
        // רשימת הפוזיציות הפעילות
        private static List<PortfolioItem> portfolio = new List<PortfolioItem>();

        // רשימת היסטוריה
        private static List<PortfolioItem> historyPortfolio = new List<PortfolioItem>();
        private static List<PortfolioItem> portfolioHistory = new List<PortfolioItem>(); // רשימת היסטוריה


        // GET: Portfolio
        public ActionResult Index()
        {
            // הכנת נתוני תיק ההשקעות
            decimal totalInvestment = portfolio.Sum(p => p.Investment);
            decimal portfolioReturns = portfolio.Sum(p => p.ProfitLoss);
            decimal myReturnsPercentage = totalInvestment == 0 ? 0 : (portfolioReturns / totalInvestment) * 100;

            foreach (var item in portfolio)
            {
                item.ProfitLossPercentage = item.Investment == 0 ? 0 : (item.ProfitLoss / item.Investment) * 100;
            }

            // קיבוץ נתונים לפי חודשים
            var groupedData = portfolio
                .GroupBy(p => p.DateAdded.ToString("MMMM yyyy"))
                .Select(g => new
                {
                    Month = g.Key,
                    MonthlyReturn = g.Sum(p => p.ProfitLoss) / (g.Sum(p => p.Investment) == 0 ? 1 : g.Sum(p => p.Investment)) * 100
                })
                .ToList();

            // הכנת נתונים ל-ViewBag
            ViewBag.GroupedData = groupedData;
            ViewBag.Months = groupedData.Select(g => g.Month).ToList();
            ViewBag.MonthlyReturns = groupedData.Select(g => g.MonthlyReturn).ToList();


            // העברת המידע ל-View
            return View(portfolio);
        }



        public ActionResult Alerts()
        {
            return View();
        }


        [HttpGet]
        public JsonResult Sp500Returns(string months)
        {
            var monthList = months.Split(',');
            var returns = monthList.Select(month => new
            {
                Month = month,
                Return = new Random().Next(-10, 10) // נתונים מדומים בין -10% ל-10%
            });

            return Json(returns, JsonRequestBehavior.AllowGet);
        }



        // פעולה להצגת ההיסטוריה
        public ActionResult Full(DateTime? startDate, DateTime? endDate)
        {
            // עדכון אחוזי הרווח/הפסד עבור כל מניה בהיסטוריה
            foreach (var item in portfolioHistory)
            {
                item.ProfitLossPercentage = item.Investment == 0 ? 0 : (item.ProfitLoss / item.Investment) * 100;
            }

            // סינון לפי התקופה המבוקשת
            var filteredHistory = portfolioHistory.AsQueryable();
            if (startDate.HasValue && endDate.HasValue)
            {
                filteredHistory = filteredHistory.Where(p => p.DateAdded >= startDate.Value && p.DateAdded <= endDate.Value);
            }

            decimal totalProfitInRange = filteredHistory.Sum(p => p.ProfitLoss);
            ViewBag.TotalProfitInRange = totalProfitInRange;

            // קיבוץ לפי שנים וחודשים
            var yearlyData = filteredHistory
                .GroupBy(p => p.DateAdded.Year)
                .Select(yearGroup => new YearlyProfitViewModel
                {
                    Year = yearGroup.Key,
                    TotalProfit = yearGroup.Sum(p => p.ProfitLoss),
                    Transactions = yearGroup
                        .GroupBy(p => p.DateAdded.ToString("MMMM"))
                        .Select(monthGroup => new MonthlyTransactionsViewModel
                        {
                            Month = monthGroup.Key,
                            Items = monthGroup.ToList()
                        }).ToList()
                }).ToList();

            // שליחת התקופה ל-ViewBag להצגה בטופס
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(yearlyData);
        }

        [HttpPost]
        public ActionResult FilterByDate(DateTime startDate, DateTime endDate)
        {
            var filteredTransactions = portfolioHistory
                .Where(p => p.DateAdded >= startDate && p.DateAdded <= endDate)
                .ToList();

            decimal totalProfit = filteredTransactions.Sum(p => p.ProfitLoss);

            ViewBag.FilteredTransactions = filteredTransactions;
            ViewBag.TotalProfitForPeriod = totalProfit;

            return View("Full", filteredTransactions);
        }


        [HttpPost]
        public async Task<ActionResult> AddToFull(string stock, decimal purchasePrice, decimal investment, DateTime dateAdded)
        {
            try
            {
                // קריאה ל-API לקבלת מידע עדכני על המניה
                var stockData = await GetStockInfo(stock);
                if (stockData == null)
                {
                    ViewBag.Error = "Failed to fetch stock data. Please check the stock symbol.";
                    return RedirectToAction("Full");
                }

                // חישוב כמות המניות ורווח/הפסד
                double quantity = (double)(investment / purchasePrice);
                decimal profitLoss = ((decimal)quantity * stockData.Price) - investment;
                decimal profitLossPercentage = investment == 0 ? 0 : (profitLoss / investment) * 100;

                // הוספת מניה היסטורית לרשימה
                portfolioHistory.Add(new PortfolioItem
                {
                    Stock = stockData.Stock,
                    Quantity = quantity,
                    PurchasePrice = purchasePrice,
                    Investment = investment,
                    Price = stockData.Price, // מחיר המניה הנוכחי
                    ChangePercentage = stockData.ChangePercentage, // שינוי יומי באחוזים
                    ProfitLoss = profitLoss, // רווח/הפסד
                    ProfitLossPercentage = profitLossPercentage, // רווח/הפסד באחוזים
                    DateAdded = dateAdded // התאריך שסופק
                });

                return RedirectToAction("Full");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred: " + ex.Message;
                return RedirectToAction("Full");
            }
        }


        // פעולה להוספת מניה עם תאריך
        [HttpPost]
        public async Task<ActionResult> AddStock(string stock, decimal purchasePrice, decimal investment, DateTime dateAdded)
        {
            try
            {
                // קריאה ל-API לקבלת מידע עדכני על המניה
                var stockData = await GetStockInfo(stock);
                if (stockData == null)
                {
                    ViewBag.Error = "Failed to fetch stock data. Please check the stock symbol.";
                    return View("Index", portfolio);
                }

                // חישוב כמות המניות ורווח/הפסד
                double quantity = (double)(investment / purchasePrice);
                decimal profitLoss = ((decimal)quantity * stockData.Price) - investment;

                // יצירת פריט מניה חדש
                var newStock = new PortfolioItem
                {
                    Stock = stockData.Stock,
                    Quantity = quantity,
                    PurchasePrice = purchasePrice,
                    Investment = investment,
                    Price = stockData.Price,
                    ChangePercentage = stockData.ChangePercentage,
                    ProfitLoss = profitLoss,
                    DateAdded = dateAdded // הגדרת התאריך שהמשתמש סיפק
                };

                // הוספת המניה לרשימה הראשית
                portfolio.Add(newStock);

                // הוספת המניה גם להיסטוריה
                portfolioHistory.Add(newStock);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred: " + ex.Message;
                return View("Index", portfolio);
            }
        }


        [HttpPost]
        public ActionResult DeleteStockFromHistory(string stock)
        {
            // מציאת המניה ברשימת ההיסטוריה ומחיקתה
            var stockItem = portfolioHistory.FirstOrDefault(s => s.Stock == stock);
            if (stockItem != null)
            {
                portfolioHistory.Remove(stockItem);
            }

            return RedirectToAction("Full");
        }
        [HttpGet]

        public async Task<JsonResult> GetSP500Returns()
        {
            try
            {
                string apiKey = "cuav1s1r01qof06jg10gcuav1s1r01qof06jg110"; // החלף במפתח ה-API שלך
                string symbol = "^GSPC"; // סמל של S&P 500
                string apiUrl = $"https://finnhub.io/api/v1/stock/candle?symbol={symbol}&resolution=M&from={GetUnixTime(DateTime.Now.AddYears(-1))}&to={GetUnixTime(DateTime.Now)}&token={apiKey}";

                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync(apiUrl);
                    var data = JsonConvert.DeserializeObject<dynamic>(response);

                    if (data == null || data["c"] == null)
                    {
                        return Json(new { error = "No data found for S&P 500" }, JsonRequestBehavior.AllowGet);
                    }

                    // שליפת מחירי הסגירה וחישוב תשואות חודשיות
                    var returns = new List<decimal>();
                    var closingPrices = ((IEnumerable<dynamic>)data["c"]).Select(price => (decimal)price).ToList();

                    for (int i = 1; i < closingPrices.Count; i++)
                    {
                        var monthlyReturn = ((closingPrices[i] - closingPrices[i - 1]) / closingPrices[i - 1]) * 100;
                        returns.Add(monthlyReturn);
                    }

                    return Json(returns, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        // פונקציה להמרת תאריך ל-UNIX timestamp
        private long GetUnixTime(DateTime date)
        {
            return ((DateTimeOffset)date).ToUnixTimeSeconds();
        }




        // פעולה למחיקת מניה מהטבלה הפעילה בלבד
        [HttpPost]
        public ActionResult DeleteStock(string stock)
        {
            // מציאת המניה ברשימה הפעילה ומחיקתה
            var stockItem = portfolio.FirstOrDefault(s => s.Stock == stock);
            if (stockItem != null)
            {
                portfolio.Remove(stockItem);
            }

            return RedirectToAction("Index");
        }

        // פעולה לשליפת מידע על מניה מ-API
        private async Task<StockData> GetStockInfo(string symbol)
        {
            try
            {
                string apiKey = "cuav1s1r01qof06jg10gcuav1s1r01qof06jg110"; // שים כאן את ה-API KEY שלך
                string apiUrl = $"https://finnhub.io/api/v1/quote?symbol={symbol}&token={apiKey}";

                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync(apiUrl);
                    var data = JsonConvert.DeserializeObject<dynamic>(response);

                    if (data == null || data["c"] == null)
                    {
                        return null; // אם אין נתונים על המניה
                    }

                    return new StockData
                    {
                        Stock = symbol,
                        Price = (decimal)data["c"], // מחיר המניה הנוכחי
                        ChangePercentage = (decimal)((data["d"] != null && data["pc"] != null)
                            ? ((decimal)data["d"] / (decimal)data["pc"] * 100)
                            : 0) // חישוב אחוז שינוי
                    };
                }
            }
            catch
            {
                return null;
            }
        }




        // מחלקה לשליפת נתוני מניה מה-API
        public class StockData
        {
            public string Stock { get; set; }
            public decimal Price { get; set; }
            public decimal ChangePercentage { get; set; }
        }
    }
}
