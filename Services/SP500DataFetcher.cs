//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Threading.Tasks;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;

//namespace PortfolioTracker.Services // או שם namespace המתאים לפרויקט שלך
//{
//    public class SP500DataFetcher
//    {
//        private const string ApiKey = "TY8WL7BS8F85U9SC"; // החלף במפתח ה-API שלך
//        private const string ApiUrl = "https://www.alphavantage.co/query?function=TIME_SERIES_MONTHLY&symbol=SPY&apikey=" + ApiKey;

//        public async Task<List<decimal>> FetchMonthlyReturns()
//        {
//            using (HttpClient client = new HttpClient())
//            {
//                // שליחת בקשה ל-API
//                var response = await client.GetAsync(ApiUrl);
//                response.EnsureSuccessStatusCode();

//                // קריאת התגובה
//                var content = await response.Content.ReadAsStringAsync();
//                dynamic data = JsonConvert.DeserializeObject(content);

//                // נתוני התשואות החודשיות
//                var timeSeries = data["Monthly Time Series"];
//                List<decimal> monthlyReturns = new List<decimal>();
//                decimal previousClose = 0;

//                foreach (var entry in timeSeries)
//                {
//                    // בדוק אם הנתון קיים
//                    if (entry.Value.TryGetValue("4. close", out JToken closePriceToken) && closePriceToken != null)
//                    {
//                        decimal closePrice = closePriceToken.Value<decimal>();

//                        // אם יש ערך קודם, חישוב תשואה חודשית
//                        if (previousClose != 0)
//                        {
//                            decimal monthlyReturn = ((closePrice - previousClose) / previousClose) * 100;
//                            monthlyReturns.Add(monthlyReturn);
//                        }

//                        // עדכון ערך סגירה קודם
//                        previousClose = closePrice;
//                    }
//                    else
//                    {
//                        // במקרה שאין נתון, ניתן להוסיף טיפול מותאם אישית (למשל, לדלג)
//                        monthlyReturns.Add(0); // הוספת 0 במקרה של נתון חסר
//                    }
//                }



//                return monthlyReturns;
//            }
//        }
//    }
//}
