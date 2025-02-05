namespace PortfolioTracker.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSoldColumn : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PortfolioItems", "Sold", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PortfolioItems", "Sold");
        }
    }
}
