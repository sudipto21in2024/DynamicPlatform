# Notification Engine & Asset Management Architecture

## 1. Executive Summary
This document outlines the architecture for a unified **Communication & Content Subsystem**. This subsystem fulfills two critical platform needs:
1.  **Notification Engine**: A robust, multi-channel system (Email, SMS, In-App, Push) with a visual template designer.
2.  **Asset Management (DAM)**: A centralized repository for managing uploading, optimizing, and serving static assets (Images, Documents) backed by a pluggable storage layer.

## 2. Business Capabilities

### 2.1 Universal Notification Hub
| Capability | Description |
| :--- | :--- |
| **Visual Template Designer** | Drag-and-drop builder for responsive specific emails (MJML-based) and simple text alerts. |
| **Multi-Channel Support** | Single API to send to **Email (SMTP/SendGrid)**, **SMS (Twilio)**, **Push (Firebase)**, and **In-App (SignalR)**. |
| **Dynamic Personalization** | "Hello `{{User.FirstName}}`", injecting data from platform entities into templates. |
| **Batch & Bulk Sending** | Job-based processing for newsletters or mass alerts. |

### 2.2 Digital Asset Management (DAM)
| Capability | Description |
| :--- | :--- |
| **Unified Upload UI** | A reusable "File Picker" component for all apps. |
| **Storage Abstraction** | Switch providers (AWS S3, Azure Blob, MinIO, Local Disk) without code changes. |
| **Image Optimization** | auto-generation of thumbnails and web-optimized formats (WebP). |
| **Access Control** | Private vs. Public assets with signed URL support. |

## 3. Technical Architecture

### 3.1 Storage Connector Layer (The Foundation)
To support multiple cloud providers, we implement the **Adapter Pattern**.

**Interface Definition (`IStorageProvider`):**
```csharp
public interface IStorageProvider
{
    Task<string> UploadAsync(Stream stream, string fileName, string container, bool isPublic);
    Task<Stream> DownloadAsync(string fileId);
    Task DeleteAsync(string fileId);
    string GetPublicUrl(string fileId);
    string GetSignedUrl(string fileId, TimeSpan expiry); // For private assets
}
```

**Connectors:**
1.  **AzureBlobProvider**: Uses `Azure.Storage.Blobs`.
2.  **S3Provider**: Uses `AWSSDK.S3` (Works for AWS, MinIO, DigitalOcean Spaces).
3.  **LocalStorageProvider**: Uses physical disk (Development/On-Prem).

### 3.2 Asset Manager (DAM)
**Entity Model (`Asset`):**
```json
{
  "assetId": "guid",
  "originalName": "profile.jpg",
  "storageProvider": "S3",
  "storagePath": "tenants/t1/images/profile.jpg",
  "mimeType": "image/jpeg",
  "sizeBytes": 10240,
  "isPublic": true,
  "metadata": {
    "width": 800,
    "height": 600,
    "altText": "User Profile"
  }
}
```

**Key Workflows:**
*   **Upload**: Stream -> Virus Scan (Optional) -> Image Resizer (if Image) -> Storage Provider -> DB Record.
*   **Usage**: UI components reference `AssetId`. The API resolves this to a CDN URL or Signed URL at runtime.

### 3.3 Notification Engine
**Template Model (`NotificationTemplate`):**
```json
{
  "templateId": "guid",
  "name": "OrderConfirmation",
  "channel": "Email", // or SMS, InApp
  "engine": "Liquid", // or Scriban
  "subjectTemplate": "Order #{{Order.Id}} Confirmed",
  "bodyContent": "<html>...</html>", // MJML or HTML
  "metadata": {
    "requiredVariables": ["Order", "User"]
  }
}
```

**The Sending Pipeline:**
1.  **Trigger**: Workflow or API calls `Notify("OrderConfirmation", { Order: orderObj })`.
2.  **Render**: Engine compiles `bodyContent` with data.
3.  **Dispatch**: Route to appropriate provider (e.g., SMTP Client).
4.  **Log**: Create `NotificationLog` entry (Status: Sent/Failed, RetryCount).

## 4. Visual Designers

### 4.1 Email Template Designer
*   **Technology**: **MJML** (Mailjet Markup Language) transpiled to responsive HTML.
*   **UI**:
    *   **Canvas**: React/Angular wrapper around MJML renderer.
    *   **Blocks**: "Image", "Text", "Button", "Social Links".
    *   **Variable Picker**: Tree view of platform Entities to drag-and-drop properties (e.g., `{{Customer.Email}}`).
    *   **Asset Integration**: Clicking "Image Block" opens the **Asset Picker Modal**.

### 4.2 Asset Picker Modal
*   **Features**: Grid view of images, Drag-and-drop upload zone, Search/Filter.
*   **Integration**: Returns the `AssetUrl` to the parent designer.

## 5. Missing Scenarios & Risk Mitigation

### 5.1 Dynamic Attachments (Reports)
*   **Scenario**: "Send the Monthly Invoice PDF to the user."
*   **Solution**: The Notification API must accept `List<Attachment>`.
    *   *Integration*: The Report Engine generates a Stream -> Notification Engine attaches it as a file.

### 5.2 Throttling & Anti-Spam
*   **Risk**: A loop in a workflow triggers 10,000 emails in 1 minute.
*   **Mitigation**: Implement **Rate Limiting** per Tenant/User (e.g., Max 50 emails/minute). Queue excess messages or reject with 429.

### 5.3 Storage Costs & Cleanup
*   **Risk**: Users upload terabytes of unreferenced images.
*   **Mitigation**: Implement **Reference Counting**. When an Entity referencing an Image is deleted, decrement count. If 0, mark Asset for "Garbage Collection".

### 5.4 Private Assets Security
*   **Scenario**: HR Documents shouldn't be accessible via public URL.
*   **Solution**: Flag `IsPublic: false`. The File API checks permissions (`CanReadEntity`) before generating a temporary SAS/Signed URL (valid for 15 mins).

## 6. Detailed Implementation Roadmap

### Phase 1: Storage & Assets (Core)
1.  Define `IStorageProvider` and implement `LocalStorageProvider` & `S3Provider`.
2.  Create `Asset` entity and `AssetService`.
3.  Build the **Upload Endpoint** (multipart/form-data).
4.  Build the **Asset Picker UI** component.

### Phase 2: Notification Backend
1.  Define `NotificationTemplate` entity.
2.  Implement `TemplateRenderer` (Scriban/Liquid).
3.  Implement `EmailSender` (SMTP) and `SmsSender` (Twilio stub).
4.  Create the `NotificationQueue` (Producer/Consumer) for async sending.

### Phase 3: Visual Template Designer
1.  Integrate MJML Editor into Platform Studio.
2.  Implement "Send Test Email" feature.
3.  Connect Asset Picker to the Email Designer.

### Phase 4: Advanced Features
1.  **In-App Notifications Box**: A UI widget showing list of alerts for the logged-in user.
2.  **Bounce Handling**: Webhook to mark emails as invalid if they bounce.
