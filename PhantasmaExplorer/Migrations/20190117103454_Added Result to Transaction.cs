using Microsoft.EntityFrameworkCore.Migrations;

namespace Phantasma.Explorer.Migrations
{
    public partial class AddedResulttoTransaction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "Transactions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Result",
                table: "Transactions");
        }
    }
}
