using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HealthAuthorizations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "AutorizacionesNumeroSeq");

            migrationBuilder.CreateTable(
                name: "Intermediarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intermediarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposPrestadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposPrestadores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposServicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposServicios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contratantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IntermediaryId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contratantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contratantes_Intermediarios_IntermediaryId",
                        column: x => x.IntermediaryId,
                        principalTable: "Intermediarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Prestadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProviderTypeId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prestadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prestadores_TiposPrestadores_ProviderTypeId",
                        column: x => x.ProviderTypeId,
                        principalTable: "TiposPrestadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Afiliados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AffiliateNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ContractorId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Afiliados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Afiliados_Contratantes_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "Contratantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Autorizaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('\"AutorizacionesNumeroSeq\"')"),
                    AffiliateId = table.Column<int>(type: "integer", nullable: false),
                    ProviderId = table.Column<int>(type: "integer", nullable: false),
                    ContractorId = table.Column<int>(type: "integer", nullable: false),
                    IntermediaryId = table.Column<int>(type: "integer", nullable: true),
                    ServiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    TotalClaimed = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCovered = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCopay = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    DeactivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Autorizaciones", x => x.Id);
                    table.CheckConstraint("CK_Autorizaciones_Montos_Cuadran", "\"TotalCovered\" + \"TotalCopay\" = \"TotalClaimed\"");
                    table.CheckConstraint("CK_Autorizaciones_Montos_NoNegativos", "\"TotalClaimed\" >= 0 AND \"TotalCovered\" >= 0 AND \"TotalCopay\" >= 0");
                    table.ForeignKey(
                        name: "FK_Autorizaciones_Afiliados_AffiliateId",
                        column: x => x.AffiliateId,
                        principalTable: "Afiliados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Autorizaciones_Contratantes_ContractorId",
                        column: x => x.ContractorId,
                        principalTable: "Contratantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Autorizaciones_Intermediarios_IntermediaryId",
                        column: x => x.IntermediaryId,
                        principalTable: "Intermediarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Autorizaciones_Prestadores_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Prestadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutorizacionesDetalle",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    AuthorizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceTypeId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ClaimedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CoveredAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CopayAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutorizacionesDetalle", x => x.Id);
                    table.CheckConstraint("CK_AutorizacionesDetalle_MontoReclamado_Positivo", "\"ClaimedAmount\" > 0");
                    table.CheckConstraint("CK_AutorizacionesDetalle_Montos_Cuadran", "\"CoveredAmount\" + \"CopayAmount\" = \"ClaimedAmount\"");
                    table.CheckConstraint("CK_AutorizacionesDetalle_Montos_NoNegativos", "\"CoveredAmount\" >= 0 AND \"CopayAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_AutorizacionesDetalle_Autorizaciones_AuthorizationId",
                        column: x => x.AuthorizationId,
                        principalTable: "Autorizaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AutorizacionesDetalle_TiposServicios_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "TiposServicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Contratantes",
                columns: new[] { "Id", "Code", "IntermediaryId", "IsActive", "Name", "TaxId" },
                values: new object[] { 3, "CTR-003", null, true, "Plan Individual", "101000003" });

            migrationBuilder.InsertData(
                table: "Intermediarios",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "INT-001", true, "Corredora de Seguros Caribe" },
                    { 2, "INT-002", true, "Asesores Unidos" }
                });

            migrationBuilder.InsertData(
                table: "TiposPrestadores",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { 1, "MED", "Médico" },
                    { 2, "FAR", "Farmacia" },
                    { 3, "CLI", "Clínica" },
                    { 4, "LAB", "Laboratorio" }
                });

            migrationBuilder.InsertData(
                table: "TiposServicios",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "CON", true, "Consulta" },
                    { 2, "EME", true, "Emergencia" },
                    { 3, "HOS", true, "Hospitalización" },
                    { 4, "LAB", true, "Laboratorio" },
                    { 5, "IMG", true, "Imágenes" }
                });

            migrationBuilder.InsertData(
                table: "Afiliados",
                columns: new[] { "Id", "AffiliateNumber", "BirthDate", "ContractorId", "DocumentNumber", "FirstName", "IsActive", "LastName" },
                values: new object[] { 4, "AF-0004", new DateOnly(2001, 6, 30), 3, "00100000004", "Luis", true, "Gómez" });

            migrationBuilder.InsertData(
                table: "Contratantes",
                columns: new[] { "Id", "Code", "IntermediaryId", "IsActive", "Name", "TaxId" },
                values: new object[,]
                {
                    { 1, "CTR-001", 1, true, "Industrias del Norte SRL", "101000001" },
                    { 2, "CTR-002", 2, true, "Comercial Las Américas SA", "101000002" }
                });

            migrationBuilder.InsertData(
                table: "Prestadores",
                columns: new[] { "Id", "Code", "IsActive", "Name", "ProviderTypeId", "TaxId" },
                values: new object[,]
                {
                    { 1, "PRE-001", true, "Dr. Roberto Castillo", 1, "00200000001" },
                    { 2, "PRE-002", true, "Farmacia Central", 2, "130000001" },
                    { 3, "PRE-003", true, "Clínica Santa Lucía", 3, "130000002" },
                    { 4, "PRE-004", true, "Laboratorio Referencia", 4, "130000003" },
                    { 5, "PRE-005", true, "Centro Médico del Este", 3, "130000004" }
                });

            migrationBuilder.InsertData(
                table: "Afiliados",
                columns: new[] { "Id", "AffiliateNumber", "BirthDate", "ContractorId", "DocumentNumber", "FirstName", "IsActive", "LastName" },
                values: new object[,]
                {
                    { 1, "AF-0001", new DateOnly(1985, 4, 12), 1, "00100000001", "María", true, "Pérez" },
                    { 2, "AF-0002", new DateOnly(1990, 9, 3), 1, "00100000002", "Juan", true, "Rodríguez" },
                    { 3, "AF-0003", new DateOnly(1978, 1, 25), 2, "00100000003", "Ana", true, "Martínez" },
                    { 5, "AF-0005", new DateOnly(1965, 11, 8), 2, "00100000005", "Carmen", false, "Santos" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Afiliados_AffiliateNumber",
                table: "Afiliados",
                column: "AffiliateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Afiliados_ContractorId",
                table: "Afiliados",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_Afiliados_DocumentNumber",
                table: "Afiliados",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Autorizaciones_AffiliateId_ServiceDate",
                table: "Autorizaciones",
                columns: new[] { "AffiliateId", "ServiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Autorizaciones_ContractorId",
                table: "Autorizaciones",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizaciones_IdempotencyKey",
                table: "Autorizaciones",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizaciones_IntermediaryId",
                table: "Autorizaciones",
                column: "IntermediaryId");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizaciones_Number",
                table: "Autorizaciones",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Autorizaciones_ProviderId_ServiceDate",
                table: "Autorizaciones",
                columns: new[] { "ProviderId", "ServiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Autorizaciones_Status_ServiceDate",
                table: "Autorizaciones",
                columns: new[] { "Status", "ServiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AutorizacionesDetalle_AuthorizationId",
                table: "AutorizacionesDetalle",
                column: "AuthorizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AutorizacionesDetalle_ServiceTypeId",
                table: "AutorizacionesDetalle",
                column: "ServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratantes_Code",
                table: "Contratantes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contratantes_IntermediaryId",
                table: "Contratantes",
                column: "IntermediaryId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratantes_TaxId",
                table: "Contratantes",
                column: "TaxId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Intermediarios_Code",
                table: "Intermediarios",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prestadores_Code",
                table: "Prestadores",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prestadores_ProviderTypeId",
                table: "Prestadores",
                column: "ProviderTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposPrestadores_Code",
                table: "TiposPrestadores",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposServicios_Code",
                table: "TiposServicios",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutorizacionesDetalle");

            migrationBuilder.DropTable(
                name: "Autorizaciones");

            migrationBuilder.DropTable(
                name: "TiposServicios");

            migrationBuilder.DropTable(
                name: "Afiliados");

            migrationBuilder.DropTable(
                name: "Prestadores");

            migrationBuilder.DropTable(
                name: "Contratantes");

            migrationBuilder.DropTable(
                name: "TiposPrestadores");

            migrationBuilder.DropTable(
                name: "Intermediarios");

            migrationBuilder.DropSequence(
                name: "AutorizacionesNumeroSeq");
        }
    }
}
