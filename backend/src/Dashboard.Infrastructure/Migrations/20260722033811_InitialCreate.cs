using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "monthly_snapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "metric_definitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EvaluationStrategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EvaluationConfig = table.Column<string>(type: "jsonb", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_metric_definitions_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "metric_snapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MetricDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    MonthlySnapshotId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_metric_snapshots_metric_definitions_MetricDefinitionId",
                        column: x => x.MetricDefinitionId,
                        principalTable: "metric_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_metric_snapshots_monthly_snapshots_MonthlySnapshotId",
                        column: x => x.MonthlySnapshotId,
                        principalTable: "monthly_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_metric_definitions_CategoryId",
                table: "metric_definitions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_metric_snapshots_MetricDefinitionId_MonthlySnapshotId",
                table: "metric_snapshots",
                columns: new[] { "MetricDefinitionId", "MonthlySnapshotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_metric_snapshots_MonthlySnapshotId",
                table: "metric_snapshots",
                column: "MonthlySnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_monthly_snapshots_Month",
                table: "monthly_snapshots",
                column: "Month",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "metric_snapshots");

            migrationBuilder.DropTable(
                name: "metric_definitions");

            migrationBuilder.DropTable(
                name: "monthly_snapshots");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
