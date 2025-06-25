using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koncierge.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KubeConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KubeConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    KonciergeKubeConfigId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contexts_KubeConfigs_KonciergeKubeConfigId",
                        column: x => x.KonciergeKubeConfigId,
                        principalTable: "KubeConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NameSpaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    KonciergeContextConfigId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NameSpaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NameSpaces_Contexts_KonciergeContextConfigId",
                        column: x => x.KonciergeContextConfigId,
                        principalTable: "Contexts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Forwards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetType = table.Column<int>(type: "INTEGER", nullable: false),
                    KonciergeNamespaceConfigId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forwards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Forwards_NameSpaces_KonciergeNamespaceConfigId",
                        column: x => x.KonciergeNamespaceConfigId,
                        principalTable: "NameSpaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ForwardLinkedConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    KonciergeForwardId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForwardLinkedConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForwardLinkedConfigs_Forwards_KonciergeForwardId",
                        column: x => x.KonciergeForwardId,
                        principalTable: "Forwards",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contexts_KonciergeKubeConfigId",
                table: "Contexts",
                column: "KonciergeKubeConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ForwardLinkedConfigs_KonciergeForwardId",
                table: "ForwardLinkedConfigs",
                column: "KonciergeForwardId");

            migrationBuilder.CreateIndex(
                name: "IX_Forwards_KonciergeNamespaceConfigId",
                table: "Forwards",
                column: "KonciergeNamespaceConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_NameSpaces_KonciergeContextConfigId",
                table: "NameSpaces",
                column: "KonciergeContextConfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForwardLinkedConfigs");

            migrationBuilder.DropTable(
                name: "Forwards");

            migrationBuilder.DropTable(
                name: "NameSpaces");

            migrationBuilder.DropTable(
                name: "Contexts");

            migrationBuilder.DropTable(
                name: "KubeConfigs");
        }
    }
}
