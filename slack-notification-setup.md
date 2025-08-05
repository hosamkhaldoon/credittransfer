# 📧 Slack Notification Setup for GitHub Actions

## 🎯 Overview
This guide shows how to configure Slack notifications for your GitHub Actions CI/CD pipeline.

## 🔧 Setup Steps

### 1. Create Slack Webhook URL

#### Option A: Slack Apps (Recommended)
1. Go to [Slack API: Your Apps](https://api.slack.com/apps)
2. Click **"Create New App"** → **"From scratch"**
3. Name your app (e.g., "GitHub CI/CD Bot")
4. Select your workspace
5. Go to **"Incoming Webhooks"** in the sidebar
6. Turn on **"Activate Incoming Webhooks"**
7. Click **"Add New Webhook to Workspace"**
8. Choose the channel for notifications
9. Copy the webhook URL (starts with `https://hooks.slack.com/services/`)

#### Option B: Legacy Webhooks
1. Go to your Slack workspace
2. Navigate to **Apps** → **Manage** → **Custom Integrations**
3. Click **"Incoming WebHooks"**
4. Choose a channel and click **"Add Incoming WebHooks Integration"**
5. Copy the webhook URL

### 2. Add Secret to GitHub Repository

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **"New repository secret"**
4. Name: `SLACK_WEBHOOK_URL`
5. Value: Paste your webhook URL
6. Click **"Add secret"**

### 3. Test the Integration

1. **Trigger a workflow** (push to main or manual run)
2. **Check your Slack channel** for notifications
3. **Verify the message format** includes:
   - Pipeline status
   - Build results for each job
   - Version and branch information
   - Direct link to GitHub Actions run

## 📊 What You'll Get

### Notification Content
```
🚀 CreditTransfer.Modern CI/CD Pipeline

📊 Status: success
🏗️ Build: success
🧪 Integration Tests: Skipped (DB issues)
🐳 Docker: success
🔒 Security: success
⚡ Performance: skipped

📋 Version: 1.0.42
🌿 Branch: main
👤 Actor: your-username

🔗 View Results: [Direct link to GitHub Actions run]
```

### Notification Triggers
- ✅ **Always sent** regardless of pipeline success/failure
- 📊 **Includes all job statuses** (success, failed, skipped)
- 🔄 **Sent at pipeline completion** (after all jobs finish)
- 🎯 **Contains direct links** to view detailed results

## 🛠️ Customization Options

### Change Notification Channel
1. Update the webhook URL to point to a different channel
2. Or create multiple webhook URLs for different channels

### Modify Notification Content
Edit the Slack notification step in `.github/workflows/ci-build-test.yml`:

```yaml
- name: "📧 Notify Slack"
  if: always() && secrets.SLACK_WEBHOOK_URL != ''
  uses: 8398a7/action-slack@v3
  with:
    status: ${{ job.status }}
    fields: repo,message,commit,author,action,eventName,ref,workflow
    # Add custom fields here
    custom_payload: |
      {
        "text": "Custom message here",
        "blocks": [
          {
            "type": "section",
            "text": {
              "type": "mrkdwn",
              "text": "Custom notification format"
            }
          }
        ]
      }
  env:
    SLACK_WEBHOOK_URL: ${{ secrets.SLACK_WEBHOOK_URL }}
  continue-on-error: true
```

## 🔍 Troubleshooting

### No Notifications Received
1. **Check webhook URL** - Ensure it's correct in repository secrets
2. **Verify channel permissions** - Ensure the webhook has access to the channel
3. **Test webhook manually**:
   ```bash
   curl -X POST -H 'Content-type: application/json' \
     --data '{"text":"Test message"}' \
     YOUR_WEBHOOK_URL
   ```

### Partial Information in Notifications
- **Missing job results**: Check that all jobs are properly defined with outputs
- **Incorrect status**: Verify the job dependencies and conditional logic

### Webhook URL Errors
- **Invalid webhook**: Regenerate the webhook URL in Slack
- **Expired webhook**: Some webhooks expire, create a new one
- **Wrong format**: Ensure URL starts with `https://hooks.slack.com/services/`

## 🎯 Alternative Notification Methods

### Email Notifications
If you prefer email over Slack, you can modify the workflow to use email actions instead:

```yaml
- name: "📧 Email Notification"
  uses: dawidd6/action-send-mail@v3
  with:
    server_address: smtp.gmail.com
    server_port: 465
    username: ${{ secrets.EMAIL_USERNAME }}
    password: ${{ secrets.EMAIL_PASSWORD }}
    subject: "CI/CD Pipeline Result: ${{ job.status }}"
    body: "Pipeline completed with status: ${{ job.status }}"
    to: your-email@example.com
```

### Teams Notifications
For Microsoft Teams:

```yaml
- name: "📧 Teams Notification"
  uses: skitionek/notify-microsoft-teams@master
  with:
    webhook_url: ${{ secrets.TEAMS_WEBHOOK_URL }}
    overwrite: |
      {
        "text": "Pipeline Status: ${{ job.status }}"
      }
```

## ✅ Verification

After setup, you should see:
1. ✅ **Slack notifications** in your chosen channel
2. ✅ **No more "Specify secrets.SLACK_WEBHOOK_URL" errors**
3. ✅ **Detailed pipeline status** in each notification
4. ✅ **Direct links** to GitHub Actions runs for detailed investigation

The notification system is now **fault-tolerant** - if Slack is not configured, the workflow will continue successfully and show a summary in the GitHub Actions UI instead. 