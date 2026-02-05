using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeviceStatusBeacon.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    ProtectedSecretKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceName = table.Column<string>(type: "TEXT", nullable: false),
                    ProtectedSecretKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    LatestLogTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LatestReportedAddresses = table.Column<string>(type: "TEXT", nullable: false),
                    LatestReporterRemoteAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.DeviceId);
                });

            migrationBuilder.CreateTable(
                name: "AccountDevice",
                columns: table => new
                {
                    AuthorizedAccountsAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    QueryableDevicesDeviceId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountDevice", x => new { x.AuthorizedAccountsAccountId, x.QueryableDevicesDeviceId });
                    table.ForeignKey(
                        name: "FK_AccountDevice_Accounts_AuthorizedAccountsAccountId",
                        column: x => x.AuthorizedAccountsAccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountDevice_Devices_QueryableDevicesDeviceId",
                        column: x => x.QueryableDevicesDeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnlineLogs",
                columns: table => new
                {
                    OnlineLogId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LogTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReportedAddresses = table.Column<string>(type: "TEXT", nullable: false),
                    ReporterRemoteAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineLogs", x => x.OnlineLogId);
                    table.ForeignKey(
                        name: "FK_OnlineLogs_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountDevice_QueryableDevicesDeviceId",
                table: "AccountDevice",
                column: "QueryableDevicesDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Username",
                table: "Accounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceName",
                table: "Devices",
                column: "DeviceName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnlineLogs_DeviceId_LogTime",
                table: "OnlineLogs",
                columns: new[] { "DeviceId", "LogTime" });

            migrationBuilder.CreateIndex(
                name: "IX_OnlineLogs_LogTime",
                table: "OnlineLogs",
                column: "LogTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountDevice");

            migrationBuilder.DropTable(
                name: "OnlineLogs");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
