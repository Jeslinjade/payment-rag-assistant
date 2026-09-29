TRANSACTION FAILURE CODES REFERENCE

Code 51 - Insufficient Funds
The cardholder's account does not have enough available balance to cover the
transaction amount. This is a decline from the issuing bank, not from our system.
Recommended action: Ask the customer to retry with a different payment method or
contact their bank. Do not retry the same card automatically - repeated attempts
can trigger fraud flags on the issuer's side.

Code 05 - Do Not Honor
A generic decline from the issuing bank with no specific reason given. Common causes
include suspected fraud, card restrictions, or the issuer's own risk rules.
Recommended action: Advise the customer to contact their card issuer directly, since
we do not receive more detail than this code.

Code 14 - Invalid Card Number
The card number failed the issuer's validation (often a Luhn check failure or a
number that doesn't exist). Usually caused by a typo during entry.
Recommended action: Ask the customer to re-enter their card details carefully.

Code 61 - Exceeds Withdrawal Limit
The transaction amount exceeds the cardholder's daily or per-transaction limit set
by their bank.
Recommended action: Suggest the customer retry with a smaller amount or contact
their bank to raise the limit temporarily.

Code 91 - Issuer Unavailable
The cardholder's bank system was unreachable at the time of authorization. This is
usually transient.
Recommended action: Retry the transaction after a short delay (our system already
retries automatically up to 3 times with exponential backoff for this code).

Code TIMEOUT - Gateway Timeout
Our payment gateway did not receive a response from the acquiring bank within the
configured timeout window (default 30 seconds).
Recommended action: Check acquirer status dashboard first. If the acquirer is healthy,
escalate to the payments engineering on-call rotation - this may indicate a network
issue between our gateway and the acquirer.
