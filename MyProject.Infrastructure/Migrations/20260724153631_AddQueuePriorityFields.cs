using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQueuePriorityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInTime",
                table: "Appointments",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsWalkIn",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "QueuePriorityTime",
                table: "Appointments",
                type: "datetime",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckInTime",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "IsWalkIn",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "QueuePriorityTime",
                table: "Appointments");
        }
    }
}
