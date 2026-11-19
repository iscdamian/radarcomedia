using System;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RadarComedy.Domain;

namespace RadarComedy.Infrastructure;

// Persistence mapping for the reference V001 SQL schema. Adjust EF migrations and model
// snapshot together when production code changes. The SQL remains the reviewed baseline.
public sealed class RadarDbContext(DbContextOptions<RadarDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Promoter> Promoters => Set<Promoter>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<ComedyEvent> Events => Set<ComedyEvent>();
    public DbSet<EventRevision> EventRevisions => Set<EventRevision>();
    public DbSet<RecurrenceSchedule> RecurrenceSchedules => Set<RecurrenceSchedule>();
    public DbSet<EventOccurrence> EventOccurrences => Set<EventOccurrence>();
    public DbSet<ListingEntitlement> ListingEntitlements => Set<ListingEntitlement>();
    public DbSet<CheckoutOrder> CheckoutOrders => Set<CheckoutOrder>();
    public DbSet<StripeWebhookInbox> StripeWebhookInbox => Set<StripeWebhookInbox>();
    public DbSet<TransferRequest> TransferRequests => Set<TransferRequest>();
    public DbSet<EmergencyHide> EmergencyHides => Set<EmergencyHide>();
    public DbSet<OccurrenceException> OccurrenceExceptions => Set<OccurrenceException>();
    public DbSet<Comedian> Comedians => Set<Comedian>();
    public DbSet<EventPerformer> EventPerformers => Set<EventPerformer>();
    public DbSet<ModerationAction> ModerationActions => Set<ModerationAction>();
    public DbSet<EventReport> EventReports => Set<EventReport>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<ImportSource> ImportSources => Set<ImportSource>();
    public DbSet<ImportCandidate> ImportCandidates => Set<ImportCandidate>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasPostgresExtension("postgis");
        b.Entity<AppUser>(e => { e.ToTable("app_users"); e.HasKey(x => x.Id);
            e.Property(x => x.Role).HasConversion<string>(); e.HasIndex(x => x.EmailNormalized).IsUnique(); });
        b.Entity<Promoter>(e => { e.ToTable("promoters"); e.HasKey(x => x.Id);
            e.HasIndex(x => x.OwnerUserId).IsUnique();
            e.Property(x => x.Status).HasConversion<string>(); e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<Venue>(e => { e.ToTable("venues"); e.HasKey(x => x.Id);
            e.Property(x => x.Location).HasColumnType("geography (Point, 4326)");
            e.HasIndex(x => x.Location).HasMethod("gist"); });
        b.Entity<ComedyEvent>(e => { e.ToTable("events"); e.HasKey(x => x.Id);
            e.Property(x => x.Category).HasConversion<string>(); e.Property(x => x.Format).HasConversion<string>();
            e.Property(x => x.Version).IsConcurrencyToken(); e.HasIndex(x => x.Slug).IsUnique();
            e.HasOne<Promoter>().WithMany().HasForeignKey(x => x.PromoterId).OnDelete(DeleteBehavior.Restrict);
            // Composite FK ensures the published revision belongs to this event.
            e.HasOne<EventRevision>().WithMany().HasForeignKey(x => new { x.Id, x.PublishedRevisionId })
                .HasPrincipalKey(x => new { x.EventId, x.Id }).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<EventRevision>(e => { e.ToTable("event_revisions"); e.HasKey(x => x.Id);
            e.HasAlternateKey(x => new { x.EventId, x.Id });
            e.HasIndex(x => new { x.EventId, x.RevisionNumber }).IsUnique();
            e.Property(x => x.SnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.Status).HasConversion<string>();
            e.HasOne<ComedyEvent>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<RecurrenceSchedule>(e => { e.ToTable("recurrence_schedules"); e.HasKey(x => x.EventId);
            e.Property(x => x.Frequency).HasConversion<string>();
            e.HasOne<ComedyEvent>().WithOne().HasForeignKey<RecurrenceSchedule>(x => x.EventId); });
        b.Entity<EventOccurrence>(e => { e.ToTable("event_occurrences"); e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
            e.HasIndex(x => x.StartsAtUtc);
            e.HasOne<ComedyEvent>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Venue>().WithMany().HasForeignKey(x => x.VenueId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<ListingEntitlement>(e => { e.ToTable("listing_entitlements"); e.HasKey(x => x.Id);
            e.Property(x => x.Plan).HasConversion<string>(); e.Property(x => x.Status).HasConversion<string>();
            e.HasOne<ComedyEvent>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<CheckoutOrder>(e => { e.ToTable("checkout_orders"); e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
            e.HasIndex(x => x.StripeCheckoutSessionId).IsUnique(); e.HasIndex(x => x.StripePaymentIntentId).IsUnique();
            e.HasOne<ListingEntitlement>().WithMany().HasForeignKey(x => x.EntitlementId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<StripeWebhookInbox>(e => { e.ToTable("stripe_webhook_inbox"); e.HasKey(x => x.StripeEventId);
            e.Property(x => x.PayloadJson).HasColumnType("jsonb"); e.Property(x => x.Status).HasConversion<string>(); });
        b.Entity<TransferRequest>(e => { e.ToTable("transfer_requests"); e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>(); });
        b.Entity<EmergencyHide>(e => { e.ToTable("emergency_hides"); e.HasKey(x => x.Id);
            e.Property(x => x.ReviewStatus).HasConversion<string>(); });
        b.Entity<OccurrenceException>(e => { e.ToTable("occurrence_exceptions"); e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasConversion<string>(); });
        b.Entity<Comedian>(e => { e.ToTable("comedians"); e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.ProfileStatus).HasConversion<string>();
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ApprovedBy).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<EventPerformer>(e => { e.ToTable("event_performers"); e.HasKey(x => new { x.OccurrenceId, x.ComedianId });
            e.HasOne<EventOccurrence>().WithMany().HasForeignKey(x => x.OccurrenceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Comedian>().WithMany().HasForeignKey(x => x.ComedianId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<ModerationAction>(e => { e.ToTable("moderation_actions"); e.HasKey(x => x.Id); });
        b.Entity<EventReport>(e => { e.ToTable("event_reports"); e.HasKey(x => x.Id); });
        b.Entity<NewsletterSubscriber>(e => { e.ToTable("newsletter_subscribers"); e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EmailNormalized, x.CityCode, x.Language }).IsUnique(); });
        b.Entity<ImportSource>(e => { e.ToTable("import_sources"); e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique(); });
        b.Entity<ImportCandidate>(e => { e.ToTable("import_candidates"); e.HasKey(x => x.Id);
            e.Property(x => x.PermittedMetadataJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.SourceId, x.ExternalId }).IsUnique(); });

        // Deterministic snake_case property mapping, no extra naming-convention package needed.
        // DB constraints/indexes not expressible in EF metadata live in the audited SQL migration.
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
                property.SetColumnName(SnakeCase(property.Name));
    }

    private static string SnakeCase(string name)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0 &&
                (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]) ||
                 (char.IsUpper(name[i - 1]) && i + 1 < name.Length && char.IsLower(name[i + 1]))))
                sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
