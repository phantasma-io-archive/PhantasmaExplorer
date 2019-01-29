using Microsoft.EntityFrameworkCore.Migrations;

namespace Phantasma.Explorer.Migrations
{
    public partial class AddedinfofieldstoNFT : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DetailsUrl",
                table: "NonFungibleTokens",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ViewerUrl",
                table: "NonFungibleTokens",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DetailsUrl",
                table: "NonFungibleTokens");

            migrationBuilder.DropColumn(
                name: "ViewerUrl",
                table: "NonFungibleTokens");
        }
    }
}
