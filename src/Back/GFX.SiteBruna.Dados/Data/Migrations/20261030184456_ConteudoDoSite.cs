using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GFX.SiteBruna.Dados.Data.Migrations
{
    /// <inheritdoc />
    internal partial class ConteudoDoSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "events",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pagina = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    visitor_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    utm_source = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    utm_medium = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    utm_campaign = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    referrer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dispositivo = table.Column<string>(type: "text", nullable: false),
                    meta = table.Column<string>(type: "jsonb", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                    table.CheckConstraint("ck_events_dispositivo", "dispositivo in ('mobile', 'tablet', 'desktop')");
                    table.CheckConstraint("ck_events_tipo", "tipo in ('page_view', 'property_view', 'whatsapp_click', 'filter_used', 'share_click')");
                    table.CheckConstraint("ck_events_visitor_id", "visitor_id ~ '^[A-Za-z0-9-]{8,64}$'");
                    table.ForeignKey(
                        name: "fk_events_imovel_property_id",
                        column: x => x.property_id,
                        principalSchema: "site",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "faqs",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pergunta = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    resposta = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_faqs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pages",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    home = table.Column<string>(type: "jsonb", nullable: false),
                    sobre = table.Column<string>(type: "jsonb", nullable: false),
                    mcmv = table.Column<string>(type: "jsonb", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pages", x => x.id);
                    table.CheckConstraint("ck_pages_linha_unica", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    whatsapp = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    mensagem_padrao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    instagram = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    facebook = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    creci = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    cidade = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ga4_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    meta_pixel_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dias_selo_vendido = table.Column<int>(type: "integer", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settings", x => x.id);
                    table.CheckConstraint("ck_settings_dias_selo_vendido", "dias_selo_vendido between 0 and 365");
                    table.CheckConstraint("ck_settings_linha_unica", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "testimonials",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    texto = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    foto_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    exibir_home = table.Column<bool>(type: "boolean", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_testimonials", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "site",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    hash_da_senha = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ultimo_acesso_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_events_criado_em",
                schema: "site",
                table: "events",
                column: "criado_em");

            migrationBuilder.CreateIndex(
                name: "ix_events_property_id_visitor_id",
                schema: "site",
                table: "events",
                columns: new[] { "property_id", "visitor_id" },
                unique: true,
                filter: "tipo = 'property_view'");

            migrationBuilder.CreateIndex(
                name: "ix_events_tipo_criado_em",
                schema: "site",
                table: "events",
                columns: new[] { "tipo", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_faqs_ordem",
                schema: "site",
                table: "faqs",
                column: "ordem");

            migrationBuilder.CreateIndex(
                name: "ix_testimonials_ordem",
                schema: "site",
                table: "testimonials",
                column: "ordem");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_email",
                schema: "site",
                table: "usuarios",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "events",
                schema: "site");

            migrationBuilder.DropTable(
                name: "faqs",
                schema: "site");

            migrationBuilder.DropTable(
                name: "pages",
                schema: "site");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "site");

            migrationBuilder.DropTable(
                name: "testimonials",
                schema: "site");

            migrationBuilder.DropTable(
                name: "usuarios",
                schema: "site");
        }
    }
}
