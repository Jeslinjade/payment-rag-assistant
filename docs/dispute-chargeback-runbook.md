DISPUTE AND CHARGEBACK RESOLUTION RUNBOOK

What is a chargeback?
A chargeback happens when a cardholder contacts their bank to reverse a transaction,
rather than requesting a refund directly from us. The bank provisionally debits our
account and gives us a window to respond with evidence.

Step 1: Identify the dispute reason code
Common reason codes:
- "Fraudulent transaction" - cardholder claims they did not authorize the charge.
- "Product not received" - cardholder claims they paid but never got the goods/service.
- "Duplicate processing" - cardholder was charged twice for the same order.
- "Credit not processed" - a refund was promised but never appeared on the statement.

Step 2: Gather evidence within the response window
For "fraudulent transaction" disputes, gather: IP address at time of purchase, device
fingerprint, AVS/CVV match results, and any prior order history for that customer.
For "product not received" disputes, gather: shipping/delivery confirmation, tracking
number, or service access logs.
For "duplicate processing" disputes, gather: transaction IDs for both charges and
confirm via the ledger whether one was already voided.

Step 3: Submit the response before the deadline
Each card network sets its own response window - typically 7 to 20 days from when
the chargeback was raised, depending on the network and dispute reason. Missing the
deadline results in an automatic loss, so track deadlines in the dispute queue and
flag anything within 2 days of expiry as urgent.

Step 4: Track the outcome
Outcomes are one of: Won (funds returned to us), Lost (cardholder keeps the funds and
we absorb the loss), or Re-presented (the bank contests our response and it goes to
a second round, arbitration in rare cases).

Step 5: Root-cause repeat disputes
If the same reason code is spiking for a specific merchant category or time window,
flag it to the fraud team - this is often correlated with either a compromised card
batch or an actual issue in the product/fulfillment flow.
