using System;
using NetTopologySuite.Geometries;

namespace RadarComedy.Domain;

// These entities are deliberately persistence-friendly. Business rules belong in services
// and are enforced again at DB/API boundaries. Guid keys are application-generated.
public enum UserRole { Fan, Promoter, Admin }
public enum PromoterStatus { PendingReview, Approved, Rejected, Suspended }
public enum EventCategory { StandUp, OpenMic, Improv, Sketch, ComedyTheater, Festival, OtherLiveComedy }
public enum EventFormat { Single, Recurring }
public enum RevisionStatus { Draft, Submitted, ChangesRequested, Approved, Rejected, Superseded }
public enum OccurrenceStatus { Scheduled, Canceled, Postponed, Completed }
public enum ListingPlan { Single, RecurringThreeMonths }
public enum EntitlementStatus { PendingPayment, Active, Suspended, Expired, Revoked }
public enum OrderStatus { Created, CheckoutOpen, Paid, Failed, Canceled, Refunded, Disputed }
public enum TransferStatus { Requested, Approved, Rejected, Applied }
public enum HideReviewStatus { Pending, Confirmed, Dismissed }
public enum ExceptionKind { Skip, Move }
public enum RecurrenceFrequency { Daily, Weekly, Monthly }
public enum WebhookProcessingStatus { Received, Processed, Failed }
public enum ComedianProfileStatus { Draft, Published, Hidden }

public sealed class AppUser
{
    public Guid Id { get; set; }
    public string EmailNormalized { get; set; } = "";
    public DateTime? EmailVerifiedAt { get; set; }
    public UserRole Role { get; set; } = UserRole.Fan;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class Promoter
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string DisplayName { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string PublicProfileUrl { get; set; } = "";
    public string CityCode { get; set; } = "";
    public PromoterStatus Status { get; set; } = PromoterStatus.PendingReview;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}
public sealed class Venue
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string StreetAddress { get; set; } = "";
    public string City { get; set; } = "";
    public string Region { get; set; } = "";
    public string? PostalCode { get; set; }
    public string CountryCode { get; set; } = "";
    public string CityCode { get; set; } = "";
    public string TimeZoneId { get; set; } = "";
    // NTS convention: X=longitude, Y=latitude, SRID=4326.
    public Point Location { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class ComedyEvent
{
    public Guid Id { get; set; }
    public Guid PromoterId { get; set; }
    public string Slug { get; set; } = "";
    public EventCategory Category { get; set; }
    public EventFormat Format { get; set; }
    public Guid? PublishedRevisionId { get; set; }
    public DateTime? VisibilitySuppressedAt { get; set; }
    public string? SuppressionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long Version { get; set; } = 1;
}
public sealed class EventRevision
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public int RevisionNumber { get; set; }
    public Guid? BasedOnRevisionId { get; set; }
    public string SnapshotJson { get; set; } = "{}";
    public RevisionStatus Status { get; set; } = RevisionStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class RecurrenceSchedule
{
    public Guid EventId { get; set; }
    public Guid VenueId { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public int IntervalCount { get; set; } = 1;
    public int DaysOfWeekMask { get; set; }
    public int? DayOfMonth { get; set; }
    public TimeOnly LocalStartTime { get; set; }
    public int DurationMinutes { get; set; }
    public string TimeZoneId { get; set; } = "";
    public Guid? PublishedRevisionId { get; set; }
}
public sealed class EventOccurrence
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid PublishedRevisionId { get; set; }
    public Guid VenueId { get; set; }
    public DateOnly LocalDate { get; set; }
    public TimeOnly LocalStartTime { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public string TimeZoneId { get; set; } = "";
    public OccurrenceStatus Status { get; set; } = OccurrenceStatus.Scheduled;
    public DateTime? EmergencyHiddenAt { get; set; }
    public string TicketUrl { get; set; } = "";
    public int? AdmissionPriceCents { get; set; }
    public string? AdmissionCurrency { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class ListingEntitlement
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public ListingPlan Plan { get; set; }
    public int PriceCents { get; set; }
    public string Currency { get; set; } = "USD";
    public EntitlementStatus Status { get; set; } = EntitlementStatus.PendingPayment;
    public Guid? SingleOccurrenceId { get; set; }
    public DateOnly? TermStartLocalDate { get; set; }
    public DateOnly? TermEndExclusiveLocalDate { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class CheckoutOrder
{
    public Guid Id { get; set; }
    public Guid EntitlementId { get; set; }
    public int AmountCents { get; set; }
    public int? TaxIncludedCents { get; set; }
    public string Currency { get; set; } = "USD";
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public string? StripeCheckoutSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class StripeWebhookInbox
{
    public string StripeEventId { get; set; } = "";
    public string EventType { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
    public WebhookProcessingStatus Status { get; set; } = WebhookProcessingStatus.Received;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorSummary { get; set; }
}
public sealed class TransferRequest
{
    public Guid Id { get; set; }
    public Guid EntitlementId { get; set; }
    public Guid OriginalOccurrenceId { get; set; }
    public Guid ProposedRevisionId { get; set; }
    public DateOnly ProposedLocalDate { get; set; }
    public string? Reason { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Requested;
    public Guid? ReviewerId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AppliedAt { get; set; }
}
public sealed class EmergencyHide
{
    public Guid Id { get; set; }
    public Guid OccurrenceId { get; set; }
    public Guid PromoterUserId { get; set; }
    public string Reason { get; set; } = "";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public HideReviewStatus ReviewStatus { get; set; } = HideReviewStatus.Pending;
}
public sealed class OccurrenceException
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public DateOnly OriginalLocalDate { get; set; }
    public ExceptionKind Kind { get; set; }
    public DateOnly? ReplacementLocalDate { get; set; }
    public Guid ApprovedRevisionId { get; set; }
}
public sealed class Comedian
{
    public Guid Id { get; set; }
    public string StageName { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Bio { get; set; }
    public string? PhotoKey { get; set; }
    public string? HomeCity { get; set; }
    public string? CountryCode { get; set; }
    public string? PrimaryLanguage { get; set; }
    public string? InstagramUrl { get; set; }
    public string? TikTokUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public ComedianProfileStatus ProfileStatus { get; set; } = ComedianProfileStatus.Draft;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class EventPerformer
{
    public Guid OccurrenceId { get; set; }
    public Guid ComedianId { get; set; }
    public short BillingOrder { get; set; }
}
public sealed class ModerationAction
{
    public Guid Id { get; set; }
    public string SubjectType { get; set; } = "";
    public Guid SubjectId { get; set; }
    public string Action { get; set; } = "";
    public Guid ReviewerId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class EventReport
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid? OccurrenceId { get; set; }
    public string Reason { get; set; } = "";
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
public sealed class NewsletterSubscriber
{
    public Guid Id { get; set; }
    public string EmailNormalized { get; set; } = "";
    public string CityCode { get; set; } = "";
    public string Language { get; set; } = "en";
    public DateTime ConsentAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? UnsubscribedAt { get; set; }
    public string? ConfirmationTokenHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class ImportSource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string TermsUrl { get; set; } = "";
    public bool PermittedInternalUse { get; set; }
    public string? PermissionNotes { get; set; }
    public DateTime CheckedAt { get; set; }
    public bool IsEnabled { get; set; }
}
public sealed class ImportCandidate
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public string ExternalId { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string PermittedMetadataJson { get; set; } = "{}";
    public Guid? PromoterClaimId { get; set; }
    public DateTime FetchedAt { get; set; }
}
