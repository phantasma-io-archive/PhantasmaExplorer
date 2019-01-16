using Microsoft.EntityFrameworkCore.Migrations;

namespace Phantasma.Explorer.Migrations
{
    public partial class AddedChainNametoblock : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChainName",
                table: "Blocks",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChainName",
                table: "Blocks");
        }
    }
}
