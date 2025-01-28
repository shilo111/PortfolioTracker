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
        public ActionResult Index(int? selectedYear)
        {
            // סינון מניות פעילות בלבד
            var activePortfolio = portfolio.Where(p => p.IsActive).ToList();

            // חישוב נתונים עבור המניות הפעילות
            decimal totalInvestment = activePortfolio.Sum(p => p.Investment);
            decimal portfolioReturns = activePortfolio.Sum(p => p.ProfitLoss);
            decimal myReturnsPercentage = totalInvestment == 0 ? 0 : (portfolioReturns / totalInvestment) * 100;

            foreach (var item in activePortfolio)
            {
                item.ProfitLossPercentage = item.Investment == 0 ? 0 : (item.ProfitLoss / item.Investment) * 100;
            }

            // קיבוץ נתונים לפי חודשים ושנים
            var groupedData = portfolioHistory
                .Where(p => !selectedYear.HasValue || p.DateAdded.Year == selectedYear.Value) // סינון לפי שנה
                .OrderBy(p => p.DateAdded) // מיון עולה לפי תאריך
                .GroupBy(p => new { p.DateAdded.Year, p.DateAdded.Month }) // קיבוץ לפי שנה וחודש
                .Select(g => new
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1), // יצירת תאריך מהשנה והחודש
                    MonthlyReturn = g.Sum(p => p.ProfitLoss) / (g.Sum(p => p.Investment) == 0 ? 1 : g.Sum(p => p.Investment)) * 100
                })
                .OrderBy(g => g.Date) // מיון נוסף לפי התאריך
                .ToList();

            // רשימת השנים לבחירה ב-Dropdown
            ViewBag.Years = portfolioHistory.Select(p => p.DateAdded.Year).Distinct().OrderByDescending(y => y).ToList();
            ViewBag.SelectedYear = selectedYear;


            // הכנת נתונים ל-ViewBag
            ViewBag.Months = groupedData.Select(g => g.Date.ToString("MM/yyyy")).ToList(); // פורמט: חודש/שנה
            ViewBag.MonthlyReturns = groupedData.Select(g => g.MonthlyReturn).ToList();

            // העברת המידע ל-View
            return View(activePortfolio);
        }



        [HttpGet]
        public JsonResult GetYearlyData(int year)
        {
            var dataForYear = portfolioHistory
                .Where(p => p.DateAdded.Year == year)
                .GroupBy(p => p.DateAdded.ToString("MMMM"))
                .Select(g => new {
                    Month = g.Key,
                    Return = g.Sum(p => p.ProfitLoss) / (g.Sum(p => p.Investment) == 0 ? 1 : g.Sum(p => p.Investment)) * 100
                })
                .OrderBy(g => DateTime.ParseExact(g.Month, "MMMM", System.Globalization.CultureInfo.InvariantCulture))
                .ToList();

            var months = dataForYear.Select(d => d.Month).ToList();
            var returns = dataForYear.Select(d => d.Return).ToList();

            return Json(new { months, returns }, JsonRequestBehavior.AllowGet);
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
        public async Task<ActionResult> Full(DateTime? startDate, DateTime? endDate)
        {
            // עדכון אחוזי הרווח/הפסד עבור מניות פעילות
            foreach (var item in portfolioHistory.Where(p => p.IsActive))
            {
                var stockData = await GetStockInfo(item.Stock); // קריאה ל-API
                if (stockData != null)
                {
                    item.Price = stockData.Price; // עדכון המחיר הנוכחי
                    item.ChangePercentage = stockData.ChangePercentage; // עדכון אחוז השינוי
                    item.ProfitLoss = ((decimal)item.Quantity * item.Price) - item.Investment; // חישוב רווח/הפסד
                    item.ProfitLossPercentage = item.Investment == 0 ? 0 : (item.ProfitLoss / item.Investment) * 100; // חישוב אחוזי רווח/הפסד
                }
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
             .OrderByDescending(p => p.DateAdded) // מיון לפי תאריך בסדר יורד
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
                         Items = monthGroup.OrderByDescending(p => p.DateAdded).ToList() // מיון לפי תאריך בסדר יורד
                     }).ToList()
             }).ToList();


            // שליחת התקופה ל-ViewBag להצגה בטופס
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(yearlyData);
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
                    ID = portfolioHistory.Any() ? portfolioHistory.Max(p => p.ID) + 1 : 1, // ID ייחודי

                    Stock = stockData.Stock,
                    Quantity = quantity,
                    PurchasePrice = purchasePrice,
                    Investment = investment,
                    Price = stockData.Price, // מחיר המניה הנוכחי
                    ChangePercentage = stockData.ChangePercentage, // שינוי יומי באחוזים
                    ProfitLoss = profitLoss, // רווח/הפסד
                    ProfitLossPercentage = profitLossPercentage, // רווח/הפסד באחוזים
                    DateAdded = dateAdded, // התאריך שסופק
                    IsActive = false, // הופך ללא פעיל מיד
                    IsDeleted = true // הגדרת השדה החדש
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
                      ID = portfolioHistory.Any() ? portfolioHistory.Max(p => p.ID) + 1 : 1, // ID ייחודי

                    Stock = stockData.Stock,
                    Quantity = quantity,
                    PurchasePrice = purchasePrice,
                    Investment = investment,
                    Price = stockData.Price,
                    ChangePercentage = stockData.ChangePercentage,
                    ProfitLoss = profitLoss,
                    DateAdded = dateAdded, // הגדרת התאריך שהמשתמש סיפק
                    IsActive = true
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
       
        public ActionResult ToggleActiveStatus(int id)
        {
            var stock = portfolioHistory.FirstOrDefault(p => p.ID == id);
            if (stock == null)
            {
                TempData["Error"] = "Stock not found.";
                return RedirectToAction("Full");
            }

            // בדיקה אם המנייה נמחקה לצמיתות
            if (stock.IsDeleted)
            {
                TempData["Error"] = "This stock has been permanently disabled and cannot be re-enabled.";
                return RedirectToAction("Full");
            }

            // שינוי הסטטוס בין Active ל-Inactive
            stock.IsActive = !stock.IsActive;

            // אם הסטטוס הפך ל-Inactive, סמן את המנייה כ-Deleted
            if (!stock.IsActive)
            {
                stock.IsDeleted = true;
            }

            return RedirectToAction("Full");
        }






        [HttpPost]
        public ActionResult DeleteStockFromHistory(int id)
        {
            var transactionToRemove = portfolioHistory.FirstOrDefault(p => p.ID == id);
            if (transactionToRemove != null)
            {
                portfolioHistory.Remove(transactionToRemove); // הסרה מהרשימה
            }

            // חזרה לדף Full
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
