using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastronomiaMarketplace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SuperAdminDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BloqueadoEmUtc",
                table: "Usuarios",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BloqueadoPorId",
                table: "Usuarios",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBloqueado",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MotivoBloqueio",
                table: "Usuarios",
                type: "varchar(500)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Notificacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Titulo = table.Column<string>(type: "varchar(200)", nullable: false),
                    Mensagem = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<short>(type: "smallint", nullable: false),
                    Lida = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LidaEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    Url = table.Column<string>(type: "varchar(500)", nullable: true),
                    EntidadeTipo = table.Column<string>(type: "varchar(100)", nullable: true),
                    EntidadeId = table.Column<string>(type: "varchar(100)", nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_CriadoEmUtc",
                table: "Notificacoes",
                column: "CriadoEmUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_Lida",
                table: "Notificacoes",
                column: "Lida");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UsuarioId",
                table: "Notificacoes",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "BloqueadoEmUtc",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "BloqueadoPorId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "IsBloqueado",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "MotivoBloqueio",
                table: "Usuarios");
        }
    }
}
