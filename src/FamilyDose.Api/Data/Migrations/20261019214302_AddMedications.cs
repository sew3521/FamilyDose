using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyDose.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMedications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChildId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    DoseAmount = table.Column<string>(type: "TEXT", nullable: false),
                    DoseUnit = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Concentration = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    MinimumIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxDosesPer24Hours = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduleMode = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferenceUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medications_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_ChildId",
                table: "Medications",
                column: "ChildId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Medications");
        }
    }
}
