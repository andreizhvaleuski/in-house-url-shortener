using FluentMigrator;
using IHUS.Domain.Constants;

[Migration(20220708110948)]
public sealed class Initial : Migration
{
    public override void Up()
    {
        Create.Table("ShortenedUrls")
            .WithColumn("ShortUrlKey")
                .AsFixedLengthString(Limits.ShortUrlKeyLength)
                .PrimaryKey()
            .WithColumn("ActualUrl")
                .AsString(Limits.ActualUrlMaxLength)
                .NotNullable();
    }

    public override void Down()
    {
        Delete.Table("ShortenedUrls");
    }
}
