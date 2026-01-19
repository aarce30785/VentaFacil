#!/bin/bash

# Define hostname
export HOSTNAME=${SERVER_HOSTNAME:-localhost}

# Configure Postfix
postconf -e "myhostname = $HOSTNAME"
postconf -e "inet_interfaces = all"
postconf -e "inet_protocols = ipv4"
# Allow relay from Docker networks (private addresses)
postconf -e "mynetworks = 127.0.0.0/8 10.0.0.0/8 172.16.0.0/12 192.168.0.0/16"
# Log to stdout for Docker logs
postconf -e "maillog_file = /dev/stdout"

# Start Postfix in foreground
echo "Starting Postfix ($HOSTNAME)..."
/usr/sbin/postfix start-fg
