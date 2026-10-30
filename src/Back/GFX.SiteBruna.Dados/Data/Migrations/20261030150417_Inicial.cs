using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GFX.SiteBruna.Dados.Data.Migrations
{
    /// <inheritdoc />
    internal partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "site");

            migrationBuilder.CreateSequence(
                name: "property_code_seq",
                schema: "site",
                startValue: 101L);

            migrationBuilder.CreateTable(
                name: "neighborhoods",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_neighborhoods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "properties",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    finalidade = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    titulo = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    descricao = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    preco = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    aluguel = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    condominio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    iptu = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    entrada_a_partir = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    neighborhood_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mostrar_mapa = table.Column<bool>(type: "boolean", nullable: false),
                    quartos = table.Column<int>(type: "integer", nullable: false),
                    suites = table.Column<int>(type: "integer", nullable: false),
                    banheiros = table.Column<int>(type: "integer", nullable: false),
                    vagas = table.Column<int>(type: "integer", nullable: false),
                    area = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    andar = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    diferenciais = table.Column<List<string>>(type: "text[]", nullable: false),
                    mcmv = table.Column<bool>(type: "boolean", nullable: false),
                    entrada_parcelada = table.Column<bool>(type: "boolean", nullable: false),
                    aceita_fgts = table.Column<bool>(type: "boolean", nullable: false),
                    situacao = table.Column<string>(type: "text", nullable: false),
                    previsao_entrega = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    mobiliado = table.Column<bool>(type: "boolean", nullable: false),
                    aceita_pet = table.Column<bool>(type: "boolean", nullable: false),
                    garantias = table.Column<List<string>>(type: "text[]", nullable: false),
                    video_url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    destaque = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    vendido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    origem = table.Column<string>(type: "text", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_properties", x => x.id);
                    table.CheckConstraint("ck_properties_codigo", "codigo ~ '^[A-Z0-9]{2,20}$'");
                    table.CheckConstraint("ck_properties_finalidade", "finalidade in ('venda', 'aluguel')");
                    table.CheckConstraint("ck_properties_origem", "origem in ('painel', 'whatsapp')");
                    table.CheckConstraint("ck_properties_situacao", "situacao in ('pronto', 'na_planta')");
                    table.CheckConstraint("ck_properties_status", "status in ('rascunho', 'ativo', 'pausado', 'vendido', 'alugado')");
                    table.CheckConstraint("ck_properties_tipo", "tipo in ('apartamento', 'casa', 'terreno', 'sala_comercial')");
                    table.ForeignKey(
                        name: "fk_properties_neighborhoods_neighborhood_id",
                        column: x => x.neighborhood_id,
                        principalSchema: "site",
                        principalTable: "neighborhoods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "agent_operators",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    current_property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    processando_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    processando_ate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_operators", x => x.id);
                    table.CheckConstraint("ck_agent_operators_phone", "phone ~ '^[0-9]{12,13}$'");
                    table.ForeignKey(
                        name: "fk_agent_operators_properties_current_property_id",
                        column: x => x.current_property_id,
                        principalSchema: "site",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "property_internal",
                schema: "site",
                columns: table => new
                {
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proprietario_nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    endereco = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    atualizado_por = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_property_internal", x => x.property_id);
                    table.ForeignKey(
                        name: "fk_property_internal_imovel_property_id",
                        column: x => x.property_id,
                        principalSchema: "site",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "property_photos",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    capa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_property_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_property_photos_imovel_property_id",
                        column: x => x.property_id,
                        principalSchema: "site",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_actions",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    operacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    detalhe = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    antes = table.Column<string>(type: "jsonb", nullable: true),
                    depois = table.Column<string>(type: "jsonb", nullable: true),
                    campos_alterados = table.Column<List<string>>(type: "text[]", nullable: false),
                    sucesso = table.Column<bool>(type: "boolean", nullable: false),
                    erro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_actions", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_actions_agent_operators_operator_id",
                        column: x => x.operator_id,
                        principalSchema: "site",
                        principalTable: "agent_operators",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_agent_actions_properties_property_id",
                        column: x => x.property_id,
                        principalSchema: "site",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "agent_pending_actions",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    dados_propostos = table.Column<string>(type: "jsonb", nullable: true),
                    novo_status = table.Column<string>(type: "text", nullable: true),
                    novo_destaque = table.Column<bool>(type: "boolean", nullable: true),
                    diff = table.Column<string>(type: "jsonb", nullable: false),
                    resumo = table.Column<string>(type: "text", nullable: false),
                    base_atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    expira_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_pending_actions", x => x.id);
                    table.CheckConstraint("ck_agent_pending_actions_status", "status in ('pendente', 'confirmada', 'cancelada', 'expirada')");
                    table.CheckConstraint("ck_agent_pending_actions_tipo", "tipo in ('publicar', 'alterar', 'status', 'destaque', 'desfazer')");
                    table.ForeignKey(
                        name: "fk_agent_pending_actions_imovel_property_id",
                        column: x => x.property_id,
                        principalSchema: "site",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_agent_pending_actions_operador_operator_id",
                        column: x => x.operator_id,
                        principalSchema: "site",
                        principalTable: "agent_operators",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_mensagens",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wamid = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    conteudo = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    erro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    recebida_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_whatsapp_mensagens", x => x.id);
                    table.CheckConstraint("ck_whatsapp_mensagens_status", "status in ('pendente', 'processada', 'erro', 'ignorada')");
                    table.ForeignKey(
                        name: "fk_whatsapp_mensagens_operador_operator_id",
                        column: x => x.operator_id,
                        principalSchema: "site",
                        principalTable: "agent_operators",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_actions_operator_id_operacao_criado_em",
                schema: "site",
                table: "agent_actions",
                columns: new[] { "operator_id", "operacao", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_actions_property_id",
                schema: "site",
                table: "agent_actions",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_operators_current_property_id",
                schema: "site",
                table: "agent_operators",
                column: "current_property_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_operators_phone",
                schema: "site",
                table: "agent_operators",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_agent_pending_actions_operator_id_status_criado_em",
                schema: "site",
                table: "agent_pending_actions",
                columns: new[] { "operator_id", "status", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_pending_actions_property_id",
                schema: "site",
                table: "agent_pending_actions",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_neighborhoods_slug",
                schema: "site",
                table: "neighborhoods",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_properties_codigo",
                schema: "site",
                table: "properties",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_properties_destaque",
                schema: "site",
                table: "properties",
                column: "destaque",
                filter: "destaque");

            migrationBuilder.CreateIndex(
                name: "ix_properties_neighborhood_id",
                schema: "site",
                table: "properties",
                column: "neighborhood_id");

            migrationBuilder.CreateIndex(
                name: "ix_properties_status",
                schema: "site",
                table: "properties",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_property_photos_property_id_ordem",
                schema: "site",
                table: "property_photos",
                columns: new[] { "property_id", "ordem" });

            migrationBuilder.CreateIndex(
                name: "ix_property_photos_storage_path",
                schema: "site",
                table: "property_photos",
                column: "storage_path");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_mensagens_operator_id_recebida_em",
                schema: "site",
                table: "whatsapp_mensagens",
                columns: new[] { "operator_id", "recebida_em" },
                filter: "status = 'pendente'");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_mensagens_wamid",
                schema: "site",
                table: "whatsapp_mensagens",
                column: "wamid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_actions",
                schema: "site");

            migrationBuilder.DropTable(
                name: "agent_pending_actions",
                schema: "site");

            migrationBuilder.DropTable(
                name: "property_internal",
                schema: "site");

            migrationBuilder.DropTable(
                name: "property_photos",
                schema: "site");

            migrationBuilder.DropTable(
                name: "whatsapp_mensagens",
                schema: "site");

            migrationBuilder.DropTable(
                name: "agent_operators",
                schema: "site");

            migrationBuilder.DropTable(
                name: "properties",
                schema: "site");

            migrationBuilder.DropTable(
                name: "neighborhoods",
                schema: "site");

            migrationBuilder.DropSequence(
                name: "property_code_seq",
                schema: "site");
        }
    }
}
