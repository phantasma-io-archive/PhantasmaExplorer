using Microsoft.EntityFrameworkCore.Migrations;

namespace Phantasma.Explorer.Migrations
{
    public partial class AccountTransactions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Accounts_AccountAddress",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountAddress",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AccountAddress",
                table: "Transactions");

            migrationBuilder.CreateTable(
                name: "AccountTransaction",
                columns: table => new
                {
                    AccountId = table.Column<string>(nullable: false),
                    TransactionId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTransaction", x => new { x.AccountId, x.TransactionId });
                    table.ForeignKey(
                        name: "FK_AccountTransaction_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountTransaction_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransaction_TransactionId",
                table: "AccountTransaction",
                column: "TransactionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountTransaction");

            migrationBuilder.AddColumn<string>(
                name: "AccountAddress",
                table: "Transactions",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountAddress",
                table: "Transactions",
                column: "AccountAddress");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Accounts_AccountAddress",
                table: "Transactions",
                column: "AccountAddress",
                principalTable: "Accounts",
                principalColumn: "Address",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
