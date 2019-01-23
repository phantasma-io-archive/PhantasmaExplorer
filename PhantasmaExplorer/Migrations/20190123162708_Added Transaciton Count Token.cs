using Microsoft.EntityFrameworkCore.Migrations;

namespace Phantasma.Explorer.Migrations
{
    public partial class AddedTransacitonCountToken : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TransactionCount",
                table: "Tokens",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionCount",
                table: "Tokens");
        }
    }
}
