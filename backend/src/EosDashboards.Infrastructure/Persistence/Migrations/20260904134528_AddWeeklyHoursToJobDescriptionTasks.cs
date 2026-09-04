using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EosDashboards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyHoursToJobDescriptionTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WeeklyHours",
                table: "JobDescriptionTasks",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql("""
                EXEC(N'
                CREATE FUNCTION dbo.ConvertStoredPersianDate(@value date)
                RETURNS date
                AS
                BEGIN
                    DECLARE @jy int = YEAR(@value), @jm int = MONTH(@value) - 1, @jd int = DAY(@value);
                    DECLARE @gy int, @gm int = 1, @gd int, @jDayNo int, @gDayNo int, @i int = 0;
                    IF @jy > 979
                    BEGIN
                        SET @gy = 1600;
                        SET @jy = @jy - 979;
                    END
                    ELSE
                    BEGIN
                        SET @gy = 621;
                    END;
                    SET @jDayNo = 365 * @jy + (@jy / 33) * 8 + ((@jy % 33 + 3) / 4);
                    WHILE @i < @jm
                    BEGIN
                        SET @jDayNo = @jDayNo + CASE WHEN @i < 6 THEN 31 ELSE 30 END;
                        SET @i = @i + 1;
                    END;
                    SET @jDayNo = @jDayNo + @jd;
                    SET @gDayNo = @jDayNo + 79;
                    SET @gy = @gy + 400 * (@gDayNo / 146097);
                    SET @gDayNo = @gDayNo % 146097;
                    IF @gDayNo >= 36525
                    BEGIN
                        SET @gDayNo = @gDayNo - 1;
                        SET @gy = @gy + 100 * (@gDayNo / 36524);
                        SET @gDayNo = @gDayNo % 36524;
                        IF @gDayNo >= 365 SET @gDayNo = @gDayNo + 1;
                    END;
                    SET @gy = @gy + 4 * (@gDayNo / 1461);
                    SET @gDayNo = @gDayNo % 1461;
                    IF @gDayNo > 365
                    BEGIN
                        SET @gDayNo = @gDayNo - 1;
                        SET @gy = @gy + (@gDayNo / 365);
                        SET @gDayNo = @gDayNo % 365;
                    END;
                    WHILE @gm <= 12 AND @gDayNo >= CASE
                        WHEN @gm IN (1, 3, 5, 7, 8, 10, 12) THEN 31
                        WHEN @gm = 2 AND (@gy % 4 = 0 AND (@gy % 100 <> 0 OR @gy % 400 = 0)) THEN 29
                        WHEN @gm = 2 THEN 28
                        ELSE 30
                    END
                    BEGIN
                        SET @gDayNo = @gDayNo - CASE
                            WHEN @gm IN (1, 3, 5, 7, 8, 10, 12) THEN 31
                            WHEN @gm = 2 AND (@gy % 4 = 0 AND (@gy % 100 <> 0 OR @gy % 400 = 0)) THEN 29
                            WHEN @gm = 2 THEN 28
                            ELSE 30
                        END;
                        SET @gm = @gm + 1;
                    END;
                    SET @gd = @gDayNo + 1;
                    RETURN DATEFROMPARTS(@gy, @gm, @gd);
                END');
                """);
            migrationBuilder.Sql("""
                UPDATE JobDescriptionTasks
                SET StartDate = dbo.ConvertStoredPersianDate(StartDate)
                WHERE StartDate IS NOT NULL AND YEAR(StartDate) BETWEEN 1200 AND 1700;
                UPDATE JobDescriptionTasks
                SET EndDate = dbo.ConvertStoredPersianDate(EndDate)
                WHERE EndDate IS NOT NULL AND YEAR(EndDate) BETWEEN 1200 AND 1700;
                UPDATE JobDescriptionVersionUnresolvedTasks
                SET StartDate = dbo.ConvertStoredPersianDate(StartDate)
                WHERE StartDate IS NOT NULL AND YEAR(StartDate) BETWEEN 1200 AND 1700;
                UPDATE JobDescriptionVersionUnresolvedTasks
                SET EndDate = dbo.ConvertStoredPersianDate(EndDate)
                WHERE EndDate IS NOT NULL AND YEAR(EndDate) BETWEEN 1200 AND 1700;
                """);
            migrationBuilder.Sql("DROP FUNCTION dbo.ConvertStoredPersianDate;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeeklyHours",
                table: "JobDescriptionTasks");
        }
    }
}
