# 🚀 Jenkins Email Fix: Bypass SMTP Blocks

## 🎯 **Problem: Corporate Firewall Blocks SMTP Ports**

Your corporate network blocks SMTP ports (587, 465, 25), preventing Jenkins from sending emails.

## ✅ **Solution 1: HTTP-Based Email Service (Recommended)**

### **Option A: Use SendGrid HTTP API**

1. **Sign up for SendGrid**: https://sendgrid.com (free tier: 100 emails/day)
2. **Get API Key**: SendGrid Dashboard → Settings → API Keys
3. **Install HTTP Request Plugin** in Jenkins:
   - Go to: Manage Jenkins → Plugins → Available
   - Search: "HTTP Request Plugin"
   - Install and restart

4. **Configure Jenkins Pipeline Email**:
```groovy
// In your Jenkinsfile, replace emailext with HTTP call
stage('📧 Send Notification') {
    steps {
        script {
            def emailData = [
                personalizations: [[
                    to: [[email: "hosam93644@gmail.com"]],
                    subject: "Jenkins Build #${env.BUILD_NUMBER} - ${currentBuild.currentResult}"
                ]],
                from: [email: "jenkins@leading-point.com"],
                content: [[
                    type: "text/plain",
                    value: """
Build Status: ${currentBuild.currentResult}
Project: ${env.JOB_NAME}
Build Number: ${env.BUILD_NUMBER}
Build URL: ${env.BUILD_URL}
                    """
                ]]
            ]
            
            httpRequest(
                httpMode: 'POST',
                url: 'https://api.sendgrid.com/v3/mail/send',
                customHeaders: [
                    [name: 'Authorization', value: 'Bearer YOUR_SENDGRID_API_KEY'],
                    [name: 'Content-Type', value: 'application/json']
                ],
                requestBody: groovy.json.JsonBuilder(emailData).toString()
            )
        }
    }
}
```

### **Option B: Use Mailgun HTTP API**

1. **Sign up for Mailgun**: https://mailgun.com (free tier: 5,000 emails/month)
2. **Get API Key**: Mailgun Dashboard → Settings → API Keys
3. **Use HTTP Request**:
```groovy
httpRequest(
    httpMode: 'POST',
    url: 'https://api.mailgun.net/v3/sandbox-xxx.mailgun.org/messages',
    authentication: 'mailgun-api-key', // Configure in Jenkins credentials
    requestBody: "from=jenkins@leading-point.com&to=hosam93644@gmail.com&subject=Build ${env.BUILD_NUMBER}&text=Build completed successfully"
)
```

## ✅ **Solution 2: Use Slack/Teams Notifications**

### **Slack Webhook (Easier Setup)**

1. **Create Slack Webhook**: https://api.slack.com/messaging/webhooks
2. **Add to Jenkins**:
```groovy
stage('📧 Slack Notification') {
    steps {
        script {
            def slackMessage = [
                text: "Jenkins Build Notification",
                attachments: [[
                    color: currentBuild.currentResult == 'SUCCESS' ? 'good' : 'danger',
                    fields: [
                        [title: "Project", value: env.JOB_NAME, short: true],
                        [title: "Build", value: env.BUILD_NUMBER, short: true],
                        [title: "Status", value: currentBuild.currentResult, short: true],
                        [title: "URL", value: env.BUILD_URL, short: false]
                    ]
                ]]
            ]
            
            httpRequest(
                httpMode: 'POST',
                url: 'YOUR_SLACK_WEBHOOK_URL',
                requestBody: groovy.json.JsonBuilder(slackMessage).toString(),
                contentType: 'APPLICATION_JSON'
            )
        }
    }
}
```

## ✅ **Solution 3: Simple File-Based Notifications**

**If external APIs are also blocked**, create local notifications:

```groovy
// In your Jenkinsfile post section
post {
    always {
        script {
            def status = currentBuild.currentResult
            def timestamp = new Date().format('yyyy-MM-dd HH:mm:ss')
            def notification = """
=== JENKINS BUILD NOTIFICATION ===
Time: ${timestamp}
Project: ${env.JOB_NAME}
Build: #${env.BUILD_NUMBER}
Status: ${status}
URL: ${env.BUILD_URL}
Branch: ${env.BRANCH_NAME}
Commit: ${env.GIT_COMMIT}
================================
"""
            writeFile file: "build-notification-${env.BUILD_NUMBER}.txt", text: notification
            archiveArtifacts artifacts: "build-notification-${env.BUILD_NUMBER}.txt"
            
            // Also write to a shared location if mounted
            sh "echo '${notification}' >> /shared/jenkins-notifications.log"
        }
    }
}
```

## ✅ **Solution 4: Fix Docker Networking (Advanced)**

**Restart Jenkins with host networking** to bypass Docker network restrictions:

```bash
# Stop current Jenkins
docker stop jenkins-with-docker

# Start with host networking (bypasses Docker network isolation)
docker run -d --name jenkins-host-network \
  --network host \
  -v jenkins_home:/var/jenkins_home \
  -v /var/run/docker.sock:/var/run/docker.sock \
  jenkins/jenkins:lts

# Access Jenkins at: http://localhost:8080
```

## 🎯 **Immediate Action Plan**

### **Quick Fix (5 minutes):**
1. **Install HTTP Request Plugin** in Jenkins
2. **Sign up for SendGrid** (free)
3. **Get API Key** from SendGrid
4. **Update Jenkinsfile** with HTTP email calls

### **Alternative (2 minutes):**
1. **Create Slack webhook**
2. **Update Jenkinsfile** with Slack notifications

### **Emergency Fix:**
1. **Use file-based notifications** (no external dependencies)
2. **Archive notification files** in Jenkins

## 📧 **Expected Result**
```
✅ Build notification sent via SendGrid API
✅ Email delivered to hosam93644@gmail.com
✅ No SMTP ports required
```

**Choose SendGrid HTTP API - it's the fastest solution that bypasses all firewall restrictions!** 