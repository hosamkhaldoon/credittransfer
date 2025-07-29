# 📧 Jenkins Email Notification Setup Guide

## 🎯 **Configure SMTP for Email Notifications**

### **Step 1: Jenkins System Configuration**

1. **Access Jenkins**: http://localhost:8080
2. **Navigate**: Dashboard → Manage Jenkins → System
3. **Scroll down** to "Extended E-mail Notification" section

### **Step 2: Gmail SMTP Configuration (Recommended)**

#### **📋 Gmail Settings:**
```
SMTP Server: smtp.gmail.com
SMTP Port: 587
Use SMTP Authentication: ✓ (checked)
Username: your-email@gmail.com
Password: [App Password - see below]
Use SSL: ✓ (checked)
Use TLS: ✓ (checked)
```

#### **🔑 Generate Gmail App Password:**
1. Go to: https://myaccount.google.com/security
2. Enable **2-Factor Authentication** (required)
3. Go to **App Passwords** 
4. Generate password for "Jenkins"
5. **Copy the 16-character password** (use this in Jenkins)

### **Step 3: Alternative Email Providers**

#### **📧 Outlook/Hotmail SMTP:**
```
SMTP Server: smtp-mail.outlook.com
SMTP Port: 587
Username: your-email@outlook.com
Password: [Your password]
Use TLS: ✓ (checked)
```

#### **📧 Yahoo SMTP:**
```
SMTP Server: smtp.mail.yahoo.com
SMTP Port: 587
Username: your-email@yahoo.com
Password: [App Password required]
Use TLS: ✓ (checked)
```

### **Step 4: Jenkins Configuration Steps**

1. **Extended E-mail Notification Section:**
   - SMTP Server: `smtp.gmail.com`
   - SMTP Port: `587`
   - Click "Advanced..."
   - ✓ Use SMTP Authentication
   - Username: `hosam93644@gmail.com`
   - Password: `[Your App Password]`
   - ✓ Use SSL
   - ✓ Use TLS

2. **Default Content Settings:**
   - Default Subject: `$PROJECT_NAME - Build #$BUILD_NUMBER - $BUILD_STATUS`
   - Default Content: 
   ```
   Build Status: $BUILD_STATUS
   Project: $PROJECT_NAME
   Build Number: $BUILD_NUMBER
   Build URL: $BUILD_URL
   ```

3. **E-mail Notification Section (Legacy):**
   - SMTP Server: `smtp.gmail.com`
   - ✓ Use SMTP Authentication
   - Username: `hosam93644@gmail.com`
   - Password: `[Your App Password]`
   - ✓ Use SSL
   - SMTP Port: `465` (for legacy email)

### **Step 5: Test Email Configuration**

1. **Scroll to bottom** of System Configuration
2. Click **"Test configuration by sending test e-mail"**
3. Enter: `hosam93644@gmail.com`
4. Click **"Test configuration"**
5. Check your inbox for test email

### **Step 6: Save Configuration**
- Click **"Save"** at bottom of page

## 🚀 **Quick Setup Commands**

### **Method 1: Jenkins UI (Recommended)**
Use the steps above through the web interface.

### **Method 2: Jenkins CLI (Advanced)**
```bash
# Access Jenkins container
docker exec -it jenkins-with-docker bash

# Edit Jenkins configuration file
vim /var/jenkins_home/jenkins.model.JenkinsLocationConfiguration.xml
```

## 🔧 **Troubleshooting Email Issues**

### **Common Problems:**

1. **"535 Authentication failed"**
   - Use App Password, not regular password
   - Enable 2FA on Gmail account

2. **"Connection timeout"**
   - Check firewall settings
   - Try port 465 instead of 587

3. **"SSL handshake failed"**
   - Ensure both SSL and TLS are enabled
   - Try different SMTP ports

### **Test Email Manually:**
```bash
# From Jenkins container
docker exec jenkins-with-docker curl -v smtp://smtp.gmail.com:587
```

## 📧 **Expected Success Message**
After configuration:
```
✅ Email sent successfully to hosam93644@gmail.com
Test email configuration successful
```

## 🎯 **Final Verification**
1. Trigger a Jenkins build
2. Check for email notification in inbox
3. Verify build status in email content

---

## 📝 **Notes**
- Gmail requires App Passwords (not regular passwords)
- 2-Factor Authentication must be enabled for Gmail
- Corporate firewalls may block SMTP ports
- Use port 587 (TLS) or 465 (SSL) for Gmail 