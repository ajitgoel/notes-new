##### **Error responses (RFC 7807 problem+json)** 
Instead of returning vague errors, return a structured JSON object:
```json
{
  "type": "https://api.example.com/errors/prescription-conflict",
  "title": "Duplicate Prescription",
  "status": 409,
  "detail": "Patient 42 already has an active prescription for Lisinopril 10mg.",
  "instance": "/patients/42/prescriptions"
}
```

  Every error has the same shape, so your frontend can handle them consistently. `type` is a URL that identifies _which kind_ of error this is (your docs can explain it). `detail` gives the human-readable specifics.
##### **Idempotency keys on POST** 
POST creates new resources, but what if the network glitches? The client doesn’t know if the request went through, so it retries — and now you might get two identical prescriptions.
**How idempotency keys fix this:**
The client generates a unique ID (a UUID) and sends it as a header:
```
    POST /patients/42/prescriptions
    Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000
    Body: { "drug": "Lisinopril", "dosage": "10mg" }
```

==The server creates the prescription and saves that key.==
==If the client retries with the **same** `Idempotency-Key`, the server recognizes it and returns the original response instead of creating a duplicate.==

The client can safely retry as many times as needed without worrying about creating duplicates. This is especially important in healthcare/financial systems where a duplicate prescription or double charge would be a serious problem.