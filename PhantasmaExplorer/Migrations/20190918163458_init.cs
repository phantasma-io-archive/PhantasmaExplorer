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
                    Name = table.Column<string>(nullable: true),
                    SoulStaked = table.Column<string>(nullable: true)
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
                    Height = table.Column<uint>(nullable: false),
                    Contracts = table.Column<string>(nullable: true)
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
                    Decimals = table.Column<uint>(nullable: false),
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
                    Amount = table.Column<string>(nullable: false),
                    TokenSymbol = table.Column<string>(nullable: false),
                    Chain = table.Column<string>(nullable: false),
                    Address = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FBalance", x => new { x.Address, x.Chain, x.TokenSymbol, x.Amount });
                    table.ForeignKey(
                        name: "FK_FBalance_Accounts_Address",
                        column: x => x.Address,
                        principalTable: "Accounts",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    Hash = table.Column<string>(nullable: false),
                    ChainAddress = table.Column<string>(nullable: true),
                    ChainName = table.Column<string>(nullable: true),
                    PreviousHash = table.Column<string>(nullable: true),
                    Timestamp = table.Column<uint>(nullable: false),
                    Height = table.Column<uint>(nullable: false),
                    Payload = table.Column<string>(nullable: true),
                    ValidatorAddress = table.Column<string>(nullable: true),
                    Reward = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Hash);
                    table.ForeignKey(
                        name: "FK_Blocks_Chains",
                        column: x => x.ChainAddress,
                        principalTable: "Chains",
                        principalColumn: "Address",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TokenMetadata",
                columns: table => new
                {
                    Key = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: false),
                    Symbol = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenMetadata", x => new { x.Symbol, x.Key, x.Value });
                    table.ForeignKey(
                        name: "FK_TokenMetadata_Tokens_Symbol",
                        column: x => x.Symbol,
                        principalTable: "Tokens",
                        principalColumn: "Symbol",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Hash = table.Column<string>(nullable: false),
                    BlockHash = table.Column<string>(nullable: true),
                    Timestamp = table.Column<uint>(nullable: false),
                    Script = table.Column<string>(nullable: true),
                    Result = table.Column<string>(nullable: true),
                    TokenSymbol = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Hash);
                    table.ForeignKey(
                        name: "FK_Transactions_Blocks",
                        column: x => x.BlockHash,
                        principalTable: "Blocks",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Tokens_TokenSymbol",
                        column: x => x.TokenSymbol,
                        principalTable: "Tokens",
                        principalColumn: "Symbol",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    EventAddress = table.Column<string>(nullable: false),
                    Data = table.Column<string>(nullable: false),
                    EventKind = table.Column<int>(nullable: false),
                    Hash = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => new { x.Hash, x.Data, x.EventAddress, x.EventKind });
                    table.ForeignKey(
                        name: "FK_Event_Transactions_Hash",
                        column: x => x.Hash,
                        principalTable: "Transactions",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransaction_TransactionId",
                table: "AccountTransaction",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_ChainAddress",
                table: "Blocks",
                column: "ChainAddress");

            migrationBuilder.CreateIndex(
                name: "IX_NonFungibleTokens_AccountAddress",
                table: "NonFungibleTokens",
                column: "AccountAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BlockHash",
                table: "Transactions",
                column: "BlockHash");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TokenSymbol",
                table: "Transactions",
                column: "TokenSymbol");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountTransaction");

            migrationBuilder.DropTable(
                name: "Apps");

            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropTable(
                name: "FBalance");

            migrationBuilder.DropTable(
                name: "NonFungibleTokens");

            migrationBuilder.DropTable(
                name: "TokenMetadata");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "Tokens");

            migrationBuilder.DropTable(
                name: "Chains");
        }
    }
}
