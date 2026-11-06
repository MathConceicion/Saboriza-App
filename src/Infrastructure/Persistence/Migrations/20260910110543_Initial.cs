using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace GastronomiaMarketplace.Infrastructure.Persistence.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"pgcrypto\";");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"postgis\";");
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Acao = table.Column<string>(type: "varchar(100)", nullable: false),
                    Entidade = table.Column<string>(type: "varchar(100)", nullable: false),
                    EntidadeId = table.Column<string>(type: "varchar(100)", nullable: false),
                    DataUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    ValoresAntigosJson = table.Column<string>(type: "jsonb", nullable: true),
                    ValoresNovosJson = table.Column<string>(type: "jsonb", nullable: true),
                    IpOrigem = table.Column<string>(type: "varchar(45)", nullable: true),
                    UserAgent = table.Column<string>(type: "varchar(500)", nullable: true),
                    Detalhes = table.Column<string>(type: "text", nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoriasLoja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Nome = table.Column<string>(type: "varchar(100)", nullable: false),
                    Slug = table.Column<string>(type: "varchar(100)", nullable: false),
                    IconeUrl = table.Column<string>(type: "varchar(500)", nullable: true),
                    OrdemExibicao = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasLoja", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracoesGlobais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TaxaComissaoPadraoPercentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    VigenciaDesde = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesGlobais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    NomeCompleto = table.Column<string>(type: "varchar(200)", nullable: false),
                    Email = table.Column<string>(type: "varchar(254)", nullable: false),
                    SenhaHash = table.Column<string>(type: "varchar(255)", nullable: false),
                    Cpf = table.Column<string>(type: "varchar(11)", nullable: true),
                    Telefone = table.Column<string>(type: "varchar(20)", nullable: true),
                    EnderecoAtual = table.Column<string>(type: "varchar(500)", nullable: true),
                    Cep = table.Column<string>(type: "varchar(9)", nullable: true),
                    Papel = table.Column<short>(type: "smallint", nullable: false),
                    AlergiasSeveras = table.Column<string>(type: "text", nullable: true),
                    RestricoesAlimentares = table.Column<string>(type: "text", nullable: true),
                    ObservacoesSaude = table.Column<string>(type: "text", nullable: true),
                    IsAnonimizado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsentimentosLgpd",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<short>(type: "smallint", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ConcedidoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    RevogadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    TextoConsentimento = table.Column<string>(type: "text", nullable: false),
                    IpOrigem = table.Column<string>(type: "varchar(45)", nullable: true),
                    UserAgent = table.Column<string>(type: "varchar(500)", nullable: true),
                    MotivoRevogacao = table.Column<string>(type: "varchar(500)", nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentimentosLgpd", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsentimentosLgpd_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vendedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeLoja = table.Column<string>(type: "varchar(150)", nullable: false),
                    CategoriaLojaId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnderecoComercial = table.Column<string>(type: "varchar(500)", nullable: false),
                    Cep = table.Column<string>(type: "varchar(9)", nullable: false),
                    Telefone = table.Column<string>(type: "varchar(20)", nullable: false),
                    Cnpj = table.Column<string>(type: "varchar(14)", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    MotivoRejeicao = table.Column<string>(type: "varchar(500)", nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendedores_CategoriasLoja_CategoriaLojaId",
                        column: x => x.CategoriaLojaId,
                        principalTable: "CategoriasLoja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vendedores_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lojas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    VendedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "varchar(150)", nullable: false),
                    CategoriaLojaId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnderecoComercial = table.Column<string>(type: "varchar(500)", nullable: false),
                    Cep = table.Column<string>(type: "varchar(9)", nullable: false),
                    Localizacao = table.Column<Point>(type: "geography (point)", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Aprovado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    HorarioFuncionamentoJson = table.Column<string>(type: "jsonb", nullable: true),
                    TaxaComissaoPercentual = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lojas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lojas_CategoriasLoja_CategoriaLojaId",
                        column: x => x.CategoriaLojaId,
                        principalTable: "CategoriasLoja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lojas_Vendedores_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    LojaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    ValorBruto = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TaxaComissaoPercentual = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    TaxaComissaoValor = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    ValorLiquidoVendedor = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    PixPayload = table.Column<string>(type: "text", nullable: true),
                    PixSimuladoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    AprovadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    MotivoCancelamento = table.Column<string>(type: "varchar(500)", nullable: true),
                    ClienteNomeSnapshot = table.Column<string>(type: "varchar(200)", nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pedidos_Lojas_LojaId",
                        column: x => x.LojaId,
                        principalTable: "Lojas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pedidos_Usuarios_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LojaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoriaLojaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Nome = table.Column<string>(type: "varchar(150)", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    Preco = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    QuantidadeEmEstoque = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Aprovado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ImagemUrl = table.Column<string>(type: "varchar(1000)", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Produtos_Lojas_LojaId",
                        column: x => x.LojaId,
                        principalTable: "Lojas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensAgenda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PedidoId = table.Column<Guid>(type: "uuid", nullable: false),
                    LojaId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    AprovadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    ProntoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensAgenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensAgenda_Lojas_LojaId",
                        column: x => x.LojaId,
                        principalTable: "Lojas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensAgenda_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensDoPedido",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PedidoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoNomeSnapshot = table.Column<string>(type: "varchar(150)", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ProdutoAtivo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensDoPedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensDoPedido_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_DataUtc",
                table: "AuditLogs",
                column: "DataUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entidade_EntidadeId",
                table: "AuditLogs",
                columns: new[] { "Entidade", "EntidadeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UsuarioId_DataUtc",
                table: "AuditLogs",
                columns: new[] { "UsuarioId", "DataUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasLoja_Slug",
                table: "CategoriasLoja",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsentimentosLgpd_UsuarioId_Tipo",
                table: "ConsentimentosLgpd",
                columns: new[] { "UsuarioId", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_ItensAgenda_LojaId_AprovadoEmUtc",
                table: "ItensAgenda",
                columns: new[] { "LojaId", "AprovadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ItensAgenda_LojaId_Status_AprovadoEmUtc",
                table: "ItensAgenda",
                columns: new[] { "LojaId", "Status", "AprovadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ItensAgenda_PedidoId",
                table: "ItensAgenda",
                column: "PedidoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensAgenda_VendedorId_Status",
                table: "ItensAgenda",
                columns: new[] { "VendedorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ItensDoPedido_PedidoId",
                table: "ItensDoPedido",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensDoPedido_ProdutoId",
                table: "ItensDoPedido",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Lojas_CategoriaLojaId",
                table: "Lojas",
                column: "CategoriaLojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Lojas_Localizacao",
                table: "Lojas",
                column: "Localizacao")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Lojas_VendedorId",
                table: "Lojas",
                column: "VendedorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_ClienteId",
                table: "Pedidos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_CriadoEmUtc",
                table: "Pedidos",
                column: "CriadoEmUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_LojaId",
                table: "Pedidos",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_Status",
                table: "Pedidos",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_LojaId",
                table: "Produtos",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Cpf",
                table: "Usuarios",
                column: "Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_CategoriaLojaId",
                table: "Vendedores",
                column: "CategoriaLojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_Status",
                table: "Vendedores",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_UsuarioId",
                table: "Vendedores",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.Sql("CREATE OR REPLACE FUNCTION fn_prevent_audit_mutation() RETURNS trigger AS $$ BEGIN RAISE EXCEPTION 'AuditLogs é imutável — UPDATE/DELETE não permitido'; END; $$ LANGUAGE plpgsql;");
            migrationBuilder.Sql("CREATE TRIGGER trg_audit_immutable BEFORE UPDATE OR DELETE ON \"AuditLogs\" FOR EACH ROW EXECUTE FUNCTION fn_prevent_audit_mutation();");
            migrationBuilder.Sql("INSERT INTO \"ConfiguracoesGlobais\" (\"Id\", \"TaxaComissaoPadraoPercentual\", \"VigenciaDesde\", \"CriadoEmUtc\", \"IsDeleted\") VALUES (gen_random_uuid(), 8.50, now(), now(), false) ON CONFLICT DO NOTHING;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ConfiguracoesGlobais");

            migrationBuilder.DropTable(
                name: "ConsentimentosLgpd");

            migrationBuilder.DropTable(
                name: "ItensAgenda");

            migrationBuilder.DropTable(
                name: "ItensDoPedido");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropTable(
                name: "Lojas");

            migrationBuilder.DropTable(
                name: "Vendedores");

            migrationBuilder.DropTable(
                name: "CategoriasLoja");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
