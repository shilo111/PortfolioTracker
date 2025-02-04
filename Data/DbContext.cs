using PortfolioTracker.Models;
using System.Data.Entity;

public class TradingJournalContext : DbContext
{
    public TradingJournalContext() : base("name=TradingJournalDB") { }

    public DbSet<PortfolioItem> Portfolios { get; set; }
}
