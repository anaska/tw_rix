namespace login.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Searches",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SearchDate = c.DateTime(nullable: false),
                        SearchTerm = c.String(),
                        SearchFrequency = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Searches");
        }
    }
}
