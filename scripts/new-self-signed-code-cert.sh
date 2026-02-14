#!/bin/bash
#
# new-self-signed-code-cert.sh
#
# Generates a self-signed code signing certificate for Koncierge UI.
# This is the macOS/Linux equivalent of New-SelfSignedCodeCert.ps1
#
# The script creates a self-signed certificate suitable for code signing Windows applications.
# It exports the certificate in two formats:
# - PFX file (with private key, password protected) - for signing
# - CER file (public key only) - for distribution to users
#
# Usage:
#   ./new-self-signed-code-cert.sh [OPTIONS]
#
# Options:
#   -n, --name       Certificate subject name (default: "Koncierge UI")
#   -p, --password   Password for the PFX file (if not provided, a random one is generated)
#   -o, --output     Output directory (default: current directory)
#   -h, --help       Show this help message
#
# Example:
#   ./new-self-signed-code-cert.sh --name "Koncierge UI" --password "MySecurePass123!" --output ./certs
#
# After running this script:
#   1. Keep the PFX file secure (it contains the private key)
#   2. Add the content of cert-base64.txt to GitHub secrets as WINDOWS_CERTIFICATE
#   3. Add the password to GitHub secrets as WINDOWS_CERTIFICATE_PASSWORD
#   4. Distribute the CER file to end users who want to trust your certificate
#

set -e

# Default values
CERT_NAME="Koncierge UI"
PASSWORD=""
OUTPUT_PATH="."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Print colored output
print_color() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Show help
show_help() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Generate a self-signed code signing certificate for Koncierge UI."
    echo ""
    echo "Options:"
    echo "  -n, --name       Certificate subject name (default: \"Koncierge UI\")"
    echo "  -p, --password   Password for the PFX file (auto-generated if not provided)"
    echo "  -o, --output     Output directory (default: current directory)"
    echo "  -h, --help       Show this help message"
    echo ""
    echo "Example:"
    echo "  $0 --name \"Koncierge UI\" --password \"MySecurePass123!\" --output ./certs"
    echo ""
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -n|--name)
            CERT_NAME="$2"
            shift 2
            ;;
        -p|--password)
            PASSWORD="$2"
            shift 2
            ;;
        -o|--output)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            print_color "$RED" "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Check if openssl is installed
if ! command -v openssl &> /dev/null; then
    print_color "$RED" "Error: openssl is not installed."
    echo "Install it with:"
    echo "  macOS:  brew install openssl"
    echo "  Ubuntu: sudo apt-get install openssl"
    exit 1
fi

# Generate a strong random password if not provided
if [ -z "$PASSWORD" ]; then
    print_color "$YELLOW" "No password provided. Generating a strong random password..."
    PASSWORD=$(openssl rand -base64 24 | tr -dc 'A-Za-z0-9!@#$%&*+=' | head -c 32)
    print_color "$GREEN" "✓ Generated strong random password"
    echo ""
fi

# Create output directory if it doesn't exist
if [ ! -d "$OUTPUT_PATH" ]; then
    print_color "$YELLOW" "Creating output directory: $OUTPUT_PATH"
    mkdir -p "$OUTPUT_PATH"
fi

# Resolve to absolute path
OUTPUT_PATH=$(cd "$OUTPUT_PATH" && pwd)

# Define file paths
KEY_PATH="$OUTPUT_PATH/KonciergeUI-CodeSigning.key"
CSR_PATH="$OUTPUT_PATH/KonciergeUI-CodeSigning.csr"
CRT_PATH="$OUTPUT_PATH/KonciergeUI-CodeSigning.crt"
PFX_PATH="$OUTPUT_PATH/KonciergeUI-CodeSigning.pfx"
CER_PATH="$OUTPUT_PATH/KonciergeUI-CodeSigning.cer"
BASE64_PATH="$OUTPUT_PATH/cert-base64.txt"

print_color "$CYAN" "============================================"
print_color "$CYAN" "  Self-Signed Code Signing Certificate"
print_color "$CYAN" "============================================"
echo ""
print_color "$WHITE" "Certificate Name: $CERT_NAME"
print_color "$WHITE" "Output Path:      $OUTPUT_PATH"
echo ""

# Check if files already exist
if [ -f "$PFX_PATH" ] || [ -f "$CER_PATH" ]; then
    print_color "$YELLOW" "Warning: Certificate files already exist in $OUTPUT_PATH"
    read -p "Overwrite existing files? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_color "$YELLOW" "Operation cancelled."
        exit 0
    fi
fi

# Calculate dates
NOT_BEFORE=$(date -u +"%Y%m%d000000Z")
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS date command
    NOT_AFTER=$(date -u -v+3y +"%Y%m%d000000Z")
else
    # Linux date command
    NOT_AFTER=$(date -u -d "+3 years" +"%Y%m%d000000Z")
fi

print_color "$GREEN" "Creating self-signed certificate..."

# Create OpenSSL config file for code signing
CONFIG_PATH="$OUTPUT_PATH/codesign.cnf"
cat > "$CONFIG_PATH" << EOF
[req]
default_bits = 2048
prompt = no
default_md = sha256
distinguished_name = dn
x509_extensions = v3_ext

[dn]
CN = $CERT_NAME

[v3_ext]
basicConstraints = CA:FALSE
keyUsage = digitalSignature
extendedKeyUsage = codeSigning
subjectKeyIdentifier = hash
EOF

# Generate private key
print_color "$GRAY" "  Generating 2048-bit RSA private key..."
openssl genrsa -out "$KEY_PATH" 2048 2>/dev/null

# Generate self-signed certificate
print_color "$GRAY" "  Creating self-signed certificate (valid for 3 years)..."
openssl req -new -x509 \
    -key "$KEY_PATH" \
    -out "$CRT_PATH" \
    -days 1095 \
    -config "$CONFIG_PATH" \
    -extensions v3_ext \
    2>/dev/null

# Get certificate info
THUMBPRINT=$(openssl x509 -in "$CRT_PATH" -noout -fingerprint -sha1 2>/dev/null | sed 's/.*=//' | tr -d ':')
VALID_FROM=$(openssl x509 -in "$CRT_PATH" -noout -startdate 2>/dev/null | sed 's/notBefore=//')
VALID_TO=$(openssl x509 -in "$CRT_PATH" -noout -enddate 2>/dev/null | sed 's/notAfter=//')

print_color "$GREEN" "✓ Certificate created successfully"
print_color "$GRAY" "  Thumbprint: $THUMBPRINT"
print_color "$GRAY" "  Valid From: $VALID_FROM"
print_color "$GRAY" "  Valid To:   $VALID_TO"
echo ""

# Export to PFX (PKCS#12) format
print_color "$GREEN" "Exporting PFX file (with private key)..."
openssl pkcs12 -export \
    -out "$PFX_PATH" \
    -inkey "$KEY_PATH" \
    -in "$CRT_PATH" \
    -passout "pass:$PASSWORD" \
    2>/dev/null
print_color "$GREEN" "✓ Exported: $PFX_PATH"
echo ""

# Export CER (DER format, public key only)
print_color "$GREEN" "Exporting CER file (public key only)..."
openssl x509 -in "$CRT_PATH" -outform DER -out "$CER_PATH" 2>/dev/null
print_color "$GREEN" "✓ Exported: $CER_PATH"
echo ""

# Generate base64 for GitHub secrets
print_color "$GREEN" "Generating base64 encoding for GitHub secrets..."
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS base64 (no -w option)
    base64 -i "$PFX_PATH" -o "$BASE64_PATH"
else
    # Linux base64
    base64 -w 0 "$PFX_PATH" > "$BASE64_PATH"
fi
print_color "$GREEN" "✓ Base64 saved to: $BASE64_PATH"
echo ""

# Clean up temporary files
print_color "$YELLOW" "Cleaning up temporary files..."
rm -f "$KEY_PATH" "$CSR_PATH" "$CRT_PATH" "$CONFIG_PATH"
print_color "$GREEN" "✓ Temporary files removed"
echo ""

print_color "$CYAN" "============================================"
print_color "$CYAN" "  Certificate Generation Complete!"
print_color "$CYAN" "============================================"
echo ""
print_color "$YELLOW" "Next Steps:"
print_color "$WHITE" "1. Keep the PFX file ($PFX_PATH) SECURE"
print_color "$WHITE" "   This contains your private key!"
echo ""
print_color "$WHITE" "2. Add GitHub Secrets to your repository:"
print_color "$WHITE" "   - WINDOWS_CERTIFICATE: Content of $BASE64_PATH"
print_color "$WHITE" "   - WINDOWS_CERTIFICATE_PASSWORD: $PASSWORD"
echo ""
print_color "$WHITE" "3. Distribute the CER file ($CER_PATH) to end users"
print_color "$WHITE" "   Users must install it to 'Trusted Root Certification Authorities'"
print_color "$WHITE" "   to trust your signed applications."
echo ""
print_color "$WHITE" "4. See docs/SIGNING.md for complete instructions"
echo ""

# Print password (important for user to save)
print_color "$CYAN" "============================================"
print_color "$YELLOW" "IMPORTANT - Save this password securely:"
print_color "$WHITE" "$PASSWORD"
print_color "$CYAN" "============================================"
echo ""

