# 🏢 Corporate Email SMTP Configuration for Jenkins

## 🎯 **Leading Point Email Configuration**

### **Step 1: Get SMTP Settings from Your IT Department**

Contact your IT team for these settings:
```
SMTP Server: mail.leading-point.com (or smtp.leading-point.com)
SMTP Port: 587 or 25 or 465
Authentication: Usually required
Username: husami@leading-point.com
Password: [Your email password]
Security: TLS or SSL (ask IT team)
```

### **Step 2: Common Corporate SMTP Patterns**

Try these common patterns for Leading Point:
```
Option 1:
SMTP Server: mail.leading-point.com
Port: 587
Security: STARTTLS

Option 2:
SMTP Server: smtp.leading-point.com
Port: 25
Security: None or STARTTLS

Option 3:
SMTP Server: mail.leading-point.com
Port: 465
Security: SSL/TLS
```

### **Step 3: Jenkins Configuration**

1. **Go to**: http://localhost:8080 → Manage Jenkins → System
2. **Extended E-mail Notification**:
   - SMTP Server: `mail.leading-point.com`
   - SMTP Port: `587`
   - Username: `husami@leading-point.com`
   - Password: `[your password]`
   - ✓ Use SMTP Authentication
   - ✓ Use TLS (try unchecking if it fails)

3. **Test**: Send test email to `hosam93644@gmail.com`

## 🔧 **Network Troubleshooting**

### **Check SMTP Connectivity from Jenkins Container**
```bash
# Test if Jenkins can reach SMTP servers
docker exec jenkins-with-docker nslookup smtp.gmail.com
docker exec jenkins-with-docker telnet smtp.gmail.com 587
docker exec jenkins-with-docker telnet mail.leading-point.com 587
```

### **Check Corporate Firewall**
```bash
# Test from your local machine
telnet smtp.gmail.com 587
telnet mail.leading-point.com 587
```

## 📧 **Alternative: Gmail with App Password**

If corporate SMTP doesn't work, use Gmail with proper setup:

### **Step 1: Enable 2FA and Generate App Password**
1. Go to: https://myaccount.google.com/security
2. Enable 2-Factor Authentication
3. Go to App Passwords
4. Generate password for "Jenkins"
5. Use the 16-character password (not your regular Gmail password)

### **Step 2: Jenkins Gmail Configuration**
```
SMTP Server: smtp.gmail.com
SMTP Port: 587
Username: hosam93644@gmail.com  # Use your Gmail, not corporate email
Password: [16-character App Password]
✓ Use SMTP Authentication
✓ Use TLS
```

## 🌐 **Docker Network Fix**

### **Option 1: Allow External SMTP in Docker**
```bash
# Stop Jenkins
docker stop jenkins-with-docker

# Restart with network host mode (allows external connections)
docker run -d --name jenkins-with-docker-fixed \
  --network host \
  -v jenkins_home:/var/jenkins_home \
  -v /var/run/docker.sock:/var/run/docker.sock \
  jenkins/jenkins:lts
```

### **Option 2: Configure Corporate Proxy (if needed)**
```bash
# If your company uses a proxy
docker exec jenkins-with-docker bash -c "
export http_proxy=http://proxy.leading-point.com:8080
export https_proxy=http://proxy.leading-point.com:8080
"
```

## 📝 **Quick Test Steps**

1. **Ask IT for SMTP settings**
2. **Test connectivity**: `telnet mail.leading-point.com 587`
3. **Configure Jenkins** with corporate SMTP
4. **Test email** to external address
5. **If fails**: Use Gmail with App Password

## 🚨 **Common Corporate Email Issues**

1. **VPN Required**: Some companies require VPN for SMTP
2. **Whitelisted IPs**: SMTP server might only allow specific IPs
3. **Proxy Configuration**: Corporate proxy might block direct SMTP
4. **Port Blocking**: IT might block ports 587/465/25

## ✅ **Expected Success**
```
✅ Email sent successfully to hosam93644@gmail.com
SMTP connection established to mail.leading-point.com:587
``` 