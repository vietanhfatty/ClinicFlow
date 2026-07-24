using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentIdToPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_AppointmentId",
                table: "Payments",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Appointments",
                table: "Payments",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "AppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Appointments",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_AppointmentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "Payments");
        }
    }
}
