namespace CustomerReturnCRM.Domain.Entities;

public enum SmsCampaignStatus
{
    Scheduled = 1,
    Sending = 2,
    Completed = 3,
    PartiallyFailed = 4,
    Failed = 5,
    Cancelled = 6
}
