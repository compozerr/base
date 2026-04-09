using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stripe.Migrations
{
    /// <inheritdoc />
    public partial class BackfillOrgInternalIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clean up orphaned StripeCustomer records where the InternalId points to
            // a user ID that no longer exists in the Users table and has no personal org.
            // These are stale records from deleted users.
            migrationBuilder.Sql(@"
        DELETE FROM stripe.""StripeCustomers"" sc
        WHERE NOT EXISTS (
            SELECT 1 FROM organizations.""Organizations"" o2
            WHERE o2.""Id""::text = sc.""InternalId""
        )
        AND NOT EXISTS (
            SELECT 1 FROM auth.""Users"" u
            WHERE u.""Id""::text = sc.""InternalId""
        );
    ");

            // Pre-flight: verify all remaining StripeCustomer InternalIds that look like user IDs
            // have a corresponding personal org
            migrationBuilder.Sql(@"
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1 FROM stripe.""StripeCustomers"" sc
                LEFT JOIN organizations.""Organizations"" o
                    ON o.""OwnerUserId"" = sc.""InternalId""::uuid
                    AND o.""IsPersonal"" = true
                WHERE NOT EXISTS (
                    SELECT 1 FROM organizations.""Organizations"" o2
                    WHERE o2.""Id""::text = sc.""InternalId""
                )
                AND o.""Id"" IS NULL
            ) THEN
                RAISE EXCEPTION 'Pre-flight failed: some StripeCustomer InternalIds are user IDs without personal organizations';
            END IF;
        END $$;
    ");

            // Backfill: replace user IDs with their personal org IDs
            // Only update rows where InternalId matches a user ID (not already an org ID)
            migrationBuilder.Sql(@"
        UPDATE stripe.""StripeCustomers"" sc
        SET ""InternalId"" = o.""Id""::text
        FROM organizations.""Organizations"" o
        JOIN auth.""Users"" u ON o.""OwnerUserId"" = u.""Id""
        WHERE o.""IsPersonal"" = true
          AND sc.""InternalId"" = u.""Id""::text;
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: restore user IDs from personal org owner
            migrationBuilder.Sql(@"
        UPDATE stripe.""StripeCustomers"" sc
        SET ""InternalId"" = o.""OwnerUserId""::text
        FROM organizations.""Organizations"" o
        WHERE o.""IsPersonal"" = true
          AND sc.""InternalId"" = o.""Id""::text;
    ");
        }
    }
}
