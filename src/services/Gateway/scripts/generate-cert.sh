#!/bin/bash

# Generate a self-signed certificate for bookmarks.cezn.tech
# Note: this will be CA certificate which can be used to sign other certificates.
# Use it to sign certificates for other services for tls.
openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 365 -nodes -subj "/CN=bookmarks.cezn.tech"
sudo chown 1654:1654 cert.pem key.pem
echo "Certificate generated: cert.pem and key.pem"
