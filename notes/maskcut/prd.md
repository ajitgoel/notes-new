**MaskCut**

On-Device Batch Face & Screen Redactor

Product Requirements Document · v1.0 MVP

*30-Day Delivery Target*

|  |  |
| --- | --- |
| **Status** | Draft — In Review |
| **Target Platform** | macOS (Apple Silicon) |
| **Tech Stack** | Swift + SwiftUI, Apple Vision, Core ML, AVFoundation, ffmpeg |
| **Licensing** | Paddle / Lemon Squeezy |
| **Update Mechanism** | Sparkle |
| **Knowledge Cutoff** | February 2025 |

**"***Redact faces and sensitive content from your videos on your Mac.
No upload. No cloud. No per-minute fees.***"**

## **1. Problem Statement**

Teams that handle sensitive video footage — law enforcement, healthcare organizations, insurance adjusters, school districts, and legal professionals — must routinely redact faces and on-screen text before sharing or archiving footage. Today, they face an impossible choice:

* Upload footage to cloud services (Redactor.ai, AWS Rekognition, Google Video Intelligence) at $0.10–$0.50/minute, creating data sovereignty and compliance risks.
* Manually blur faces in iMovie or Premiere, a painstaking frame-by-frame process that doesn't scale.
* Engage external agencies for redaction work, introducing cost, delays, and additional custody concerns.

|  |  |
| --- | --- |
| **💡** | A security firm processing 500 hours of bodycam footage per month pays $3,000–$15,000/month in cloud fees alone — before addressing the fundamental problem that many customers legally cannot upload raw footage to any third-party cloud. |

For these users, uploading raw footage to any cloud is not merely inconvenient — it is the risk they are trying to eliminate. A local-first, batch-capable redaction tool that runs entirely on-device closes this gap completely.

## **2. Solution**

MaskCut is a native macOS application for Apple Silicon that redacts faces and on-screen text from videos entirely on-device. Users import a folder of videos, configure redaction settings once, preview a sample frame, and batch process all files while they walk away. Redacted copies are written to a /Redacted subfolder; originals are never modified. A structured JSON audit log is generated per batch.

The core value proposition: drop 50 bodycam clips in, configure once, walk away, come back to 50 redacted files.

|  |  |
| --- | --- |
| **🔒** | Architecture principle: Nothing leaves the device. No API keys. No upload. No per-minute metering. The Neural Engine on Apple Silicon handles detection at a fraction of the cost of cloud services. |

## **3. User Stories**

### **3.1 Import & File Management**

1. As a paralegal, I want to drag a folder of video files onto the app, so that I can start processing without navigating a file picker.
2. As a training coordinator, I want to import individual files alongside full folders, so that I can handle mixed batches in a single session.
3. As a compliance manager, I want the app to automatically discover supported video formats in imported folders, so that I don't need to pre-filter files.
4. As a video analyst, I want to see a file list with thumbnail, duration, and file size for each import, so that I can verify the right files are queued.
5. As a user, I want to remove individual files from the queue before processing, so that I can exclude files that don't need redaction.

### **3.2 Configuration**

1. As a security analyst, I want to choose between 'Faces', 'Screens/Text', or 'Both' redaction modes, so that I apply only the processing relevant to my footage.
2. As a user, I want to select blur strength (light, medium, heavy), so that redaction is visually appropriate for my use case.
3. As a power user, I want to configure the frame-sampling interval (N frames), so that I can trade processing speed for detection smoothness.
4. As a user, I want my settings to persist between sessions, so that I don't reconfigure on every launch.

### **3.3 Preview**

1. As an insurance adjuster, I want to preview redaction applied to the first frame of each video before committing to a full batch, so that I can verify detection accuracy.
2. As a user, I want the preview to render in under 5 seconds per file, so that I can quickly check a large batch.
3. As a user, I want to adjust blur strength in the preview without re-importing, so that I can dial in settings before processing.

### **3.4 Batch Processing**

1. As a school media coordinator, I want to click 'Process All' and have the app handle the entire batch unattended, so that I can focus on other work.
2. As a user, I want a per-file progress bar and an overall batch progress indicator, so that I have visibility into processing status.
3. As a user, I want to see estimated time remaining for the batch, so that I can plan around the processing window.
4. As a user, I want the app to gracefully handle a failed file and continue processing the rest, so that one corrupted clip doesn't block the batch.
5. As a user, I want the app to run efficiently in the background, so that I can use my Mac for other tasks during processing.

### **3.5 Output & Audit**

1. As a compliance officer, I want redacted files output to a /Redacted subfolder alongside the originals, so that source footage is never modified or overwritten.
2. As an auditor, I want a JSON log per batch containing source filename, duration, frame count, detection count, processing time, model version, and app version, so that I have a tamper-evident record of what was processed.
3. As a user, I want redacted files to preserve the original codec and bitrate, so that downstream editing workflows are not disrupted.
4. As a user, I want a summary screen after batch completion showing success/failure counts and output location, so that I can confirm the run completed.

### **3.6 Trust & Transparency**

1. As a user, I want a persistent UI notice that reads 'AI-assisted redaction — always review outputs before sharing', so that I understand the limitations of automated detection.
2. As a manager, I want the audit log to include confidence thresholds used, so that my team can identify frames warranting manual review.

## **4. Implementation Decisions**

### **4.1 Module Architecture**

The pipeline separates into two independent, testable stages with a persisted intermediate format:

* Detection Pipeline: Frame extraction (AVAssetImageGenerator) → Vision requests → bounding box collection → bounding box smoothing → JSON persisted bounding box manifest per file.
* Rendering Pipeline: Reads persisted bounding box manifest → invokes ffmpeg with Gaussian blur filter over bounding box regions → re-encodes with original codec/bitrate → writes to /Redacted subfolder.

|  |  |
| --- | --- |
| **⚙️** | Key architectural benefit: If the ffmpeg render fails, detection does not need to re-run. The persisted manifest also enables a future 'review mode' (v1.1) where users inspect detected regions before committing to render — without any pipeline restructuring. |

### **4.2 Detection Modules**

Face detection uses Apple Vision VNDetectFaceRectanglesRequest, running on the Neural Engine on Apple Silicon with no model download required. Screen/text detection combines VNRecognizeTextRequest for text regions with a lightweight Core ML model (MobileNetV2-based, fine-tuned on 'monitor/screen' class via Create ML) for physical screen detection.

Frame sampling defaults to every 4th frame (N=4), with bounding box interpolation between keyframes for smooth redaction at reduced compute cost. N is configurable via settings.

### **4.3 Batch Queue**

An async Swift actor-based queue manages file processing order, concurrency limits, and error isolation. The same pattern as ClipForge applies. Each file runs in its own actor context; errors are caught per-file and reported in the summary without halting the batch.

### **4.4 Audit Log Schema**

Each batch produces a Codable struct serialized to JSON alongside the /Redacted output. Fields: source\_filename, duration\_seconds, frame\_count, detections\_count, confidence\_threshold, processing\_time\_seconds, model\_version, app\_version, timestamp\_utc.

### **4.5 Licensing & Updates**

Licensing via Paddle or Lemon Squeezy — both support invoice-style receipts expected by B2B buyers. App updates via Sparkle.

## **5. Testing Decisions**

### **5.1 Testing Philosophy**

Tests validate external behavior — inputs and outputs — not implementation details or internal state. A good test asserts that given a video file with N visible faces, the bounding box manifest contains N detection events with confidence above threshold. It does not assert which Vision API method was called or how many intermediate frames were sampled.

### **5.2 Modules to Test**

* Detection Pipeline: Given a fixture video with known face positions, assert the output bounding box manifest contains correctly-positioned, temporally-smoothed bounding boxes.
* Bounding Box Smoother: Given a sparse set of keyframe bounding boxes, assert the interpolated sequence produces geometrically correct intermediate positions.
* Render Pipeline: Given a bounding box manifest and input video, assert the output video file exists, matches codec/bitrate of input, and pixel regions corresponding to bounding boxes differ from the source.
* Audit Log: Given a completed batch, assert the JSON log is well-formed, contains all required fields, and detection counts match pipeline output.
* Batch Queue: Given a mixed batch of valid and intentionally-corrupted files, assert valid files complete successfully and corrupted files are isolated without halting the queue.

### **5.3 Test Data**

Maintain a small fixture library of synthetic videos (programmatically generated, not sourced from real footage) with known face counts and positions, used exclusively for unit and integration tests.

## **6. Pricing**

Pricing is designed to make the cloud cost comparison obvious at a glance. The target buyer calculates their per-minute cloud spend and arrives at MaskCut already sold.

| **Tier** | **Monthly** | **Annual** | **Seats** | **Key Differentiator** |
| --- | --- | --- | --- | --- |
| Free Trial | – | – | 1 | Fully functional on videos under 60 seconds |
| Solo | $29 | $199 | 1 | Unlimited minutes, single machine, personal use |
| Team | $79 | $599 | 5 | Priority support, volume processing |
| Agency | $149 | – | 10 | White-label report headers, dedicated support |

|  |  |
| --- | --- |
| **📊** | A security firm processing 500 hours/month via cloud redaction at $0.10/min pays ~$3,000/month. MaskCut Team is $79/month. The math sells itself — and that's before the regulatory argument. |

Positioning avoidance: Do not market as 'compliance software' — this creates legal liability. Position as 'the privacy tool for teams that handle sensitive video.' Compliance responsibility remains with the user, which is correct and legally appropriate.

## **7. Go-to-Market Strategy (Days 0–60)**

### **7.1 Target Buyer Insight**

The decision-maker is often not the CISO. It is the person doing the actual redaction work: the paralegal at a PI firm, the training coordinator at a hospital, the insurance adjuster, the school media coordinator. These individuals hate their current workflow and will champion a tool that makes their job tangibly easier. Find and serve these people first.

### **7.2 Week 1–2: Community Seeding**

Post in targeted subreddits with authentic, problem-first framing:

* r/privacytoolsio and r/privacy
* r/legal
* r/videography
* r/k12sysadmin
* r/securityguards and r/bodycameras

### **7.3 Week 2–3: Direct Outreach**

Identify 20–30 small PI firms, insurance agencies, and security consultancies. DM individuals with titles like 'Video Analyst', 'Paralegal', 'Training Coordinator', or 'Compliance Manager'. Offer 3 months free access in exchange for 15 minutes of feedback. Personalize each outreach message.

### **7.4 Week 3: Product Hunt**

Focus PH copy on the privacy angle, not the video editing angle. Lead with: 'Redact faces from videos on your Mac. Nothing leaves your device.' The privacy-tech community on PH is engaged and vocal.

### **7.5 Weeks 4–8: Content Marketing**

Publish 'The hidden risk of using cloud tools to blur faces in sensitive footage' across Medium, Hacker News, and LinkedIn. This is the top-of-funnel piece that drives organic search for 'local face redaction video'. Pitch privacy-focused newsletters: Privacy Guides, The Privacy Advisor, Risky Business.

### **7.6 Partnership Opportunities**

* Cross-promotion with complementary Mac tools: ScreenFloat, Claquette, Permute.
* Referral partnerships with privacy/security consultants who serve small firms — offer a structured referral fee.

## **8. Out of Scope (MVP)**

The following are explicitly deferred to post-MVP releases:

* Region-of-interest manual drawing — auto-detection does all work in v1.
* Per-person identity tracking — consistent redaction of the same individual across files.
* Audio redaction — speaker name bleeping.
* License plate detection — faces and screens only in MVP.
* Cloud sync of redaction logs.
* PDF report generation — JSON log is sufficient for v1.
* Missed-detection review UI (planned architecturally for v1.1, not shipped in MVP).
* Multi-seat collaboration dashboard.
* Premiere or FCPX plugin integration.
* Windows port.

## **9. Further Notes**

### **9.1 Key Risk: Vision Detection Limitations**

Apple Vision's VNDetectFaceRectanglesRequest has known limitations with small faces, extreme angles, and partially-obscured faces. This must be communicated clearly in the product UI — not buried in documentation. The persistent UI notice 'AI-assisted redaction — always review outputs before sharing' is a required MVP element, not optional copy.

### **9.2 v1.1 Roadmap Items (Planned)**

* Missed-detection review UI: surfaces frames where detection confidence fell below threshold for manual verification before render.
* Region-of-interest manual drawing: complements auto-detection for edge cases.
* PDF audit report generation.

### **9.3 One-Liner**

***"MaskCut redacts faces and sensitive content from your videos on your Mac. No upload. No cloud. No per-minute fees."***