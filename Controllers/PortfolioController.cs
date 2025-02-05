using System;
using System.Collections.Generic;
using System.Data.Entity;
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
        private static List<MarketReturn> marketReturns = new List<MarketReturn>();
        private readonly TradingJournalContext _context = new TradingJournalContext();

        // רשימת היסטוריה
        private static List<PortfolioItem> historyPortfolio = new List<PortfolioItem>();
        private static List<PortfolioItem> portfolioHistory = new List<PortfolioItem>(); // רשימת היסטוריה


        // GET: Portfolio
        public async Task<ActionResult> Index(int? selectedYear)
        {
            // שליפת כל הנתונים מהמסד
            var activePortfolio = await _context.Portfolios
              .Where(p => p.IsActive) // הצגת מניות רק אם הן פעילות
              .ToListAsync();

            // חישוב סך ההשקעה ותשואת התיק
            decimal totalInvestment = activePortfolio.Sum(p => p.Investment);
            decimal portfolioReturns = activePortfolio.Sum(p => p.ProfitLoss);

            //// חישוב המניות הרווחיות ביותר (Top 10 Earning Stocks)
            //var topEarningStocks = activePortfolio
            //    .Where(p => p.ProfitLoss > 0) // רק מניות רווחיות
            //    .OrderByDescending(p => p.ProfitLoss)
            //    .Take(10)
            //    .ToList();

            //// חישוב המניות המפסידות ביותר (Bottom 10 Losing Stocks)
            //var bottomLosingStocks = activePortfolio
            //    .Where(p => p.ProfitLoss < 0) // רק מניות עם הפסדים
            //    .OrderBy(p => p.ProfitLoss)
            //    .Take(10)
            //    .ToList();

            //// העברת הנתונים ל-ViewBag עבור התצוגה
            //ViewBag.TopEarningStockNames = topEarningStocks.Select(s => s.Stock).ToList();
            //ViewBag.TopEarningStockProfits = topEarningStocks.Select(s => s.ProfitLoss).ToList();

            //ViewBag.BottomLosingStockNames = bottomLosingStocks.Select(s => s.Stock).ToList();
            //ViewBag.BottomLosingStockLosses = bottomLosingStocks.Select(s => Math.Abs(s.ProfitLoss)).ToList();

            // קבלת שנים ייחודיות מתוך הנתונים
            ViewBag.Years = activePortfolio.Select(p => p.DateAdded.Year).Distinct().OrderByDescending(y => y).ToList();
            ViewBag.SelectedYear = selectedYear;

            // קיבוץ נתונים לפי חודשים ושנים (לגרף קו)
            var groupedData = activePortfolio
                .Where(p => !selectedYear.HasValue || p.DateAdded.Year == selectedYear.Value) // סינון לפי שנה אם נבחרה
                .OrderBy(p => p.DateAdded) // מיון לפי תאריך
                .GroupBy(p => new { p.DateAdded.Year, p.DateAdded.Month }) // קיבוץ לפי שנה וחודש
                .Select(g => new
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1), // יצירת אובייקט תאריך
                    MonthlyReturn = g.Sum(p => p.ProfitLoss) / (g.Sum(p => p.Investment) == 0 ? 1 : g.Sum(p => p.Investment)) * 100
                })
                .OrderBy(g => g.Date) // מיון לפי תאריך
                .ToList();

            // שליחת נתונים ל-ViewBag עבור הצגת גרף
            ViewBag.Months = groupedData.Select(g => g.Date.ToString("MM/yyyy")).ToList();
            ViewBag.MonthlyReturns = groupedData.Select(g => g.MonthlyReturn).ToList();

            //// חישוב אחוז ההצלחה האבסולוטי
            //int totalTrades = activePortfolio.Count; // כמות עסקאות כוללת
            //int successfulTrades = activePortfolio.Count(p => p.ProfitLoss > 0); // כמות עסקאות רווחיות
            //decimal absoluteSuccessRate = totalTrades > 0 ? (decimal)successfulTrades / totalTrades * 100 : 0;

            //ViewBag.AbsoluteSuccessRate = absoluteSuccessRate;

            // שליחת תשואות שוק
            ViewBag.MarketReturns = marketReturns
                .OrderBy(r => r.Year)
                .ThenBy(r => r.Month)
                .Select(r => r.ReturnPercentage)
                .ToList();

            // שליחת הנתונים ל-View
            return View(activePortfolio);
        }


        [HttpPost]
        public async Task<ActionResult> SellStock(int id, double sellQuantity, decimal sellPrice)
        {
            using (var context = new TradingJournalContext())
            {
                // שליפת המניה לפי ID
                var stock = await context.Portfolios.FindAsync(id);
                if (stock == null)
                {
                    return HttpNotFound();
                }

                // עדכון הכמות שנמכרה
                stock.Sold += sellQuantity;

                // חישוב הכמות שנותרה
                double remainingQuantity = stock.FirstQuantity - stock.Sold;

                // עדכון השקעה שנותרה (Investment)
                stock.Investment = (decimal)remainingQuantity * stock.PurchasePrice;

                // חישוב הרווח/הפסד מהמכירה
                decimal saleProfit = (decimal)sellQuantity * sellPrice; // הכנסות ממכירה
                decimal costBasis = (decimal)sellQuantity * stock.PurchasePrice; // עלות מקורית של הכמות שנמכרה
                stock.ProfitLoss = ((decimal)remainingQuantity * stock.Price) - stock.Investment;

                stock.Quantity = remainingQuantity;
                // אם מכרת הכל, הפוך את המניה ללא פעילה
                if (stock.Sold >= stock.FirstQuantity)
                {
                    stock.IsActive = false;
                    stock.IsDeleted = true;
                }

                // שמירת השינויים למסד הנתונים
                await context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
        }





        public decimal CalculateTotalMarketPerformance(List<decimal> marketReturns)
        {
            if (marketReturns == null || !marketReturns.Any())
            {
                return 0; // אם אין נתונים, מחזירים 0
            }

            decimal totalPerformance = 1; // מתחילים מ-1 כדי לייצג את הערך ההתחלתי (100%)

            foreach (var monthlyReturn in marketReturns)
            {
                totalPerformance *= (1 + (monthlyReturn / 100)); // מוסיפים את התשואה של כל חודש ומכפילים
            }

            totalPerformance = (totalPerformance - 1) * 100; // מורידים 1 ומחזירים באחוזים
            return totalPerformance;
        }




        [HttpPost]
        public ActionResult AddMarketReturn(int month, int year, decimal returnPercentage)
        {
            // בדיקה אם הערך כבר קיים ברשימת תשואות המדד
            var existingReturn = marketReturns.FirstOrDefault(r => r.Month == month && r.Year == year);
            if (existingReturn != null)
            {
                existingReturn.ReturnPercentage = returnPercentage; // עדכון תשואה קיימת
            }
            else
            {
                marketReturns.Add(new MarketReturn
                {
                    Month = month,
                    Year = year,
                    ReturnPercentage = returnPercentage
                });
            }

            // חזרה לדף הראשי
            return RedirectToAction("Index");
        }


        public ActionResult Graphs()
        {
            return View();
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
            using (var context = new TradingJournalContext())
            {
                // שליפת כל הפריטים מהמסד (פעילים ולא פעילים)
                var portfolioItems = await context.Portfolios.ToListAsync();

                // עדכון אחוזי הרווח/הפסד לכל פריט (אם צריך)
                foreach (var item in portfolioItems)
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

                // סינון לפי התקופה המבוקשת (אם המשתמש בחר תאריכים)
                var filteredHistory = portfolioItems.AsQueryable();
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
                var newStock = new PortfolioItem
                {
                    ID = portfolioHistory.Any() ? portfolioHistory.Max(p => p.ID) + 1 : 1, // ID ייחודי

                    Stock = stockData.Stock,
                    Quantity = quantity,
                    FirstQuantity = quantity,
                    PurchasePrice = purchasePrice,
                    Investment = investment,
                    Price = stockData.Price, // מחיר המניה הנוכחי
                    ChangePercentage = stockData.ChangePercentage, // שינוי יומי באחוזים
                    ProfitLoss = profitLoss, // רווח/הפסד
                    ProfitLossPercentage = profitLossPercentage, // רווח/הפסד באחוזים
                    DateAdded = dateAdded, // התאריך שסופק
                    IsActive = false, // הופך ללא פעיל מיד
                    IsDeleted = true // הגדרת השדה החדש
                };

                using (var context = new TradingJournalContext())
                {
                    context.Portfolios.Add(newStock); // הוספה לטבלה
                    await context.SaveChangesAsync();     // שמירה למסד הנתונים
                }

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
                    return View("Index");
                }

                // חישוב כמות המניות ורווח/הפסד
                double quantity = (double)(investment / purchasePrice);
                decimal profitLoss = ((decimal)quantity * stockData.Price) - investment;

                // יצירת פריט מניה חדש
                var newStock = new PortfolioItem
                {
                    Stock = stockData.Stock,
                    Quantity = quantity,
                    FirstQuantity = quantity,
                    PurchasePrice = purchasePrice,
                    Investment = investment,
                    Price = stockData.Price,
                    ChangePercentage = stockData.ChangePercentage,
                    ProfitLoss = profitLoss,
                    ProfitLossPercentage = (profitLoss / investment) * 100,
                    DateAdded = dateAdded, // הגדרת התאריך שהמשתמש סיפק
                    IsActive = true
                };

                // שמירת פריט המניה במסד הנתונים
                using (var context = new TradingJournalContext())
                {
                    context.Portfolios.Add(newStock); // הוספה לטבלה
                    await context.SaveChangesAsync();     // שמירה למסד הנתונים
                }

                return RedirectToAction("Index"); // חזרה לעמוד הראשי
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred: " + ex.Message;
                return View("Index");
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

       
        public async Task<JsonResult> GetSP500MonthlyReturns()
        {
            try
            {
                string apiKey = "cuav1s1r01qof06jg10gcuav1s1r01qof06jg110"; // שים כאן את ה-API KEY שלך
                string symbol = "^GSPC"; // סמל המדד S&P 500
                long fromDate = GetUnixTime(DateTime.Now.AddYears(-1)); // 12 חודשים אחורה
                long toDate = GetUnixTime(DateTime.Now); // עד היום
                string apiUrl = $"https://finnhub.io/api/v1/stock/candle?symbol={symbol}&resolution=M&from={fromDate}&to={toDate}&token={apiKey}";

                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync(apiUrl);
                    var data = JsonConvert.DeserializeObject<dynamic>(response);

                    if (data == null || data["c"] == null)
                    {
                        return Json(new { error = "No data found for S&P 500" }, JsonRequestBehavior.AllowGet);
                    }

                    var closingPrices = ((IEnumerable<dynamic>)data["c"]).Select(price => (decimal)price).ToList();

                    // חישוב תשואות חודשיות
                    var returns = new List<decimal>();
                    for (int i = 1; i < closingPrices.Count; i++)
                    {
                        decimal monthlyReturn = ((closingPrices[i] - closingPrices[i - 1]) / closingPrices[i - 1]) * 100;
                        returns.Add(monthlyReturn);
                    }

                    // יצירת רשימת חודשים
                    var months = Enumerable.Range(0, returns.Count)
                        .Select(i => DateTime.Now.AddMonths(-1 * (returns.Count - 1 - i)).ToString("MMMM yyyy"))
                        .ToList();

                    return Json(new { months, returns }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

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
