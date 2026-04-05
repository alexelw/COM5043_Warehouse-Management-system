export type StatusTone = 'neutral' | 'success' | 'warning' | 'danger' | 'info';

export function toStatusTone(status: string): StatusTone {
  switch (status) {
    case 'Completed':
    case 'Confirmed':
    case 'Posted':
    case 'Healthy':
      return 'success';
    case 'Pending':
    case 'Draft':
    case 'PartiallyReceived':
    case 'Low stock':
      return 'warning';
    case 'Cancelled':
    case 'Voided':
      return 'danger';
    case 'Reversed':
      return 'info';
    default:
      return 'neutral';
  }
}
