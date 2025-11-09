# Gmail SMTP Setup Guide for SYNFLOX

## 📧 How to Configure Gmail to Send Emails

### Step 1: Enable 2-Step Verification

1. Go to your Google Account: https://myaccount.google.com/
2. Click **Security** (left sidebar)
3. Under **Signing in to Google**, find **2-Step Verification**
4. Click **Get Started** and follow the prompts
5. Complete the setup (you'll need your phone)

**Why?** Gmail requires 2-Step Verification to generate App Passwords.

---

### Step 2: Generate App Password

1. Go back to **Security** settings: https://myaccount.google.com/security
2. Under **Signing in to Google**, find **App passwords**
3. Click **App passwords**
4. You may need to sign in again
5. Select app: **Mail**
6. Select device: **Other (Custom name)**
7. Enter name: **SYNFLOX System**
8. Click **Generate**
9. **COPY THE 16-CHARACTER PASSWORD** (it looks like: `abcd efgh ijkl mnop`)
   - ⚠️ **IMPORTANT**: You can only see this password once! Copy it now!

---

### Step 3: Update Configuration

#### Option A: Update appsettings.json (For Local Development)

1. Open `WebAPI/appsettings.json`
2. Update the `EmailSettings` section:

```json
"EmailSettings": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "User": "seifmoustafa.off.eng@gmail.com",
  "Pass": "YOUR_16_CHARACTER_APP_PASSWORD",  // Paste the app password here (remove spaces)
  "FromName": "SYNFLOX System",
  "FromEmail": "seifmoustafa.off.eng@gmail.com",
  "Mode": "Prod"
}
```

**Example:**
```json
"Pass": "abcdefghijklmnop"  // Remove spaces from the generated password
```

#### Option B: Update Dockerfile (For Docker Deployment)

1. Open `Dockerfile`
2. Update the environment variables:

```dockerfile
ENV EmailSettings__User=seifmoustafa.off.eng@gmail.com \
    EmailSettings__Pass=YOUR_16_CHARACTER_APP_PASSWORD
```

**Example:**
```dockerfile
ENV EmailSettings__User=seifmoustafa.off.eng@gmail.com \
    EmailSettings__Pass=abcdefghijklmnop
```

#### Option C: Use Environment Variables (Recommended for Production)

Set environment variables instead of hardcoding in files:

**Windows (PowerShell):**
```powershell
$env:EmailSettings__User="seifmoustafa.off.eng@gmail.com"
$env:EmailSettings__Pass="YOUR_16_CHARACTER_APP_PASSWORD"
```

**Linux/Mac:**
```bash
export EmailSettings__User="seifmoustafa.off.eng@gmail.com"
export EmailSettings__Pass="YOUR_16_CHARACTER_APP_PASSWORD"
```

**Docker:**
```bash
docker run -e EmailSettings__User="seifmoustafa.off.eng@gmail.com" \
           -e EmailSettings__Pass="YOUR_16_CHARACTER_APP_PASSWORD" \
           your-image
```

---

### Step 4: Test Email Configuration

1. Restart your application
2. Trigger an email (e.g., activate a company subscription)
3. Check the logs for email sending status
4. Check the recipient's inbox (and spam folder)

---

## 🔒 Security Best Practices

### ✅ DO:
- ✅ Use **App Passwords** (not your regular Gmail password)
- ✅ Store passwords in **environment variables** (not in code)
- ✅ Use **secrets management** in production (Azure Key Vault, AWS Secrets Manager, etc.)
- ✅ Rotate app passwords regularly
- ✅ Use different app passwords for different environments (dev, staging, prod)

### ❌ DON'T:
- ❌ **NEVER** commit passwords to Git
- ❌ **NEVER** use your regular Gmail password
- ❌ **NEVER** hardcode passwords in source code
- ❌ **NEVER** share app passwords publicly

---

## 🐛 Troubleshooting

### Problem: "Invalid credentials" or "Authentication failed"

**Solutions:**
1. ✅ Make sure you're using an **App Password** (not your regular password)
2. ✅ Make sure **2-Step Verification** is enabled
3. ✅ Check that the password has no spaces (remove all spaces)
4. ✅ Verify the email address is correct
5. ✅ Try generating a new App Password

### Problem: "Connection timeout" or "Could not connect"

**Solutions:**
1. ✅ Check firewall settings (port 587 should be open)
2. ✅ Verify SMTP settings:
   - Host: `smtp.gmail.com`
   - Port: `587`
   - Enable SSL: `true`
3. ✅ Check if your network blocks SMTP ports

### Problem: Emails going to spam

**Solutions:**
1. ✅ Add SPF record to your domain (if using custom domain)
2. ✅ Use a professional "From Name" (e.g., "SYNFLOX System")
3. ✅ Include unsubscribe links in emails
4. ✅ Avoid spam trigger words in subject/body

### Problem: "Less secure app access" error

**Solution:**
- This error means you're using your regular password instead of an App Password
- Generate an App Password following Step 2 above

---

## 📝 Gmail SMTP Settings Summary

| Setting | Value |
|---------|-------|
| **SMTP Server** | `smtp.gmail.com` |
| **Port** | `587` (TLS) or `465` (SSL) |
| **Security** | STARTTLS (port 587) or SSL (port 465) |
| **Username** | Your full Gmail address |
| **Password** | 16-character App Password (not your regular password) |
| **Authentication** | Required (use App Password) |

---

## 🔄 How to Revoke/Regenerate App Password

If you need to revoke or regenerate an app password:

1. Go to: https://myaccount.google.com/apppasswords
2. Find the app password you want to revoke
3. Click the **X** (delete) icon
4. Generate a new one if needed
5. Update your configuration with the new password

---

## 📧 Current Configuration

After following this guide, your configuration should be:

**appsettings.json:**
```json
"EmailSettings": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "User": "seifmoustafa.off.eng@gmail.com",
  "Pass": "YOUR_APP_PASSWORD_HERE",
  "FromName": "SYNFLOX System",
  "FromEmail": "seifmoustafa.off.eng@gmail.com",
  "Mode": "Prod"
}
```

**Dockerfile:**
```dockerfile
ENV EmailSettings__User=seifmoustafa.off.eng@gmail.com \
    EmailSettings__Pass=YOUR_APP_PASSWORD_HERE
```

---

## ✅ Verification Checklist

- [ ] 2-Step Verification enabled on Gmail account
- [ ] App Password generated (16 characters)
- [ ] App Password copied (no spaces)
- [ ] `appsettings.json` updated with new email and password
- [ ] `Dockerfile` updated (if using Docker)
- [ ] Application restarted
- [ ] Test email sent successfully
- [ ] Email received in inbox (not spam)

---

## 🎯 Quick Start

1. **Enable 2-Step Verification**: https://myaccount.google.com/security
2. **Generate App Password**: https://myaccount.google.com/apppasswords
3. **Update config**: Replace `YOUR_APP_PASSWORD_HERE` in `appsettings.json` and `Dockerfile`
4. **Restart app**: Restart your application
5. **Test**: Trigger an email and verify it's sent

---

**Need Help?** Check Gmail's official guide: https://support.google.com/accounts/answer/185833

