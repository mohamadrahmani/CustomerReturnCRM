export const SmsCampaignStatus = {
  Scheduled: 1,
  Sending: 2,
  Completed: 3,
  PartiallyFailed: 4,
  Failed: 5,
  Cancelled: 6,
} as const;

export type SmsCampaignStatus = typeof SmsCampaignStatus[keyof typeof SmsCampaignStatus];

export const SmsRecipientStatus = {
  Pending: 1,
  Submitted: 2,
  Delivered: 3,
  Failed: 4,
} as const;

export type SmsRecipientStatus = typeof SmsRecipientStatus[keyof typeof SmsRecipientStatus];

export const SMS_MESSAGE_MAX_LENGTH = 2000;
export const SMS_CAMPAIGN_MAX_RECIPIENTS = 10000;

export const SMS_VARIABLES = [
  { token: "[نام]", label: "نام" },
  { token: "[نام خانوادگی]", label: "نام خانوادگی" },
  { token: "[نام کامل]", label: "نام کامل" },
  { token: "[نام کسب‌وکار]", label: "نام کسب‌وکار" },
] as const;

export function smsCampaignStatusLabel(status: SmsCampaignStatus) {
  switch (status) {
    case SmsCampaignStatus.Scheduled: return "زمان‌بندی شده";
    case SmsCampaignStatus.Sending: return "در حال ارسال";
    case SmsCampaignStatus.Completed: return "ارسال شده";
    case SmsCampaignStatus.PartiallyFailed: return "بخشی ناموفق";
    case SmsCampaignStatus.Failed: return "ناموفق";
    case SmsCampaignStatus.Cancelled: return "لغو شده";
  }
}

export function smsRecipientStatusLabel(status: SmsRecipientStatus) {
  switch (status) {
    case SmsRecipientStatus.Pending: return "در انتظار";
    case SmsRecipientStatus.Submitted: return "تحویل به سرویس‌دهنده";
    case SmsRecipientStatus.Delivered: return "تحویل شده";
    case SmsRecipientStatus.Failed: return "ناموفق";
  }
}
