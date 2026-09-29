DAILY RECONCILIATION STANDARD OPERATING PROCEDURE

Purpose
Reconciliation confirms that every transaction recorded in our internal ledger
matches what the payment processor and the bank settlement file report. Mismatches
indicate either a processing error, a timing difference, or a fraud signal.

Daily steps
1. Pull the processor's settlement report for the previous business day (usually
   available by 6 AM local time).
2. Pull the internal ledger export for the same date range.
3. Match records by transaction ID. Three possible outcomes per record:
   - Matched: amounts and status agree - no action needed.
   - Amount mismatch: same transaction ID but different amount - usually caused by
     a partial refund or currency conversion rounding. Flag for manual review.
   - Missing on one side: exists in the ledger but not the settlement file (or vice
     versa) - usually a timing difference if the transaction happened late in the
     day, but if it persists past 2 business days, escalate as a potential data loss
     or double-processing issue.

Common causes of mismatches
- Timezone cutoff differences between our ledger (UTC) and the processor's
  settlement window (often merchant local time).
- Currency conversion rounding on cross-border transactions.
- Refunds or voids processed after the settlement file was generated, appearing in
  the next day's file instead.
- Retried transactions where the retry succeeded but the original attempt's failure
  record wasn't cleaned up.

Escalation thresholds
- Under 0.1% mismatch rate: normal, log and continue.
- 0.1% to 1%: flag to the payments engineering team for pattern review.
- Over 1%: treat as an incident - page on-call, since this usually indicates a
  systemic issue like a processor outage or a bug in a recent deployment.
