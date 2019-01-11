using Microsoft.EntityFrameworkCore.Migrations;

namespace Phantasma.Explorer.Migrations
{
    public partial class fix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Blocks_Chains_ChainAddress",
                table: "Blocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Chains_ChainAddress",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Blocks_Hash",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FBalance",
                table: "FBalance");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Event",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "FBalance");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Event");

            migrationBuilder.RenameColumn(
                name: "ChainAddress",
                table: "Transactions",
                newName: "BlockHash");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_ChainAddress",
                table: "Transactions",
                newName: "IX_Transactions_BlockHash");

            migrationBuilder.AlterColumn<string>(
                name: "TokenSymbol",
                table: "FBalance",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Chain",
                table: "FBalance",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Amount",
                table: "FBalance",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventAddress",
                table: "Event",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "Event",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FBalance",
                table: "FBalance",
                columns: new[] { "Address", "Chain", "TokenSymbol", "Amount" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Event",
                table: "Event",
                columns: new[] { "Hash", "Data", "EventAddress", "EventKind" });

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_Chains",
                table: "Blocks",
                column: "ChainAddress",
                principalTable: "Chains",
                principalColumn: "Address",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Blocks",
                table: "Transactions",
                column: "BlockHash",
                principalTable: "Blocks",
                principalColumn: "Hash",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Blocks_Chains",
                table: "Blocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Blocks",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FBalance",
                table: "FBalance");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Event",
                table: "Event");

            migrationBuilder.RenameColumn(
                name: "BlockHash",
                table: "Transactions",
                newName: "ChainAddress");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_BlockHash",
                table: "Transactions",
                newName: "IX_Transactions_ChainAddress");

            migrationBuilder.AlterColumn<string>(
                name: "Amount",
                table: "FBalance",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "TokenSymbol",
                table: "FBalance",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "Chain",
                table: "FBalance",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "FBalance",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "EventAddress",
                table: "Event",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "Event",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Event",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FBalance",
                table: "FBalance",
                columns: new[] { "Address", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Event",
                table: "Event",
                columns: new[] { "Hash", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_Chains_ChainAddress",
                table: "Blocks",
                column: "ChainAddress",
                principalTable: "Chains",
                principalColumn: "Address",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Chains_ChainAddress",
                table: "Transactions",
                column: "ChainAddress",
                principalTable: "Chains",
                principalColumn: "Address",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Blocks_Hash",
                table: "Transactions",
                column: "Hash",
                principalTable: "Blocks",
                principalColumn: "Hash",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
