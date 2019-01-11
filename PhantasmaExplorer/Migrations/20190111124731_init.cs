using Microsoft.EntityFrameworkCore.Migrations;

namespace Phantasma.Explorer.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Address = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Address);
                });

            migrationBuilder.CreateTable(
                name: "Apps",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    Icon = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Chains",
                columns: table => new
                {
                    Address = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    ParentAddress = table.Column<string>(nullable: true),
                    Height = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chains", x => x.Address);
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Symbol = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Decimals = table.Column<long>(nullable: false),
                    Fungible = table.Column<bool>(nullable: false),
                    CurrentSupply = table.Column<string>(nullable: true),
                    MaxSupply = table.Column<string>(nullable: true),
                    OwnerAddress = table.Column<string>(nullable: true),
                    Flags = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Symbol);
                });

            migrationBuilder.CreateTable(
                name: "FBalance",
                columns: table => new
                {
                    Address = table.Column<string>(nullable: false),
                    Id = table.Column<int>(nullable: false),
                    Amount = table.Column<string>(nullable: true),
                    TokenSymbol = table.Column<string>(nullable: true),
                    Chain = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FBalance", x => new { x.Address, x.Id });
                    table.ForeignKey(
                        name: "FK_FBalance_Accounts_Address",
                        column: x => x.Address,
                        principalTable: "Accounts",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NfBalances",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    TokenSymbol = table.Column<string>(nullable: true),
                    Chain = table.Column<string>(nullable: true),
                    AccountAddress = table.Column<string>(nullable: true)
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

            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    Hash = table.Column<string>(nullable: false),
                    ChainAddress = table.Column<string>(nullable: true),
                    PreviousHash = table.Column<string>(nullable: true),
                    Timestamp = table.Column<long>(nullable: false),
                    Height = table.Column<long>(nullable: false),
                    Payload = table.Column<string>(nullable: true),
                    ValidatorAddress = table.Column<string>(nullable: true),
                    Reward = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Hash);
                    table.ForeignKey(
                        name: "FK_Blocks_Chains_ChainAddress",
                        column: x => x.ChainAddress,
                        principalTable: "Chains",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Hash = table.Column<string>(nullable: false),
                    Timestamp = table.Column<long>(nullable: false),
                    Script = table.Column<string>(nullable: true),
                    ChainAddress = table.Column<string>(nullable: true),
                    AccountAddress = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Hash);
                    table.ForeignKey(
                        name: "FK_Transactions_Accounts_AccountAddress",
                        column: x => x.AccountAddress,
                        principalTable: "Accounts",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Chains_ChainAddress",
                        column: x => x.ChainAddress,
                        principalTable: "Chains",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Blocks_Hash",
                        column: x => x.Hash,
                        principalTable: "Blocks",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    Hash = table.Column<string>(nullable: false),
                    Id = table.Column<int>(nullable: false),
                    EventAddress = table.Column<string>(nullable: true),
                    Data = table.Column<string>(nullable: true),
                    EventKind = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => new { x.Hash, x.Id });
                    table.ForeignKey(
                        name: "FK_Event_Transactions_Hash",
                        column: x => x.Hash,
                        principalTable: "Transactions",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_ChainAddress",
                table: "Blocks",
                column: "ChainAddress");

            migrationBuilder.CreateIndex(
                name: "IX_NfBalances_AccountAddress",
                table: "NfBalances",
                column: "AccountAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountAddress",
                table: "Transactions",
                column: "AccountAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ChainAddress",
                table: "Transactions",
                column: "ChainAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Apps");

            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropTable(
                name: "FBalance");

            migrationBuilder.DropTable(
                name: "NfBalances");

            migrationBuilder.DropTable(
                name: "Tokens");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "Chains");
        }
    }
}
