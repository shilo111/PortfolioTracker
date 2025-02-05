namespace PortfolioTracker.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddFirstQuantityColumn : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PortfolioItems", "FirstQuantity", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PortfolioItems", "FirstQuantity");
        }
    }
}
