FROM alpine:latest

# Install Postfix and dependencies
RUN apk add --no-cache postfix bash ca-certificates

# Copy entrypoint script
COPY run.sh /run.sh

# Fix line endings (if created on Windows) and permissions
RUN apk add --no-cache dos2unix && dos2unix /run.sh && chmod +x /run.sh

# Expose SMTP port
EXPOSE 25

# Run script
ENTRYPOINT ["/run.sh"]
