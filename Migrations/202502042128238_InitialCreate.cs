namespace PortfolioTracker.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PortfolioItems",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Stock = c.String(),
                        Quantity = c.Double(nullable: false),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ChangePercentage = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PurchasePrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Investment = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ProfitLoss = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ProfitLossPercentage = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DateAdded = c.DateTime(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.PortfolioItems");
        }
    }
}
