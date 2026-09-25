using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConstructionBudgeting.Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sales");

            migrationBuilder.CreateTable(
                name: "Quotes",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    TotalSalesAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "EUR")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.CheckConstraint("CK_Quotes_Currency", "\"Currency\" = 'EUR'");
                    table.CheckConstraint("CK_Quotes_Total", "\"TotalSalesAmount\" >= 0 AND \"TotalSalesAmount\" = round(\"TotalSalesAmount\", 2)");
                    table.CheckConstraint("CK_Quotes_Version", "\"Version\" >= 1");
                });

            migrationBuilder.CreateTable(
                name: "QuoteLines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkItemCode = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    SalesUnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    LineAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteLines", x => new { x.QuoteId, x.Id });
                    table.CheckConstraint("CK_QuoteLines_Amount", "\"LineAmount\" >= 0 AND \"LineAmount\" = round(\"LineAmount\", 2)");
                    table.CheckConstraint("CK_QuoteLines_Position", "\"Position\" >= 0");
                    table.CheckConstraint("CK_QuoteLines_Price", "\"SalesUnitPrice\" >= 0");
                    table.CheckConstraint("CK_QuoteLines_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_QuoteLines_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "sales",
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteLines_QuoteId_Position",
                schema: "sales",
                table: "QuoteLines",
                columns: new[] { "QuoteId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_ProjectId",
                schema: "sales",
                table: "Quotes",
                column: "ProjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuoteLines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "Quotes",
                schema: "sales");
        }
    }
}
