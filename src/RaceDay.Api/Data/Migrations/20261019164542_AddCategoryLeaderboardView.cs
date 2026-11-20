using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Api.Data.Migrations
{
    public partial class AddCategoryLeaderboardView : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE VIEW dbo.vw_CategoryLeaderboard
                AS
                SELECT
                    ec.EventId,
                    e.EventName,
                    ec.CategoryId,
                    ec.CategoryName,
                    ec.DistanceKm,
                    en.EnrolmentId,
                    en.RaceNumber,
                    u.UserId          AS ParticipantId,
                    u.FirstName + ' ' + u.LastName AS ParticipantName,
                    u.Gender,
                    r.ResultStatus,
                    r.FinishTime,
                    r.ChipTimeSeconds,
                    r.OverallPosition,
                    r.GenderPosition,
                    r.AgeGroupPosition
                FROM dbo.Results          AS r
                INNER JOIN dbo.Enrolments      AS en ON en.EnrolmentId = r.EnrolmentId
                INNER JOIN dbo.Users           AS u  ON u.UserId       = en.ParticipantId
                INNER JOIN dbo.EventCategories AS ec ON ec.CategoryId  = en.CategoryId
                INNER JOIN dbo.Events          AS e  ON e.EventId      = ec.EventId;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_CategoryLeaderboard;");
        }
    }
}
