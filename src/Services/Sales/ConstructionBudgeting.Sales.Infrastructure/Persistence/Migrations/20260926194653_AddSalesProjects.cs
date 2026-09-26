using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConstructionBudgeting.Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.CheckConstraint("CK_Projects_Name", "length(btrim(\"Name\")) > 0");
                });

            // 为已有报价补齐项目目录，再建立外键；不覆盖报价或猜测真实项目名称。
            migrationBuilder.Sql(@"
                INSERT INTO sales.""Projects"" (""Id"", ""Name"")
                SELECT DISTINCT ""ProjectId"",
                    CASE WHEN ""ProjectId"" = '11111111-1111-1111-1111-111111111111'::uuid
                        THEN '示例施工项目'
                        ELSE '已有关联项目 ' || ""ProjectId""::text END
                FROM sales.""Quotes"";");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Projects_ProjectId",
                schema: "sales",
                table: "Quotes",
                column: "ProjectId",
                principalSchema: "sales",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Projects_ProjectId",
                schema: "sales",
                table: "Quotes");

            migrationBuilder.DropTable(
                name: "Projects",
                schema: "sales");
        }
    }
}
