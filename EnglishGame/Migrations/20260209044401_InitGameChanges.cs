using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishGame.Migrations
{
    /// <inheritdoc />
    public partial class InitGameChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attempts_GameSessions_GameSessionId1",
                table: "Attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_GameSessions_AiContents_AiContentId1",
                table: "GameSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_GameSessions_Topics_TopicId1",
                table: "GameSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_GameSessions_Users_UserId1",
                table: "GameSessions");

            migrationBuilder.DropIndex(
                name: "IX_GameSessions_AiContentId1",
                table: "GameSessions");

            migrationBuilder.DropIndex(
                name: "IX_GameSessions_TopicId1",
                table: "GameSessions");

            migrationBuilder.DropIndex(
                name: "IX_GameSessions_UserId1",
                table: "GameSessions");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_GameSessionId",
                table: "Attempts");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_GameSessionId1",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "AiContentId1",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "TopicId1",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "GameSessionId1",
                table: "Attempts");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartedAtUtc",
                table: "GameSessions",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "FinishedAtUtc",
                table: "GameSessions",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ReviewedAtUtc",
                table: "AiContents",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "AiContents",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_GameSessionId_QuestionNo",
                table: "Attempts",
                columns: new[] { "GameSessionId", "QuestionNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attempts_GameSessionId_QuestionNo",
                table: "Attempts");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartedAtUtc",
                table: "GameSessions",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FinishedAtUtc",
                table: "GameSessions",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AiContentId1",
                table: "GameSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TopicId1",
                table: "GameSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "GameSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GameSessionId1",
                table: "Attempts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReviewedAtUtc",
                table: "AiContents",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "AiContents",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_AiContentId1",
                table: "GameSessions",
                column: "AiContentId1");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_TopicId1",
                table: "GameSessions",
                column: "TopicId1");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_UserId1",
                table: "GameSessions",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_GameSessionId",
                table: "Attempts",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_GameSessionId1",
                table: "Attempts",
                column: "GameSessionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Attempts_GameSessions_GameSessionId1",
                table: "Attempts",
                column: "GameSessionId1",
                principalTable: "GameSessions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameSessions_AiContents_AiContentId1",
                table: "GameSessions",
                column: "AiContentId1",
                principalTable: "AiContents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameSessions_Topics_TopicId1",
                table: "GameSessions",
                column: "TopicId1",
                principalTable: "Topics",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameSessions_Users_UserId1",
                table: "GameSessions",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
