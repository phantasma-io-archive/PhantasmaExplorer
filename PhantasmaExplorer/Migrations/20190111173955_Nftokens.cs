using Microsoft.EntityFrameworkCore.Migrations;

namespace Phantasma.Explorer.Migrations
{
    public partial class Nftokens : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NfBalances");

            migrationBuilder.CreateTable(
                name: "NonFungibleTokens",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Chain = table.Column<string>(nullable: true),
                    TokenSymbol = table.Column<string>(nullable: true),
                    AccountAddress = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonFungibleTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NonFungibleTokens_Accounts_AccountAddress",
                        column: x => x.AccountAddress,
                        principalTable: "Accounts",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NonFungibleTokens_AccountAddress",
                table: "NonFungibleTokens",
                column: "AccountAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NonFungibleTokens");

            migrationBuilder.CreateTable(
                name: "NfBalances",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    AccountAddress = table.Column<string>(nullable: true),
                    Chain = table.Column<string>(nullable: true),
                    TokenSymbol = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NfBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NfBalances_Accounts_AccountAddress",
                        column: x => x.AccountAddress,
                        principalTable: "Accounts",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NfBalances_AccountAddress",
                table: "NfBalances",
                column: "AccountAddress");
        }
    }
}
