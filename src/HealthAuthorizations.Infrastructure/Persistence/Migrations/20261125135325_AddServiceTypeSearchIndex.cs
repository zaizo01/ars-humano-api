using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthAuthorizations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceTypeSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AutorizacionesDetalle_ServiceTypeId",
                table: "AutorizacionesDetalle");

            migrationBuilder.CreateIndex(
                name: "IX_AutorizacionesDetalle_ServiceTypeId_AuthorizationId",
                table: "AutorizacionesDetalle",
                columns: new[] { "ServiceTypeId", "AuthorizationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AutorizacionesDetalle_ServiceTypeId_AuthorizationId",
                table: "AutorizacionesDetalle");

            migrationBuilder.CreateIndex(
                name: "IX_AutorizacionesDetalle_ServiceTypeId",
                table: "AutorizacionesDetalle",
                column: "ServiceTypeId");
        }
    }
}
