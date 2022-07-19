:warning: This project is deprecated in favor of Azure native solutions as both App Service and CDN now support certificates natively:

See [documentation for free certificates on App Services](https://docs.microsoft.com/azure/app-service/configure-ssl-certificate).

~~See [documentation for free certificates on Azure CDN](https://docs.microsoft.com/azure/cdn/cdn-custom-ssl).~~

Microsoft decided to deprecate apex domain certs for CDN (and also CDN seems to be deprecated in favor of frontdoor).

Since frontdoor is a minimum of 35$/month this function still has value. Although cheaper/free alternatives like [github pages](https://pages.github.com/), [netlify](https://www.netlify.com/) and [vercel](https://vercel.com/) exist.

______________

**Original readme**

# Azure function based Let's Encrypt automation

Automatically issue Let's Encrypt SSL certificates for all your custom domain names in Azure.

# Motivation

Existing solutions ([Let's Encrypt Site Extension](https://github.com/sjkp/letsencrypt-siteextension), [Let's Encrypt Webapp Renewer](https://github.com/ohadschn/letsencrypt-webapp-renewer)) work well but are target at Azure App Services only.

This solution also enables Azure CDN based domains to use Let's Encrypt certificates (Azure CDN is needed if you want a custom domain name for your static website hosted via azure blob storage).

# Details

The function runs on a daily schedule and automatically renews all certificates that are close to expiring (based on a configurable threshold). In such a case the function will issue a new certificate for the app service/CDN and automatically configure it.

# Features

* automated Let's Encrypt certificate renewal for
    * Azure App Service*
    * Azure CDN
* securely store certificates in keyvaults
* cheap to run (< 0.10$/month)

\*Managed App Service Certificates have been provided for free by Microsoft for a while now and as of [March 17th 2021 also support Apex domains](https://azure.microsoft.com/updates/public-preview-app-service-managed-certificates-now-supports-apex-domains/). This means an Azure native solution exists that automatically rotates app service certificates. I recommend you use it instead of this function for app services.

# Error handling

The function runs every day. In case of an error it will simply retry the next day (Let's encrypt also recommends running the renewal daily). If you would like to be informed of any errors you can set up an azure alert to monitor exceptions in the application insights instance (e.g. exception > 0) and have an email/notification delivered to you.

In the worst case (complete failure of the function for a long time) Let's Encrypt will also send out emails to the domain owners days before the actual expiry.

# Setup

See [Setup](./docs/Setup.md).

# Changelog

Changelog is [here](Changelog.md).
