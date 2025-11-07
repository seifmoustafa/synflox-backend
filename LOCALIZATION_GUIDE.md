# SYNFLOX Localization Guide

## How Frontend Sends Language Preference

SYNFLOX supports **3 ways** for the frontend to specify the user's selected language, in priority order (following 2025 RESTful API best practices):

### 1. Accept-Language Header (Highest Priority) ⭐ **MOST RESTFUL - RECOMMENDED**

**Best for:** Standard HTTP (RFC 7231), RESTful API design, automatic browser detection

**Why this is best practice:**
- ✅ Standard HTTP header (RFC 7231) for content negotiation
- ✅ Semantically correct (language is metadata, not part of resource)
- ✅ Clean URLs (no query parameter clutter)
- ✅ Works perfectly with all HTTP methods (GET, POST, PUT, DELETE)
- ✅ Standard RESTful approach used by major APIs (GitHub, Stripe, etc.)

**Usage:**
```http
GET /api/companies
Accept-Language: ar

POST /api/companies
Accept-Language: en
Content-Type: application/json
```

**Example (JavaScript/Axios):**
```javascript
// Frontend stores user's language preference
const userLanguage = localStorage.getItem('language') || 'en'; // 'en' or 'ar'

// Add to all API calls via headers (RECOMMENDED)
axios.defaults.headers.common['Accept-Language'] = userLanguage;

// Or per request:
axios.get('/api/companies', {
  headers: { 'Accept-Language': userLanguage }
});

axios.post('/api/companies', data, {
  headers: { 'Accept-Language': userLanguage }
});
```

**Example (React with fetch):**
```javascript
const language = 'ar'; // from user selection
fetch('/api/companies', {
  method: 'GET',
  headers: { 
    'Accept-Language': language,
    'Authorization': `Bearer ${token}` 
  }
});
```

---

### 2. Custom Header X-Language (Second Priority) ⭐ **RECOMMENDED FOR SPAs**

**Best for:** Explicit control in Single Page Applications, clean API calls

**Usage:**
```http
GET /api/companies
X-Language: ar

POST /api/companies
X-Language: en
Content-Type: application/json
```

**Example (JavaScript/Axios):**
```javascript
const userLanguage = localStorage.getItem('language') || 'en';

axios.get('/api/companies', {
  headers: {
    'X-Language': userLanguage,
    'Authorization': `Bearer ${token}`
  }
});
```

**Example (React with fetch):**
```javascript
fetch('/api/companies', {
  method: 'GET',
  headers: {
    'X-Language': 'ar',
    'Authorization': `Bearer ${token}`
  }
});
```

---

### 3. Query Parameter (Third Priority - Fallback)

**Best for:** Compatibility, debugging, simple integrations

**Note:** While supported, query parameters are not ideal for RESTful APIs because:
- ❌ They clutter URLs, especially for POST/PUT/DELETE
- ❌ Language is metadata, not part of the resource
- ❌ Can be cached separately from the resource
- ❌ Not the standard HTTP way

**Usage:**
```http
GET /api/companies?lang=ar
POST /api/companies?lang=ar
```

**Example (JavaScript/Axios):**
```javascript
// Fallback method - not recommended as primary
const userLanguage = localStorage.getItem('language') || 'en';
axios.get('/api/companies', {
  params: { lang: userLanguage }
});
```

---

## Priority Order (2025 Best Practices)

The backend checks language in this order:
1. ✅ **Accept-Language header** (Standard HTTP, RFC 7231) - **MOST RESTFUL** ⭐
2. ✅ **Custom header** `X-Language: ar` or `X-Language: en` - **RECOMMENDED FOR SPAs**
3. ✅ **Query parameter** `?lang=ar` - Fallback for compatibility
4. ✅ **Default** → English (`en`) if none provided

---

## Frontend Implementation Examples

### React Example (Recommended Approach)

```javascript
// LanguageContext.jsx
import { createContext, useContext, useState, useEffect } from 'react';
import axios from 'axios';

const LanguageContext = createContext();

export const LanguageProvider = ({ children }) => {
  const [language, setLanguage] = useState(
    localStorage.getItem('language') || 'en'
  );

  useEffect(() => {
    // Set default language in localStorage
    localStorage.setItem('language', language);
    
    // Configure axios to always send language via Accept-Language header (RECOMMENDED)
    axios.defaults.headers.common['Accept-Language'] = language;
    // OR use custom header:
    // axios.defaults.headers.common['X-Language'] = language;
  }, [language]);

  return (
    <LanguageContext.Provider value={{ language, setLanguage }}>
      {children}
    </LanguageContext.Provider>
  );
};

// Usage in components
const { language, setLanguage } = useContext(LanguageContext);

// Change language
setLanguage('ar'); // Automatically updates all API calls

// API calls automatically include ?lang=ar
const response = await axios.get('/api/companies');
```

### Vue.js Example

```javascript
// languageStore.js (Pinia/Vuex)
import { defineStore } from 'pinia';
import axios from 'axios';

export const useLanguageStore = defineStore('language', {
  state: () => ({
    language: localStorage.getItem('language') || 'en'
  }),
  
  actions: {
    setLanguage(lang) {
      this.language = lang;
      localStorage.setItem('language', lang);
      
      // Update axios interceptor to use Accept-Language header (RECOMMENDED)
      axios.interceptors.request.use(config => {
        config.headers['Accept-Language'] = this.language;
        return config;
      });
    }
  }
});

// Usage
const languageStore = useLanguageStore();
languageStore.setLanguage('ar');
```

### Angular Example

```typescript
// language.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private currentLanguage = localStorage.getItem('language') || 'en';

  constructor(private http: HttpClient) {}

  setLanguage(lang: 'en' | 'ar') {
    this.currentLanguage = lang;
    localStorage.setItem('language', lang);
  }

  getLanguage(): string {
    return this.currentLanguage;
  }

  // Helper to add language header to requests (RECOMMENDED)
  addLanguageHeader(headers: HttpHeaders): HttpHeaders {
    return headers.set('Accept-Language', this.currentLanguage);
  }
}

// Usage in component
this.http.get('/api/companies', {
  headers: this.languageService.addLanguageHeader(new HttpHeaders())
}).subscribe(...);
```

---

## API Response Examples

### English Request
```http
GET /api/companies
Accept-Language: en
Authorization: Bearer {token}
```

**Response:**
```json
{
  "statusCode": 200,
  "message": "Company created successfully",
  "data": { ... }
}
```

### Arabic Request
```http
GET /api/companies
Accept-Language: ar
Authorization: Bearer {token}
```

**Response:**
```json
{
  "statusCode": 200,
  "message": "تم إنشاء الشركة بنجاح",
  "data": { ... }
}
```

---

## Best Practices

### ✅ DO:
- **Use Accept-Language header** for RESTful API design (Recommended) ⭐
- **Or use X-Language header** for explicit control in SPAs (Also Recommended)
- Store user's language preference in localStorage/sessionStorage
- Include language in all API calls automatically via axios interceptor
- Default to English if user hasn't selected a language

### ❌ DON'T:
- Don't use query parameters as primary method (not RESTful, clutters URLs)
- Don't rely on browser/system locale only
- Don't hardcode language in frontend without user selection
- Don't forget to include language in POST/PUT/DELETE requests

---

## Testing

### Test with cURL:

**English (Accept-Language header - RECOMMENDED):**
```bash
curl -X GET "https://api.synflux.com/api/companies" \
  -H "Accept-Language: en" \
  -H "Authorization: Bearer {token}"
```

**Arabic (Accept-Language header - RECOMMENDED):**
```bash
curl -X GET "https://api.synflux.com/api/companies" \
  -H "Accept-Language: ar" \
  -H "Authorization: Bearer {token}"
```

**Using Custom Header (X-Language):**
```bash
curl -X GET "https://api.synflux.com/api/companies" \
  -H "X-Language: ar" \
  -H "Authorization: Bearer {token}"
```

**Using Query Parameter (Fallback):**
```bash
curl -X GET "https://api.synflux.com/api/companies?lang=ar" \
  -H "Authorization: Bearer {token}"
```

---

## Summary

**Recommended Approach (2025 Best Practices):**

### Primary: Accept-Language Header ⭐
```javascript
axios.defaults.headers.common['Accept-Language'] = 'ar';
```

### Alternative: X-Language Header (for SPAs)
```javascript
axios.defaults.headers.common['X-Language'] = 'ar';
```

**Why Headers are Better:**
- ✅ **RESTful** - Standard HTTP (RFC 7231) for content negotiation
- ✅ **Semantically correct** - Language is metadata, not part of resource
- ✅ **Clean URLs** - No query parameter clutter
- ✅ **Works with all HTTP methods** - GET, POST, PUT, DELETE
- ✅ **Standard practice** - Used by GitHub, Stripe, AWS, Google APIs
- ✅ **Better caching** - Headers are part of request, not URL

**Why Query Parameters are NOT Recommended:**
- ❌ Clutters URLs, especially for POST/PUT/DELETE
- ❌ Not semantically correct (language is metadata)
- ❌ Can be cached separately from resource
- ❌ Not the standard HTTP way

**Frontend Implementation:**
1. Store user's language selection in localStorage
2. Set `Accept-Language` header in axios defaults or interceptor
3. Header is automatically included in all requests
4. Update when user changes language

---

**Last Updated:** 2025

